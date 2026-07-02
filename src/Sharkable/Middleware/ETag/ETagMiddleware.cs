using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Middleware that adds ETag support to GET/HEAD responses.
/// On first request: computes SHA256 hash of the response body, stores it as ETag.
/// On subsequent requests with matching <c>If-None-Match</c>: returns 304 Not Modified.
/// </summary>
internal sealed class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ETagOptions _options;
    private readonly ILogger<ETagMiddleware> _logger;

    public ETagMiddleware(RequestDelegate next, ETagOptions options, ILogger<ETagMiddleware> logger)
    {
        _next = next;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!_options.CacheableMethods.Contains(context.Request.Method) || ShouldSkip(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var counting = new CountingResponseBody(originalBody, _options.MaxResponseSize, hash);

        context.Response.Body = counting;
        var limitExceeded = false;
        try
        {
            await _next(context);
        }
        catch (ResponseSizeExceededException)
        {
            limitExceeded = true;
        }
        finally
        {
            context.Response.Body = originalBody;
        }

        if (IsUncacheableStatus(context.Response.StatusCode))
        {
            await counting.FlushAsync(context.RequestAborted);
            return;
        }

        if (limitExceeded || counting.BytesWritten > _options.MaxResponseSize)
        {
            _logger.LogWarning(
                "ETag skipped for {Method} {Path}: response body {Bytes} bytes exceeds MaxResponseSize {Cap}",
                context.Request.Method, context.Request.Path, counting.BytesWritten, _options.MaxResponseSize);
            await counting.FlushAsync(context.RequestAborted);
            return;
        }

        var hashBytes = hash.GetHashAndReset();
        var hashHex = ToHex(hashBytes);
        var etag = $"\"{hashHex}\"";

        context.Response.Headers["ETag"] = etag;
        context.Response.Headers["Cache-Control"] = _options.CacheControlHeader;

        if (context.Request.Headers.TryGetValue("If-None-Match", out var ifNoneMatch) &&
            ifNoneMatch.ToString().Trim('"') == hashHex)
        {
            context.Response.StatusCode = 304;
            context.Response.ContentLength = 0;
            return;
        }

        await counting.FlushAsync(context.RequestAborted);
    }

    private bool ShouldSkip(string path)
    {
        foreach (var exclude in _options.ExcludePaths)
        {
            if (path.StartsWith(exclude, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private bool IsUncacheableStatus(int statusCode)
        => _options.ShouldSkipStatus(statusCode);

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
            sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    /// <summary>
    /// Wraps a response stream, forwarding bytes to the inner stream while
    /// counting bytes written, incrementally updating a hash, and throwing
    /// <see cref="ResponseSizeExceededException"/> when the byte count exceeds
    /// the configured cap. Keeps an internal spool so partial writes past the
    /// cap can still be flushed to the client with no ETag header.
    /// </summary>
    private sealed class CountingResponseBody : Stream
    {
        private readonly Stream _inner;
        private readonly long _maxBytes;
        private readonly IncrementalHash _hash;
        private readonly MemoryStream _spool = new();
        private long _bytesWritten;

        public CountingResponseBody(Stream inner, long maxBytes, IncrementalHash hash)
        {
            _inner = inner;
            _maxBytes = maxBytes;
            _hash = hash;
        }

        public long BytesWritten => _bytesWritten;

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => true;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override void Flush() => _inner.Flush();

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await _spool.FlushAsync(cancellationToken);
            _spool.Position = 0;
            await _spool.CopyToAsync(_inner, cancellationToken);
            await _spool.DisposeAsync();
            await _inner.FlushAsync(cancellationToken);
        }

        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bytesWritten += count;
            if (_bytesWritten > _maxBytes)
            {
                // Spool the over-the-limit remainder so FlushAsync can still
                // deliver the body; the caller will detect limitExceeded and
                // skip ETag generation.
                _spool.Write(buffer, offset, count);
                throw new ResponseSizeExceededException();
            }
            _hash.AppendData(buffer, offset, count);
            _spool.Write(buffer, offset, count);
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            _bytesWritten += buffer.Length;
            if (_bytesWritten > _maxBytes)
            {
                _spool.Write(buffer.Span);
                throw new ResponseSizeExceededException();
            }
            _hash.AppendData(buffer.Span);
            _spool.Write(buffer.Span);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Thrown when the buffered response body exceeds
    /// <see cref="ETagOptions.MaxResponseSize"/>. Caught by
    /// <see cref="ETagMiddleware"/> to skip ETag generation and pass the
    /// response through unchanged.
    /// </summary>
    [Serializable]
    public sealed class ResponseSizeExceededException : Exception
    {
        public ResponseSizeExceededException()
            : base("Response body exceeded the configured ETag MaxResponseSize cap.") { }
    }
}
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Middleware that enforces idempotency for unsafe HTTP methods via the
/// <c>Idempotency-Key</c> header. See
/// <c>docs/superpowers/specs/2026-06-26-idempotency-middleware-design.md</c>.
/// </summary>
internal sealed class SharkIdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    private readonly SharkIdempotencyOptions _options;
    private readonly ILogger<SharkIdempotencyMiddleware> _logger;

    public SharkIdempotencyMiddleware(
        RequestDelegate next,
        IIdempotencyStore store,
        SharkIdempotencyOptions options,
        ILogger<SharkIdempotencyMiddleware> logger)
    {
        _next = next;
        _store = store;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Header present?
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var headerValues)
            || headerValues.Count == 0
            || string.IsNullOrWhiteSpace(headerValues[0]))
        {
            await _next(context);
            return;
        }

        var key = headerValues[0]!;

        // 2. Key valid?
        if (!_options.IsValidKey(key))
        {
            await WriteUnified(context, 400, "invalid_idempotency_key",
                $"Key must be 1..{_options.MaxKeyLength} printable ASCII characters.");
            return;
        }

        // 3. Method eligible?
        if (!_options.UnsafeMethods.Contains(new HttpMethod(context.Request.Method)))
        {
            await _next(context);
            return;
        }

        // 4. Try to reserve the slot.
        if (!_store.TryReserve(key, _options.InFlightTtl))
        {
            // Slot was already taken. Look at its state.
            var existing = _store.Get(key);
            switch (existing)
            {
                case IdempotencyInFlight:
                    context.Response.Headers["Retry-After"] = "1";
                    await WriteUnified(context, 409, "idempotency_in_progress",
                        "An identical request is already in progress; retry after 1 second.");
                    return;

                case IdempotencyHit hit:
                    var fingerprint = ComputeFingerprint(context);
                    if (hit.Record.Fingerprint != fingerprint)
                    {
                        await WriteUnified(context, 422, "idempotency_key_conflict",
                            "Idempotency-Key was reused with a different request payload.");
                        return;
                    }
                    await Replay(context, hit.Record);
                    return;

                default:
                    // Race: placeholder expired between TryReserve and Get. Fall through and execute.
                    break;
            }
        }

        // 5. We own the in-flight slot. Execute downstream with response buffering.
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);

            // 5a. Oversize response -> 500, do not cache.
            if (buffer.Length > _options.MaxResponseSize)
            {
                _logger.LogWarning(
                    "Idempotency response for key {Key} exceeds {Max} bytes; " +
                    "rejecting and releasing in-flight slot.",
                    key, _options.MaxResponseSize);
                _store.Release(key);
                context.Response.StatusCode = 500;
                context.Response.Body = originalBody;
                await WriteUnified(context, 500, "idempotency_response_too_large",
                    $"Response body exceeded {_options.MaxResponseSize} bytes; " +
                    "idempotent replay is not available for this response.");
                return;
            }

            // 5b. Successful (cacheable) responses -> store and forward.
            if (ShouldCache(context.Response.StatusCode))
            {
                buffer.Position = 0;
                var bytes = buffer.ToArray();
                await buffer.CopyToAsync(originalBody);

                var record = new IdempotencyRecord(
                    key,
                    ComputeFingerprint(context),
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/octet-stream",
                    bytes,
                    DateTimeOffset.UtcNow);
                _store.Store(key, record, _options.Ttl);
            }
            else
            {
                // 429 or 5xx: do not cache. Release slot and forward body.
                _store.Release(key);
                await buffer.CopyToAsync(originalBody);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldCache(int status) =>
        status >= 200 && status < 500 && status != 429;

    private string ComputeFingerprint(HttpContext context)
    {
        // For fingerprint we use the buffered request body; if no body was read,
        // we fall back to an empty span. The path is context.Request.Path.
        // Note: this middleware does not pre-buffer the request body. We rely on
        // the body being re-readable (set by upstream middleware such as
        // EnableBuffering). If the body is not seekable and not yet consumed,
        // we treat it as empty (known limitation; see §8 of the spec).
        var bodyLength = (int)(context.Request.ContentLength ?? 0);
        byte[] body = bodyLength > 0
            ? ReadBodyBytes(context.Request.Body, bodyLength)
            : Array.Empty<byte>();
        return IdempotencyFingerprint.Compute(
            context.Request.Method,
            context.Request.Path,
            body);
    }

    private static byte[] ReadBodyBytes(Stream body, int length)
    {
        if (body.CanSeek) body.Position = 0;
        var buf = new byte[length];
        int read = 0;
        while (read < length)
        {
            int n = body.Read(buf, read, length - read);
            if (n == 0) break;
            read += n;
        }
        if (read < length) Array.Resize(ref buf, read);
        return buf;
    }

    private async Task Replay(HttpContext context, IdempotencyRecord record)
    {
        context.Response.StatusCode = record.StatusCode;
        context.Response.ContentType = record.ContentType;
        context.Response.Headers[_options.ReplayedHeaderName] = "true";
        await context.Response.Body.WriteAsync(record.Body);
    }

    private static Task WriteUnified(
        HttpContext context, int status, string code, string message)
    {
        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var result = factory.Create(data: null, errorMessage: message, statusCode: status, code: code);
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(result, result.GetType());
    }
}

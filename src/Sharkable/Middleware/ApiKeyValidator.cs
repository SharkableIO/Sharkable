using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Options;

namespace Sharkable;

internal sealed class ApiKeyValidator : IDisposable
{
    private readonly IOptionsMonitor<SharkOption> _options;
    private readonly IDisposable? _changeListener;
    private byte[][] _cachedHashes = [];
    private bool _disposed;

    public ApiKeyValidator(IOptionsMonitor<SharkOption> options)
    {
        _options = options;
        RebuildCache(options.CurrentValue.ApiKeys);
        _changeListener = options.OnChange((opt, _) => RebuildCache(opt.ApiKeys));
    }

    private void RebuildCache(string[]? keys)
    {
        if (keys == null || keys.Length == 0)
        {
            _cachedHashes = [];
            return;
        }
        var hashes = new byte[keys.Length][];
        for (var i = 0; i < keys.Length; i++)
            hashes[i] = SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]));
        _cachedHashes = hashes;
    }

    public bool Validate(string providedApiKey)
    {
        var cached = _cachedHashes;
        if (cached.Length == 0) return false;
        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(providedApiKey));
        var matched = false;
        for (var i = 0; i < cached.Length; i++)
        {
            if (CryptographicOperations.FixedTimeEquals(candidateHash, cached[i]))
                matched = true;
        }
        return matched;
    }

    public bool HasConfiguredKeys => _cachedHashes.Length > 0;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _changeListener?.Dispose();
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Computes the SHA-256 fingerprint used to detect "same Idempotency-Key,
/// different request payload". Hashes <c>method + "\n" + path + "\n" + body</c>
/// using incremental hashing so the full body need not be materialized.
/// </summary>
internal static class IdempotencyFingerprint
{
    /// <summary>
    /// Computes the lower-case hex SHA-256 of the request identity.
    /// </summary>
    /// <param name="method">HTTP method (case-insensitive; normalized to upper).</param>
    /// <param name="path">Request path. Null/empty is treated as <c>"/"</c>.</param>
    /// <param name="body">Raw request body bytes.</param>
    public static string Compute(string method, PathString path, ReadOnlySpan<byte> body)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var methodBytes = Encoding.ASCII.GetBytes(method.ToUpperInvariant());
        sha.AppendData(methodBytes);
        sha.AppendData(new byte[] { (byte)'\n' });
        var pathValue = path.Value ?? "/";
        sha.AppendData(Encoding.ASCII.GetBytes(pathValue));
        sha.AppendData(new byte[] { (byte)'\n' });
        sha.AppendData(body);
        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
    }
}
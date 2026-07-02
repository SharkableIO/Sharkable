using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sharkable.Tests.Idempotency;

public class IdempotencyFingerprintTests
{
    private const string DefaultUser = "<anon>";

    [Fact]
    public void Compute_SameInputs_ProducesSameHash()
    {
        var body = Encoding.UTF8.GetBytes("hello");
        var h1 = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/api/orders", body);
        var h2 = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/api/orders", body);
        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);
        Assert.Matches("^[0-9a-f]{64}$", h1);
    }

    [Theory]
    [InlineData("GET", "/api/orders", new byte[] { 1, 2, 3 })]
    [InlineData("POST", "/api/users", new byte[] { 1, 2, 3 })]
    [InlineData("POST", "/api/orders", new byte[] { 1, 2, 4 })]
    [InlineData("POST", "/api/orders", new byte[] { })]
    public void Compute_DifferentInputs_ProduceDifferentHashes(string method, string path, byte[] body)
    {
        var baseline = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/api/orders", new byte[] { 1, 2, 3 });
        var actual = IdempotencyFingerprint.Compute(DefaultUser, method, path, body);
        Assert.NotEqual(baseline, actual);
    }

    [Fact]
    public void Compute_MethodIsCaseInsensitive()
    {
        var body = Encoding.UTF8.GetBytes("x");
        var h1 = IdempotencyFingerprint.Compute(DefaultUser, "post", "/x", body);
        var h2 = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/x", body);
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Compute_EmptyBody_HasStableHash()
    {
        var h1 = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/api/orders", ReadOnlySpan<byte>.Empty);
        var h2 = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/api/orders", Array.Empty<byte>());
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Compute_NullOrEmptyPath_TreatedAsRootSlash()
    {
        var body = Encoding.UTF8.GetBytes("payload");
        var root = IdempotencyFingerprint.Compute(DefaultUser, "POST", "/", body);
        Assert.Equal(root, IdempotencyFingerprint.Compute(DefaultUser, "POST", default, body));
        Assert.Equal(root, IdempotencyFingerprint.Compute(DefaultUser, "POST", new PathString("/"), body));
    }

    [Fact]
    public void Compute_DifferentUsers_ProduceDifferentHashes()
    {
        // SHARK-SEC-M021: the same Idempotency-Key + body from two different
        // users must hash to different fingerprints so a replay cannot cross
        // user boundaries.
        var body = Encoding.UTF8.GetBytes("payload");
        var alice = IdempotencyFingerprint.Compute("alice", "POST", "/api/orders", body);
        var bob = IdempotencyFingerprint.Compute("bob", "POST", "/api/orders", body);
        Assert.NotEqual(alice, bob);
    }

    [Fact]
    public async Task ComputeAsync_EmptyBody_HasStableHash()
    {
        using var s1 = new MemoryStream();
        using var s2 = new MemoryStream();
        var h1 = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", s1, 1024, 0);
        var h2 = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", s2, 1024, 0);
        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);
    }

    [Fact]
    public async Task ComputeAsync_ReadsIncrementallyUpToMaxBodySize()
    {
        var body = new byte[2048];
        Random.Shared.NextBytes(body);
        using var s = new MemoryStream(body);

        var h = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", s, 1024, body.Length);
        Assert.Equal(64, h.Length);
    }

    [Fact]
    public async Task ComputeAsync_ChunkedDifferentBodies_ProduceDifferentHashes()
    {
        // SHARK-SEC-002 chunked-bypass fix: chunked requests with different bodies
        // must NOT share a fingerprint. Pass -1 as the contentLength sentinel.
        using var s1 = new MemoryStream(Encoding.UTF8.GetBytes("alpha"));
        using var s2 = new MemoryStream(Encoding.UTF8.GetBytes("bravo"));

        var h1 = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", s1, 1024, -1);
        var h2 = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", s2, 1024, -1);
        Assert.NotEqual(h1, h2);
    }

    [Fact]
    public async Task ComputeAsync_ChunkedWithEmptyBody_DiffersFromContentLengthZero()
    {
        // A chunked request that sends no body must NOT collide with a request
        // that sends Content-Length: 0 — the -1 sentinel is mixed into the hash.
        using var emptyChunked = new MemoryStream(Array.Empty<byte>());
        using var zeroCl = new MemoryStream(Array.Empty<byte>());

        var hChunked = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", emptyChunked, 1024, -1);
        var hZero = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", zeroCl, 1024, 0);
        Assert.NotEqual(hChunked, hZero);
    }

    [Fact]
    public async Task ComputeAsync_ChunkedWithBody_DiffersFromFixedLengthSameBody()
    {
        // Chunked (length unknown) must NOT collide with a fixed-length request
        // carrying the same body — the -1 sentinel differs from any non-negative length.
        var bodyBytes = Encoding.UTF8.GetBytes("identical-payload");
        using var chunked = new MemoryStream(bodyBytes);
        using var fixedLen = new MemoryStream(bodyBytes);

        var hChunked = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", chunked, 1024, -1);
        var hFixed = await IdempotencyFingerprint.ComputeAsync(DefaultUser, "POST", "/api/orders", fixedLen, 1024, bodyBytes.Length);
        Assert.NotEqual(hChunked, hFixed);
    }

    [Fact]
    public async Task ComputeAsync_DifferentUsers_ProduceDifferentHashes()
    {
        // SHARK-SEC-M021: same body from two different users must NOT share
        // a fingerprint when using the async incremental-hash path.
        using var sAlice = new MemoryStream(Encoding.UTF8.GetBytes("payload"));
        using var sBob = new MemoryStream(Encoding.UTF8.GetBytes("payload"));

        var hAlice = await IdempotencyFingerprint.ComputeAsync("alice", "POST", "/api/orders", sAlice, 1024, 7);
        var hBob = await IdempotencyFingerprint.ComputeAsync("bob", "POST", "/api/orders", sBob, 1024, 7);
        Assert.NotEqual(hAlice, hBob);
    }
}
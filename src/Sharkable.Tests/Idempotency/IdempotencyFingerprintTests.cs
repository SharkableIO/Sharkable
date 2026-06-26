using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sharkable.Tests.Idempotency;

public class IdempotencyFingerprintTests
{
    [Fact]
    public void Compute_SameInputs_ProducesSameHash()
    {
        var body = Encoding.UTF8.GetBytes("hello");
        var h1 = IdempotencyFingerprint.Compute("POST", "/api/orders", body);
        var h2 = IdempotencyFingerprint.Compute("POST", "/api/orders", body);
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
        var baseline = IdempotencyFingerprint.Compute("POST", "/api/orders", new byte[] { 1, 2, 3 });
        var actual = IdempotencyFingerprint.Compute(method, path, body);
        Assert.NotEqual(baseline, actual);
    }

    [Fact]
    public void Compute_MethodIsCaseInsensitive()
    {
        var body = Encoding.UTF8.GetBytes("x");
        var h1 = IdempotencyFingerprint.Compute("post", "/x", body);
        var h2 = IdempotencyFingerprint.Compute("POST", "/x", body);
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Compute_EmptyBody_HasStableHash()
    {
        var h1 = IdempotencyFingerprint.Compute("POST", "/api/orders", ReadOnlySpan<byte>.Empty);
        var h2 = IdempotencyFingerprint.Compute("POST", "/api/orders", Array.Empty<byte>());
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Compute_NullOrEmptyPath_TreatedAsRootSlash()
    {
        var body = Encoding.UTF8.GetBytes("payload");
        var root = IdempotencyFingerprint.Compute("POST", "/", body);
        Assert.Equal(root, IdempotencyFingerprint.Compute("POST", default, body));
        Assert.Equal(root, IdempotencyFingerprint.Compute("POST", new PathString("/"), body));
    }
}
using Microsoft.AspNetCore.Http;

namespace Sharkable.Tests.Idempotency;

public class SharkIdempotencyOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var o = new SharkIdempotencyOptions();
        Assert.Equal(TimeSpan.FromHours(24), o.Ttl);
        Assert.Equal(TimeSpan.FromSeconds(30), o.InFlightTtl);
        Assert.Equal(255, o.MaxKeyLength);
        Assert.Equal(1_048_576, o.MaxResponseSize);
        Assert.Equal("Idempotency-Key", o.HeaderName);
        Assert.Equal("X-Idempotent-Replayed", o.ReplayedHeaderName);
        Assert.Contains(HttpMethod.Post, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Put, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Patch, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Delete, o.UnsafeMethods);
    }

    [Fact]
    public void IsValidKey_AcceptsNormalUuid()
    {
        var o = new SharkIdempotencyOptions();
        Assert.True(o.IsValidKey("8c0a6f4e-9b2d-4f1a-b3c7-2e5d8a1f0b6c"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void IsValidKey_RejectsEmptyAndWhitespace(string key)
    {
        var o = new SharkIdempotencyOptions();
        Assert.False(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_RejectsTooLong()
    {
        var o = new SharkIdempotencyOptions();
        var key = new string('a', 256);
        Assert.False(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_AcceptsMaxLength()
    {
        var o = new SharkIdempotencyOptions();
        var key = new string('a', 255);
        Assert.True(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_RejectsControlChars()
    {
        var o = new SharkIdempotencyOptions();
        Assert.False(o.IsValidKey("abcdef\x01ghijklmnopqrst"));
    }

    [Theory]
    [InlineData("a")]
    [InlineData("short")]
    [InlineData("fifteen-chars")]
    [InlineData("123456789012345")] // 15 chars
    public void IsValidKey_RejectsShorterThanMinimum(string key)
    {
        // SHARK-SEC-L002: IETF draft recommends ≥ 16 chars to prevent
        // keyspace pre-burning.
        var o = new SharkIdempotencyOptions();
        Assert.False(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_AcceptsExactlyMinimumLength()
    {
        var o = new SharkIdempotencyOptions();
        var key = "1234567890123456"; // 16 chars
        Assert.True(o.IsValidKey(key));
    }
}
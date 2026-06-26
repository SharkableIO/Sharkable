using Microsoft.Extensions.Caching.Memory;

namespace Sharkable.Tests.Idempotency;

public class MemoryIdempotencyStoreTests
{
    private static MemoryIdempotencyStore NewStore() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void TryReserve_NewKey_ReturnsTrue()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void TryReserve_SameKeyTwice_SecondReturnsFalse()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
        Assert.False(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Get_BeforeReserve_ReturnsNull()
    {
        var s = NewStore();
        Assert.Null(s.Get("missing"));
    }

    [Fact]
    public void Get_AfterReserve_ReturnsInFlight()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var result = s.Get("k1");
        Assert.IsType<IdempotencyInFlight>(result);
    }

    [Fact]
    public void Store_AfterReserve_GetReturnsHit()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "hash123", 200, "application/json",
            new byte[] { 1, 2, 3 }, DateTimeOffset.UtcNow);
        s.Store("k1", record, TimeSpan.FromMinutes(1));

        var result = s.Get("k1");
        var hit = Assert.IsType<IdempotencyHit>(result);
        Assert.Equal("hash123", hit.Record.Fingerprint);
        Assert.Equal(200, hit.Record.StatusCode);
    }

    [Fact]
    public void Release_AllowsRereserve()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        s.Release("k1");
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void TryReserve_ExpiresAfterTtl()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMilliseconds(50)));
        System.Threading.Thread.Sleep(200);
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Store_RemovesInFlightPlaceholder()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "h", 200, "text/plain",
            new byte[] { 9 }, DateTimeOffset.UtcNow);
        s.Store("k1", record, TimeSpan.FromMinutes(1));

        // After store, Release + reserve should succeed; test the release path.
        s.Release("k1");
        Assert.True(s.TryReserve("k1", TimeSpan.FromMilliseconds(50)));
    }
}

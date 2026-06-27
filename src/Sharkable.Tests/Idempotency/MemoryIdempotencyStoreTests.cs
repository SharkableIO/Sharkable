using Microsoft.Extensions.Caching.Memory;

namespace Sharkable.Tests.Idempotency;

public class MemoryIdempotencyStoreTests
{
    private static MemoryIdempotencyStore NewStore() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public async Task TryReserve_NewKey_ReturnsTrue()
    {
        var s = NewStore();
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task TryReserve_SameKeyTwice_SecondReturnsFalse()
    {
        var s = NewStore();
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1)));
        Assert.False(await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Get_BeforeReserve_ReturnsNull()
    {
        var s = NewStore();
        Assert.Null(await s.GetAsync("missing"));
    }

    [Fact]
    public async Task Get_AfterReserve_ReturnsInFlight()
    {
        var s = NewStore();
        await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        var result = await s.GetAsync("k1");
        Assert.IsType<IdempotencyInFlight>(result);
    }

    [Fact]
    public async Task Store_AfterReserve_GetReturnsHit()
    {
        var s = NewStore();
        await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "hash123", 200, "application/json",
            new byte[] { 1, 2, 3 }, DateTimeOffset.UtcNow);
        await s.StoreAsync("k1", record, TimeSpan.FromMinutes(1));

        var result = await s.GetAsync("k1");
        var hit = Assert.IsType<IdempotencyHit>(result);
        Assert.Equal("hash123", hit.Record.Fingerprint);
        Assert.Equal(200, hit.Record.StatusCode);
    }

    [Fact]
    public async Task Release_AllowsRereserve()
    {
        var s = NewStore();
        await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        await s.ReleaseAsync("k1");
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task TryReserve_ExpiresAfterTtl()
    {
        var s = NewStore();
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMilliseconds(50)));
        await Task.Delay(200);
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public async Task Store_RemovesInFlightPlaceholder()
    {
        var s = NewStore();
        await s.TryReserveAsync("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "h", 200, "text/plain",
            new byte[] { 9 }, DateTimeOffset.UtcNow);
        await s.StoreAsync("k1", record, TimeSpan.FromMinutes(1));

        // After store, Release + reserve should succeed; test the release path.
        await s.ReleaseAsync("k1");
        Assert.True(await s.TryReserveAsync("k1", TimeSpan.FromMilliseconds(50)));
    }
}

using Microsoft.Extensions.Logging.Abstractions;

namespace Sharkable.Tests;

public class SagaExecutorTests
{
    [Fact]
    public void LockTtl_DefaultsToFiveMinutes()
    {
        var sut = new SagaExecutor(new MemorySagaStore(), NullLogger<SagaExecutor>.Instance);
        Assert.Equal(TimeSpan.FromMinutes(5), sut.LockTtl);
    }

    [Fact]
    public void LockRenewalInterval_DefaultsToLockTtlOverThree()
    {
        var sut = new SagaExecutor(
            new MemorySagaStore(),
            NullLogger<SagaExecutor>.Instance,
            TimeSpan.FromMinutes(3));
        Assert.Equal(TimeSpan.FromMinutes(1), sut.LockRenewalInterval);
    }

    [Fact]
    public void LockTtl_NegativeValue_Throws()
    {
        var sut = new SagaExecutor(new MemorySagaStore(), NullLogger<SagaExecutor>.Instance);
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.LockTtl = TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void LockRenewalInterval_NegativeValue_Throws()
    {
        var sut = new SagaExecutor(new MemorySagaStore(), NullLogger<SagaExecutor>.Instance);
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.LockRenewalInterval = TimeSpan.FromSeconds(-1));
    }

    [Fact]
    public void LockRenewalInterval_GreaterOrEqualToLockTtl_Throws()
    {
        var sut = new SagaExecutor(
            new MemorySagaStore(),
            NullLogger<SagaExecutor>.Instance,
            TimeSpan.FromMinutes(1));
        // default LockRenewalInterval is 20s; setting it to >= LockTtl (1min) must throw
        Assert.Throws<ArgumentOutOfRangeException>(
            () => sut.LockRenewalInterval = TimeSpan.FromMinutes(1));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => sut.LockRenewalInterval = TimeSpan.FromMinutes(2));
    }

    [Fact]
    public void LockTtl_LessOrEqualToLockRenewalInterval_Throws()
    {
        var sut = new SagaExecutor(
            new MemorySagaStore(),
            NullLogger<SagaExecutor>.Instance,
            TimeSpan.FromMinutes(5));
        // default LockRenewalInterval is 100s; shrinking LockTtl to <= 100s must throw
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.LockTtl = TimeSpan.FromSeconds(100));
        Assert.Throws<ArgumentOutOfRangeException>(() => sut.LockTtl = TimeSpan.FromSeconds(50));
    }

    [Fact]
    public void LockRenewalInterval_Zero_DisablesRenewal_WithoutThrowing()
    {
        var sut = new SagaExecutor(
            new MemorySagaStore(),
            NullLogger<SagaExecutor>.Instance,
            TimeSpan.FromMinutes(1));
        sut.LockRenewalInterval = TimeSpan.Zero;
        Assert.Equal(TimeSpan.Zero, sut.LockRenewalInterval);
    }

    [Fact]
    public void LockTtl_Zero_AcceptedWithZeroRenewal()
    {
        var sut = new SagaExecutor(new MemorySagaStore(), NullLogger<SagaExecutor>.Instance);
        sut.LockTtl = TimeSpan.Zero;
        Assert.Equal(TimeSpan.Zero, sut.LockTtl);
    }
}
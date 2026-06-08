using System.Net;

namespace Sharkable.Tests;

public class ServicesAndExtensionsTests : IClassFixture<SharkTestFixture>
{
    private readonly SharkTestFixture _fixture;
    private readonly HttpClient _client;

    public ServicesAndExtensionsTests(SharkTestFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Client;
    }

    [Fact]
    public void SingletonService_Is_Registered()
    {
        using var scope = _fixture.App.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<TestSingletonService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void ScopedService_Is_Registered()
    {
        using var scope = _fixture.App.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<TestScopedService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void TransientService_Is_Registered()
    {
        using var scope = _fixture.App.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<TestTransientService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void InterfaceBased_SingletonService_Is_Registered()
    {
        using var scope = _fixture.App.Services.CreateScope();
        var svc = scope.ServiceProvider.GetService<ITestService>();
        Assert.NotNull(svc);
        Assert.Equal("hello from DI", svc.GetMessage());
    }

    [Fact]
    public void Shark_GetService_Resolves_Service()
    {
        var svc = Shark.GetService<TestSingletonService>();
        Assert.NotNull(svc);
    }

    [Fact]
    public void Shark_GetOptions_Returns_SharkOption()
    {
        var opt = Shark.GetOptions<SharkOption>();
        Assert.NotNull(opt);
        Assert.Equal("api", opt.ApiPrefix);
    }
}

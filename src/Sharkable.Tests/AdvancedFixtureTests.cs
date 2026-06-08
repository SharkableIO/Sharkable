using System.Net;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests;

public class HealthCheckFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark([typeof(SimpleGetEndpoint).Assembly], opt =>
        {
            opt.EnableHealthChecks = true;
            opt.ApiKeys = ["secret-key-123"];
        });

        App = builder.Build();
        App.UseShark();
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

public class AdvancedTests : IClassFixture<HealthCheckFixture>
{
    private readonly HttpClient _client;

    public AdvancedTests(HealthCheckFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task HealthCheck_WhenEnabled_Returns_Ok()
    {
        var res = await _client.GetAsync("/healthz");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task ApiKey_Missing_Returns_401()
    {
        var res = await _client.GetAsync("/api/simpleget/ping");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ApiKey_Invalid_Returns_401()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/simpleget/ping");
        request.Headers.Add("X-Api-Key", "wrong-key");
        var res = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ApiKey_Valid_Passes_Through()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/simpleget/ping");
        request.Headers.Add("X-Api-Key", "secret-key-123");
        var res = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("pong", body);
    }
}

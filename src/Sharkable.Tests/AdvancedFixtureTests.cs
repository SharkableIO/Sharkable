using System.Net;
using System.Text.Json;
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

public class AutoWrapFixture : IAsyncLifetime
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
            opt.EnableAutoWrap = true;
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

public class AutoWrapUseSharkFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark([typeof(SimpleGetEndpoint).Assembly]);

        App = builder.Build();
        App.UseShark(opt =>
        {
            opt.EnableAutoWrap = true;
        });
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

public class AutoWrapTests : IClassFixture<AutoWrapFixture>
{
    private readonly HttpClient _client;

    public AutoWrapTests(AutoWrapFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task PlainString_Is_Wrapped_In_UnifiedResult()
    {
        var res = await _client.GetAsync("/api/autowraptest/plain");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("statusCode", out var code));
        Assert.Equal(200, code.GetInt32());
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal("plain-string", data.GetString());
        Assert.True(root.TryGetProperty("errorMessage", out var err));
        Assert.Equal(JsonValueKind.Null, err.ValueKind);
        Assert.True(root.TryGetProperty("timeStamp", out _));
    }

    [Fact]
    public async Task IResult_Is_Not_Wrapped()
    {
        var res = await _client.GetAsync("/api/autowraptest/iresult");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("\"iresult-string\"", body);
    }

    [Fact]
    public async Task IntValue_Is_Wrapped()
    {
        var res = await _client.GetAsync("/api/autowraptest/int-value");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("statusCode", out var code));
        Assert.Equal(200, code.GetInt32());
        Assert.True(root.TryGetProperty("data", out var data));
        Assert.Equal(42, data.GetInt32());
    }

    [Fact]
    public async Task SimpleGetEndpoint_Is_Also_Wrapped_When_AutoWrap_Global()
    {
        // EnableAutoWrap is set globally — affects all groups, not just AutoWrapTestEndpoint
        var res = await _client.GetAsync("/api/simpleget/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("data", out var data));
        Assert.Equal("pong", data.GetString());
    }
}

public class AutoWrapUseSharkTests : IClassFixture<AutoWrapUseSharkFixture>
{
    private readonly HttpClient _client;

    public AutoWrapUseSharkTests(AutoWrapUseSharkFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task PlainString_Is_Wrapped_Via_UseSharkOptions()
    {
        var res = await _client.GetAsync("/api/autowraptest/plain");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);

        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.True(json.RootElement.TryGetProperty("statusCode", out var code));
        Assert.Equal(200, code.GetInt32());
    }
}

public class AuditTrailFixture : IAsyncLifetime
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
            opt.ConfigureAuditTrail(a =>
            {
                a.ExcludePaths = ["/excluded"];
                a.ForwardCorrelationId = true;
            });
        });

        App = builder.Build();

        App.MapGet("/excluded", () => "skip-me");

        App.UseShark();
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

public class AuditTrailTests : IClassFixture<AuditTrailFixture>
{
    private readonly HttpClient _client;

    public AuditTrailTests(AuditTrailFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Response_Has_CorrelationId_Header()
    {
        var res = await _client.GetAsync("/api/simpleget/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.True(res.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task CorrelationId_Is_Unique_Per_Request()
    {
        var res1 = await _client.GetAsync("/api/simpleget/ping");
        var res2 = await _client.GetAsync("/api/simpleget/ping");

        var id1 = res1.Headers.GetValues("X-Correlation-Id").First();
        var id2 = res2.Headers.GetValues("X-Correlation-Id").First();
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public async Task Forwarded_CorrelationId_Appears_In_Response()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/simpleget/ping");
        request.Headers.Add("X-Correlation-Id", "my-trace-123");
        var res = await _client.SendAsync(request);

        var returned = res.Headers.GetValues("X-Correlation-Id").First();
        Assert.Equal("my-trace-123", returned);
    }

    [Fact]
    public async Task Excluded_Path_Does_Not_Get_CorrelationId()
    {
        var res = await _client.GetAsync("/excluded");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.False(res.Headers.Contains("X-Correlation-Id"));
    }

    [Fact]
    public async Task Normal_Endpoint_Still_Returns_Correct_Data()
    {
        var res = await _client.GetAsync("/api/simpleget/ping");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("pong", body);
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

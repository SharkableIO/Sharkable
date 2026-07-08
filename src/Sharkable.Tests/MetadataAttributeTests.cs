using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests;

[SharkDescription("Metadata test endpoint", "An endpoint for testing SharkDescription and SharkResponseType attributes.")]
[SharkResponseType(200, typeof(string), "Successful response")]
[SharkResponseType(400, null, "Bad request")]
public sealed class MetadataTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("meta", () => Results.Ok("meta-ok"));
    }
}

[SharkDeprecated]
public sealed class DeprecatedTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("old", () => Results.Ok("still-here"));
    }
}

public sealed class DeprecatedFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Services.AddShark([typeof(DeprecatedTestEndpoint).Assembly]);
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

public sealed class MetadataFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Services.AddShark([typeof(MetadataTestEndpoint).Assembly]);
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

public sealed class MetadataAttributeTests : IClassFixture<MetadataFixture>
{
    private readonly HttpClient _client;

    public MetadataAttributeTests(MetadataFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Endpoint_Is_Reachable()
    {
        var res = await _client.GetAsync("/api/metadataTest/meta");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("\"meta-ok\"", body);
    }

    [Fact]
    public async Task OpenApiDoc_Has_Summary_From_Attribute()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(doc);

        var paths = json.RootElement.GetProperty("paths");
        var getOp = paths.GetProperty("/api/metadataTest/meta").GetProperty("get");

        var summary = getOp.GetProperty("summary").GetString();
        Assert.Equal("Metadata test endpoint", summary);
    }

    [Fact]
    public async Task OpenApiDoc_Has_Description_From_Attribute()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(doc);

        var paths = json.RootElement.GetProperty("paths");
        var getOp = paths.GetProperty("/api/metadataTest/meta").GetProperty("get");

        var description = getOp.GetProperty("description").GetString();
        Assert.Equal("An endpoint for testing SharkDescription and SharkResponseType attributes.", description);
    }
}

public sealed class DeprecatedAttributeTests : IClassFixture<DeprecatedFixture>
{
    private readonly HttpClient _client;

    public DeprecatedAttributeTests(DeprecatedFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Endpoint_Is_Reachable()
    {
        var res = await _client.GetAsync("/api/deprecatedTest/old");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("\"still-here\"", body);
    }

    [Fact]
    public async Task Endpoint_Has_Deprecated_Flag()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(doc);

        var paths = json.RootElement.GetProperty("paths");
        var getOp = paths.GetProperty("/api/deprecatedTest/old").GetProperty("get");

        Assert.True(getOp.TryGetProperty("deprecated", out var deprecated));
        Assert.True(deprecated.GetBoolean());
    }
}

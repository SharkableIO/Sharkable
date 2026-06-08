using System.Net;
using System.Text.Json;

namespace Sharkable.Tests;

public class EndpointTests : IClassFixture<SharkTestFixture>
{
    private readonly HttpClient _client;

    public EndpointTests(SharkTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task SimpleEndpoint_Is_Reachable()
    {
        var res = await _client.GetAsync("/api/simpleget/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("pong", body);
    }

    [Fact]
    public async Task GroupedEndpoint_Uses_EndpointGroup_Url()
    {
        var res = await _client.GetAsync("/api/mygroup/hello");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("from-group", body);
    }

    [Fact]
    public async Task AdminEndpoint_Uses_Both_Group_And_Tag()
    {
        var res = await _client.GetAsync("/api/admin/dashboard");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("admin-dashboard", body);
    }

    [Fact]
    public async Task TaggedEndpoint_Returns_Ok()
    {
        var res = await _client.GetAsync("/api/tagged/tagged");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("tagged-value", body);
    }

    [Fact]
    public async Task OpenApiDoc_Contains_All_Paths()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var doc = await res.Content.ReadAsStringAsync();

        Assert.Contains("simpleGet", doc);
        Assert.Contains("/api/mygroup/hello", doc);
        Assert.Contains("/api/admin/dashboard", doc);
        Assert.Contains("/api/tagged/tagged", doc);
    }

    [Fact]
    public async Task OpenApiDoc_Contains_Custom_Tags()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(doc);
        var paths = json.RootElement.GetProperty("paths");

        var adminOps = paths.GetProperty("/api/admin/dashboard").GetProperty("get");
        var adminTags = adminOps.GetProperty("tags").EnumerateArray().Select(t => t.GetString()).ToList();
        Assert.Contains("admin-tag", adminTags);
    }

    [Fact]
    public async Task VersionedEndpoint_Uses_Version_In_Url()
    {
        var res = await _client.GetAsync("/api/v1/versionedtest/ping");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("versioned-pong", body);
    }

    [Fact]
    public async Task VersionedAdminEndpoint_Uses_Both_Version_And_Group()
    {
        var res = await _client.GetAsync("/api/v2/admin/status");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("v2-admin-status", body);
    }

    [Fact]
    public async Task OpenApiDoc_Contains_Versioned_Paths()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();
        Assert.Contains("/api/v1/versionedTest/ping", doc);
        Assert.Contains("/api/v2/admin/status", doc);
    }

    [Fact]
    public async Task OpenApiDoc_Has_OperationId()
    {
        var res = await _client.GetAsync("/openapi/v1.json");
        var doc = await res.Content.ReadAsStringAsync();

        using var json = JsonDocument.Parse(doc);
        var paths = json.RootElement.GetProperty("paths");

        var op = paths
            .GetProperty("/api/simpleGet/ping")
            .GetProperty("get");
        var opId = op.GetProperty("operationId").GetString();
        Assert.Equal("simpleGet_GET_ping", opId);
    }
}

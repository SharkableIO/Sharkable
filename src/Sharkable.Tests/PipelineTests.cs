using System.Net;
using System.Text.Json;

namespace Sharkable.Tests;

public class PipelineTests : IClassFixture<SharkTestFixture>
{
    private readonly HttpClient _client;

    public PipelineTests(SharkTestFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task ExceptionHandler_Returns_404_For_KeyNotFoundException()
    {
        var res = await _client.GetAsync("/api/throwingtest/notfound");
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(404, json.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("item missing", json.RootElement.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public async Task ExceptionHandler_Returns_401_For_UnauthorizedAccessException()
    {
        var res = await _client.GetAsync("/api/throwingtest/unauth");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task ExceptionHandler_Returns_400_For_ArgumentException()
    {
        var res = await _client.GetAsync("/api/throwingtest/badarg");
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task ExceptionHandler_Returns_500_For_InvalidOperationException()
    {
        var res = await _client.GetAsync("/api/throwingtest/server");
        Assert.Equal(HttpStatusCode.InternalServerError, res.StatusCode);
    }

    [Fact]
    public async Task ExceptionHandler_Response_Has_UnifiedResult_Shape()
    {
        var res = await _client.GetAsync("/api/throwingtest/server");
        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("statusCode", out _));
        Assert.True(root.TryGetProperty("errorMessage", out _));
        Assert.True(root.TryGetProperty("data", out _));
        Assert.Equal("server error", root.GetProperty("errorMessage").GetString());
    }
}

using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace Sharkable.Tests;

internal sealed class ListLoggerProvider : ILoggerProvider
{
    public ListLogger Logger { get; } = new();
    public ILogger CreateLogger(string categoryName) => Logger;
    public void Dispose() { }
}

public class ExceptionHandlerLoggingTests : IAsyncLifetime
{
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private ListLoggerProvider _loggerProvider = null!;

    public async Task InitializeAsync()
    {
        _loggerProvider = new ListLoggerProvider();

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_loggerProvider);

        builder.Services.AddShark([typeof(SimpleGetEndpoint).Assembly]);

        _app = builder.Build();
        _app.UseShark();
        await _app.StartAsync();
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await _app.DisposeAsync();
    }

    [Fact]
    public async Task ExceptionHandler_Logs_Error_With_Request_Context()
    {
        var res = await _client.GetAsync("/api/throwingtest/server");
        Assert.Equal(500, (int)res.StatusCode);

        var logEntry = _loggerProvider.Logger.Messages
            .FirstOrDefault(m => m.Contains("Unhandled exception"));
        Assert.NotNull(logEntry);
        Assert.Contains("GET", logEntry);
        Assert.Contains("/api/throwingtest/server", logEntry);
    }

    [Fact]
    public async Task ExceptionHandler_Still_Returns_UnifiedResult()
    {
        var res = await _client.GetAsync("/api/throwingtest/notfound");
        var body = await res.Content.ReadAsStringAsync();
        using var json = JsonDocument.Parse(body);
        Assert.Equal(404, json.RootElement.GetProperty("statusCode").GetInt32());
        Assert.Equal("item missing", json.RootElement.GetProperty("errorMessage").GetString());

        var logEntry = _loggerProvider.Logger.Messages
            .FirstOrDefault(m => m.Contains("Unhandled exception"));
        Assert.NotNull(logEntry);
    }
}

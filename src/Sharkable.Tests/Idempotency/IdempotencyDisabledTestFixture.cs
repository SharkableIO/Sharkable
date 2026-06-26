using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// xUnit fixture that boots a <see cref="WebApplication"/> in-process with
/// idempotency middleware disabled (the default). Shared across tests in
/// <see cref="IdempotencyDisabledIntegrationTests"/> via <c>IClassFixture&lt;&gt;</c>
/// to verify that the <c>Idempotency-Key</c> header is ignored entirely when
/// the feature is turned off.
/// </summary>
public class IdempotencyDisabledTestFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark(
            [typeof(IdempotencyTestEndpoint).Assembly]);

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

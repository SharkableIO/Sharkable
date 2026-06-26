using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// xUnit fixture that boots a <see cref="WebApplication"/> in-process with
/// idempotency middleware enabled and the <see cref="IdempotencyTestEndpoint"/>
/// registered. Shared across tests in
/// <see cref="IdempotencyIntegrationTests"/> via <c>IClassFixture&lt;&gt;</c>.
/// </summary>
public class IdempotencyTestFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark(
            [typeof(IdempotencyTestEndpoint).Assembly],
            opt =>
            {
                opt.EnableIdempotency = true;
                opt.ConfigureIdempotency(o =>
                {
                    o.Ttl = TimeSpan.FromMinutes(5);
                    o.InFlightTtl = TimeSpan.FromSeconds(10);
                });
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
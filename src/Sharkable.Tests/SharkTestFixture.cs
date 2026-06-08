using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests;

public class SharkTestFixture : IAsyncLifetime
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
        App.UseShark();
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}

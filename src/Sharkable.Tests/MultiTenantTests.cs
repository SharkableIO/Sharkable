using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests;

public class TenantTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("whoami", (HttpContext ctx) =>
            ctx.RequestServices.GetService<ITenant>()?.TenantId ?? "no-tenant");
    }
}

public class MultiTenantFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark([typeof(TenantTestEndpoint).Assembly], opt =>
        {
            opt.ConfigureMultiTenant(t => t.ResolveTenant = TenantResolver.FromHost);
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

public class MultiTenantTests : IClassFixture<MultiTenantFixture>
{
    private readonly HttpClient _client;

    public MultiTenantTests(MultiTenantFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task TenantId_Is_Resolved_From_Host()
    {
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/tenanttest/whoami");
        request.Headers.Host = "acme.example.com";
        var res = await _client.SendAsync(request);
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("acme", body);
    }

    [Fact]
    public async Task TenantId_Is_Null_When_No_Resolver()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";
        builder.Services.AddShark([typeof(TenantTestEndpoint).Assembly]);
        var app = builder.Build();
        app.UseShark();
        await app.StartAsync();
        var client = app.GetTestClient();

        var res = await client.GetAsync("/api/tenanttest/whoami");
        var body = await res.Content.ReadAsStringAsync();
        Assert.Equal("no-tenant", body);
        await app.DisposeAsync();
    }
}

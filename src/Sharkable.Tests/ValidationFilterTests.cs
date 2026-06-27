using System.Net;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests;

public record TestRequest(string? Name, int Age);

public class TestRequestValidator : AbstractValidator<TestRequest>
{
    public TestRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required");
        RuleFor(x => x.Age).InclusiveBetween(0, 150).WithMessage("Age must be between 0 and 150");
    }
}

public class ValidationTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("validate", (TestRequest req) => Results.Ok(new { received = req.Name }));
    }
}

public class ValidationFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark([typeof(ValidationTestEndpoint).Assembly], opt =>
        {
            opt.EnableValidation = true;
        });
        // AddShark([assemblies]) sets AotMode=true which skips validator auto-scanning
        builder.Services.AddSingleton<IValidator<TestRequest>, TestRequestValidator>();

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

public class ValidationFilterTests : IClassFixture<ValidationFixture>
{
    private readonly HttpClient _client;

    public ValidationFilterTests(ValidationFixture fixture)
    {
        _client = fixture.Client;
    }

    [Fact]
    public async Task Invalid_Request_Returns_400()
    {
        var body = new { Name = "", Age = 200 };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var res = await _client.PostAsync("/api/validationtest/validate", content);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Valid_Request_Returns_200()
    {
        var body = new { Name = "Alice", Age = 30 };
        var content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
        var res = await _client.PostAsync("/api/validationtest/validate", content);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }
}

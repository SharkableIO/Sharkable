using Sharkable;

namespace Sharkable.Tests;

public class SimpleGetEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("ping", () => "pong");
    }
}

[EndpointGroup("mygroup")]
public class GroupedEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("hello", () => "from-group");
    }
}

[SharkTag("mytag")]
public class TaggedEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("tagged", () => "tagged-value");
    }
}

[EndpointGroup("admin")]
[SharkTag("admin-tag")]
public class AdminTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("dashboard", () => "admin-dashboard");
    }
}

public class AutoWrapTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("plain", () => "plain-string");
        app.MapGet("iresult", () => Results.Ok("iresult-string"));
        app.MapGet("int-value", () => 42);
    }
}

public class ThrowingTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("notfound", (HttpContext _) => throw new KeyNotFoundException("item missing"));
        app.MapGet("unauth", (HttpContext _) => throw new UnauthorizedAccessException("no access"));
        app.MapGet("badarg", (HttpContext _) => throw new ArgumentException("invalid input"));
        app.MapGet("server", (HttpContext _) => throw new InvalidOperationException("server error"));
    }
}

[SingletonService]
public class TestSingletonService
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
}

[ScopedService]
public class TestScopedService
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
}

[TransientService]
public class TestTransientService
{
    public string Id { get; } = Guid.NewGuid().ToString("N")[..8];
}

public interface ITestService
{
    string GetMessage();
}

[SingletonService]
public class TestServiceImpl : ITestService
{
    public string GetMessage() => "hello from DI";
}

public class DiTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("di-service", (ITestService svc) => svc.GetMessage());
    }
}

[SharkVersion("v1")]
public class VersionedTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("ping", () => "versioned-pong");
    }
}

[SharkVersion("v2")]
[EndpointGroup("admin")]
public class VersionedAdminEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("status", () => "v2-admin-status");
    }
}
public class FormatTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("myRoute", () => "ok");
    }
}

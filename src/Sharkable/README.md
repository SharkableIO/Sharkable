# Sharkable
a dotnet minimal api framework collection

## Usage

### automatic dependency injection
```csharp
//first add extension
using Sharkable
builder.Services.AddShark();
//for aot users please specify assemblies by youself and avoid code trim
build.Services.AddShark([typeof(Program).Assembly]);

[ScopedService] //inject class as a scoped service by the given attribute
public class Monitor : IMonitor
{
    public void Show()
    {
        ...
    }
}

//map an endpoint and it works!
app.MapGet("/monitor",([FromServices]IMonitor monitor) =>
{
    monitor.Show();
});
```
### endpoint auto mapper (new style)
create a new class
```csharp
public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("show", ()=>"test result");
    }
}

//will now generate a http get method with the url
// api/test/show
```

### endpoint auto mapper (old style)
create a new class
```csharp
[SharkEndpoint]
public class TestEndpoint
{
    [SharkMethod("show", SharkHttpMethod.GET)]
    public void Show()
    {
        ...
    }
}

//will now generate a http get method with the url
// api/test/show
```

### idempotent retries via Idempotency-Key
opt-in middleware that lets clients safely retry unsafe HTTP requests. when a client
sends the `Idempotency-Key` header on a `POST` / `PUT` / `PATCH` / `DELETE` request, the
first response is cached; subsequent requests with the same key replay it. reusing a
key with a different payload returns 422; concurrent same-key requests return 409 with
`Retry-After: 1`.
```csharp
builder.Services.AddShark(opt =>
{
    opt.EnableIdempotency = true;
    opt.ConfigureIdempotency(o =>
    {
        o.Ttl = TimeSpan.FromHours(24);  // default
        o.MaxResponseSize = 1_048_576;   // default 1 MiB
    });
});
```
see `docs/superpowers/specs/2026-06-26-idempotency-middleware-design.md` for the full
specification including edge cases and limitations.

for more use sample please see Sharkable.Sample project
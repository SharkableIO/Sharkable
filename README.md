# Sharkable

[![Sharkable](https://img.shields.io/nuget/v/Sharkable.svg?color=red&style=flat-square)](https://www.nuget.org/packages/Sharkable/)
[![Sharkable](https://img.shields.io/nuget/dt/Sharkable.svg?style=flat-square)](https://www.nuget.org/packages/Sharkable/)

A .NET 10 minimal API framework collection aimed to support AOT.

## Quick Start

```bash
dotnet add package Sharkable
```

```csharp
using Sharkable;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddShark();

var app = builder.Build();
app.UseShark();
app.Run();
```

For AOT mode, pass assemblies explicitly:

```csharp
builder.Services.AddShark([typeof(Program).Assembly]);
```

## Features

### Auto Endpoint Discovery (New Style)

Create a class implementing `ISharkEndpoint`. It's automatically discovered and registered.

```csharp
public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("hello", () => Results.Ok("hi"));
        app.MapPost("create", (CreateRequest req) => Results.Ok(req));
    }
}
```

URL becomes `api/test/hello`, `api/test/create` (group name derived from class name).

### Endpoint Grouping & OpenAPI Tags

Group multiple endpoints under the same URL prefix and OpenAPI tag.

```csharp
[EndpointGroup("admin")]
[SharkTag("admin")]
public class UserEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("users", () => Results.Ok(users));
    }
}

[EndpointGroup("admin")]
[SharkTag("admin")]
public class RoleEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("roles", () => Results.Ok(roles));
    }
}
```

Both under `api/admin/users` and `api/admin/roles`, sharing OpenAPI tag `admin`. OperationIds are auto-generated via `{group}_{httpMethod}_{path}`.

### Auto DI Registration

Mark classes with attributes or marker interfaces for auto-registration.

```csharp
[ScopedService]   // or [SingletonService], [TransientService]
public class Monitor : IMonitor
{
    public void Show() { }
}

// Or use marker interfaces:
public class Monitor : IMonitor, IScoped { }
```

### Global Exception Handler

Converts unhandled exceptions to `UnifiedResult<T>` JSON responses automatically.

```csharp
app.UseShark(opt =>
{
    opt.ExceptionHandlerOptions.Map<MyException>(HttpStatusCode.Forbidden);
});
```

### Unified Response

Consistent API response format across all endpoints.

```csharp
return data.AsOkResult();
return "error".AsBadRequest();
return "no access".AsUnauthorized();

// Or with auto-wrap:
app.UseShark(opt => opt.EnableAutoWrap = true);
app.MapGet("hello", () => "world"); // -> { "statusCode": 200, "data": "world", ... }
```

### FluentValidation Integration

Automatic request validation with FluentValidation.

```csharp
builder.Services.AddShark(opt => opt.EnableValidation = true);

public class CreateUserValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }
}
```

Invalid requests return 400 with a `UnifiedResult` error body.

### OpenAPI & Scalar UI

OpenAPI spec at `/openapi/v1.json`, Scalar UI at `/scalar/v1`. Enabled by default.

```csharp
builder.Services.AddShark(opt =>
{
    opt.ConfigureOpenApi(options => { /* configure OpenAPI options */ });
});
```

### AutoCrud (SqlSugar)

Auto-generate CRUD endpoints with SqlSugar.

```csharp
builder.Services.AddShark(opt =>
{
    opt.ConfigureAutoCrud(sqlSugar => { /* configure SqlSugar */ });
});
```

### Endpoint Format

Configure URL naming conventions globally.

```csharp
builder.Services.AddShark(opt =>
{
    opt.Format = EndpointFormat.SnakeCase; // CamelCase, ToLower, UnChanged
    opt.ApiPrefix = "api"; // default
});
```

## AOT Support

Sharkable is designed for AOT compilation. Pass assemblies explicitly and register `JsonSerializerContext`:

```csharp
builder.Services.AddShark([typeof(Program).Assembly]);
```

Old-style `[SharkEndpoint]` + `[SharkMethod]` endpoints use reflection and will NOT work in AOT mode.

## Documentation

Full documentation: https://sharkableio.github.io

## License

MIT

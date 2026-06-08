# Exception Handler + Auto Unified Response — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add global exception handler middleware that converts unhandled exceptions to `UnifiedResult` JSON, plus optional auto-wrap of endpoint return values into `UnifiedResult<T>`.

**Architecture:** Middleware catches exceptions early in the pipeline, maps them to HTTP status codes via configurable `ExceptionHandlerOptions`, writes `UnifiedResult<object?>` as JSON. Auto-wrap uses a global `IEndpointFilter` registered during `UseShark()`.

**Tech Stack:** .NET 8, ASP.NET Core minimal APIs, `System.Text.Json`

---

### File Map

| File | Action | Responsibility |
|---|---|---|
| `src/Sharkable/ExceptionHandler/ExceptionHandlerOptions.cs` | Create | Options class with exception→status code mapping, dev mode toggle, error message formatting |
| `src/Sharkable/ExceptionHandler/SharkExceptionHandlerMiddleware.cs` | Create | Middleware that catches exceptions and writes `UnifiedResult<object?>` JSON |
| `src/Sharkable/ExceptionHandler/Extensions/ExceptionHandlerExtension.cs` | Create | `UseSharkExceptionHandler()` extension on `WebApplication` |
| `src/Sharkable/ExceptionHandler/UnifiedResultWrapFilter.cs` | Create | `IEndpointFilter` that wraps non-`IResult` returns in `UnifiedResult<T>` |
| `src/Sharkable/Shark/Options/UseSharkOptions.cs` | Modify | Add `EnableExceptionHandler` and `EnableAutoWrap` flags |
| `src/Sharkable/SharkableExtension.cs` | Modify | Wire exception handler + auto-wrap in `UseShark()` |
| `src/Sharkable/_global.cs` | No change needed | Usings already covered |

---

### Task 1: ExceptionHandlerOptions

**Files:**
- Create: `src/Sharkable/ExceptionHandler/ExceptionHandlerOptions.cs`
- Test: N/A (no test project)

- [ ] **Step 1: Create the file**

```csharp
using System.Net;

namespace Sharkable;

public sealed class ExceptionHandlerOptions
{
    private readonly Dictionary<Type, HttpStatusCode> _exceptionMappings = [];

    /// <summary>
    /// When true, include full exception details (stack trace) in the error response.
    /// Automatically set to <see cref="IHostEnvironment.IsDevelopment()"/> by default.
    /// </summary>
    public bool IsDevelopment { get; set; }

    /// <summary>
    /// Map an exception type to an HTTP status code.
    /// </summary>
    public void Map<TException>(HttpStatusCode statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = statusCode;
    }

    /// <summary>
    /// Resolve the HTTP status code for a given exception by walking the type hierarchy.
    /// Falls back to 500 InternalServerError if no mapping is found.
    /// </summary>
    public HttpStatusCode GetStatusCode(Exception exception)
    {
        var type = exception.GetType();
        while (type != null && type != typeof(Exception))
        {
            if (_exceptionMappings.TryGetValue(type, out var statusCode))
                return statusCode;
            type = type.BaseType;
        }
        return HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Get the error message for the response.
    /// In development mode includes the full ToString() (including stack trace).
    /// </summary>
    public string GetErrorMessage(Exception exception)
    {
        return IsDevelopment ? exception.ToString() : exception.Message;
    }
}
```

- [ ] **Step 2: Add default mappings** (in same file, append to constructor or add a static defaults method)

```csharp
// Append to the constructor after _exceptionMappings initialization:
public ExceptionHandlerOptions()
{
    _exceptionMappings = new Dictionary<Type, HttpStatusCode>
    {
        { typeof(KeyNotFoundException), HttpStatusCode.NotFound },
        { typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized },
        { typeof(ArgumentException), HttpStatusCode.BadRequest },
    };
}
```

---

### Task 2: SharkExceptionHandlerMiddleware

**Files:**
- Create: `src/Sharkable/ExceptionHandler/SharkExceptionHandlerMiddleware.cs`

- [ ] **Step 1: Create the middleware**

```csharp
using System.Net;
using System.Text.Json;

namespace Sharkable;

internal sealed class SharkExceptionHandlerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ExceptionHandlerOptions _options;

    public SharkExceptionHandlerMiddleware(RequestDelegate next, ExceptionHandlerOptions options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var options = Shark.SharkOption.ExceptionHandlerOptions;
        var statusCode = options.GetStatusCode(exception);
        var errorMessage = options.GetErrorMessage(exception);

        var result = new UnifiedResult<object?>
        {
            StatusCode = statusCode,
            Data = null,
            ErrorMessage = errorMessage,
            TimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;
        return JsonSerializer.SerializeAsync(
            context.Response.Body, 
            result, 
            typeof(UnifiedResult<object?>),
            UnifiedResultSourceContext.Default.UnifiedResultObject);
    }
}
```

Note: `UnifiedResultSourceContext` currently defines serializers for `UnifiedResult<string>` and `UnifiedResult<int>`. We need to add `UnifiedResult<object?>`.

- [ ] **Step 2: Update UnifiedResultSourceContext to include `UnifiedResult<object?>`**

Edit `src/Sharkable/UnifiedReults/Context/UnifiedResultSourceContext.cs`:

```csharp
using System.Text.Json.Serialization;

namespace Sharkable;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(UnifiedResult<string>))]
[JsonSerializable(typeof(UnifiedResult<int>))]
[JsonSerializable(typeof(UnifiedResult<object?>))]
internal partial class UnifiedResultSourceContext : JsonSerializerContext
{
    // Required for JSON source generation in AOT
    // Add more UnifiedResult<T> types here as needed
}
```

- [ ] **Step 3: Add a static `UnifiedResultObject` property for cleaner access**

In the same file, add a static property:

```csharp
// Inside UnifiedResultSourceContext:
public static UnifiedResultSourceContext UnifiedResultObject { get; } = new(new JsonSerializerOptions { WriteIndented = true });
```

Wait, actually the source generator creates the property. Let me simplify — just use `Default`:

```csharp
// Don't add custom property, just use the source-generated context
// The serializer call becomes:
return JsonSerializer.SerializeAsync(
    context.Response.Body, 
    result, 
    typeof(UnifiedResult<object?>),
    UnifiedResultSourceContext.Default);
```

The source generator already creates a `Default` static instance. So we just need to add the `[JsonSerializable]` attribute.

---

### Task 3: ExceptionHandlerExtension (UseSharkExceptionHandler)

**Files:**
- Create: `src/Sharkable/ExceptionHandler/Extensions/ExceptionHandlerExtension.cs`

- [ ] **Step 1: Create the extension method**

```csharp
namespace Sharkable;

public static class ExceptionHandlerExtension
{
    /// <summary>
    /// Adds a global exception handler middleware that converts unhandled exceptions
    /// to <see cref="UnifiedResult{T}"/> JSON responses.
    /// </summary>
    public static IApplicationBuilder UseSharkExceptionHandler(
        this IApplicationBuilder app, 
        Action<ExceptionHandlerOptions>? configure = null)
    {
        configure?.Invoke(Shark.SharkOption.ExceptionHandlerOptions);
        return app.UseMiddleware<SharkExceptionHandlerMiddleware>();
    }
}
```

---

### Task 4: Integrate into UseShark flow

**Files:**
- Modify: `src/Sharkable/Shark/Options/UseSharkOptions.cs`
- Modify: `src/Sharkable/Shark/Options/SharkOption.cs`
- Modify: `src/Sharkable/SharkableExtension.cs`

- [ ] **Step 1: Add ExceptionHandlerOptions property to SharkOption**

Edit `src/Sharkable/Shark/Options/SharkOption.cs` — add property:

```csharp
// Add after existing properties:
/// <summary>
/// Options for the global exception handler middleware.
/// </summary>
public ExceptionHandlerOptions ExceptionHandlerOptions { get; set; } = new();
```

- [ ] **Step 2: Add EnableExceptionHandler flag to UseSharkOptions**

Edit `src/Sharkable/Shark/Options/UseSharkOptions.cs`:

```csharp
public sealed class UseSharkOptions : ISharkOption
{
    /// <summary>
    /// When true (default), UseShark wires the global exception handler middleware.
    /// </summary>
    public bool EnableExceptionHandler { get; set; } = true;

    // existing members...
}
```

- [ ] **Step 3: Wire exception handler in UseShark**

Edit `src/Sharkable/SharkableExtension.cs`:

```csharp
public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
{
    // Let the user configure UseSharkOptions first
    var opt = new UseSharkOptions();
    setupOption?.Invoke(opt);
    Shark.UseSharkOptions = opt;

    app.UseCommon(setupOption);

    // Wire exception handler (before endpoints to catch everything)
    if (opt.EnableExceptionHandler)
    {
        var env = app.Services.GetRequiredService<IHostEnvironment>();
        Shark.SharkOption.ExceptionHandlerOptions.IsDevelopment = env.IsDevelopment();
        app.UseSharkExceptionHandler();
    }

    app.MapEndpoints();
}
```

Note: `UseCommon` already sets up Shark state. We need to restructure slightly — currently `setupOption` is passed to `UseCommon` which creates a new `UseSharkOptions`. We should consolidate.

Let me revise:

```csharp
public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
{
    var opt = new UseSharkOptions();
    setupOption?.Invoke(opt);
    Shark.UseSharkOptions = opt;

    app.UseCommon(setupOption);
    app.MapEndpoints();
}
```

Now in `UseCommon`:

```csharp
internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
{
    // setupOptions is already invoked in UseShark, but UseCommon still needs to set internal state
    // No need to invoke setupOptions again since Shark.UseSharkOptions is already set
    InternalShark.Configuration = app.Configuration;
    InternalShark.HostEnvironment = app.Environment;
    InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
    InternalShark.ServiceProvider = app.Services;
    app.UseSharkSwagger();
}
```

Wait, but `UseCommon` currently takes `Action<UseSharkOptions>?` and creates a new `UseSharkOptions`. If I want to change the flow, I need to update `UseCommon` to not create its own options.

Let me look at the current flow again:

```csharp
// SharkableExtension.cs
public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
{
    app.UseCommon(setupOption);
    app.MapEndpoints();
}

// SharkExtension.cs
internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
{
    var opt = new UseSharkOptions();
    setupOptions?.Invoke(opt);
    Shark.UseSharkOptions = opt;
    ...
}
```

Hmm, `UseShark()` passes its callback directly to `UseCommon()`. I could restructure so that `UseShark()` creates the options and passes them, while `UseCommon()` just accepts the already-created options. But that changes the `UseCommon` signature which might be used elsewhere.

Simplest approach: just add the exception handler wiring in `UseShark()` between `UseCommon()` and `MapEndpoints()`:

```csharp
public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
{
    app.UseCommon(setupOption);
    // Wire exception handler after UseCommon (which sets up opt) and before endpoints
    if (Shark.UseSharkOptions?.EnableExceptionHandler ?? true)
    {
        var env = app.Services.GetRequiredService<IHostEnvironment>();
        Shark.SharkOption.ExceptionHandlerOptions.IsDevelopment = env.IsDevelopment();
        app.UseSharkExceptionHandler();
    }
    app.MapEndpoints();
}
```

This is the minimal change. The exception handler middleware is inserted BEFORE `MapEndpoints()` so it wraps all endpoint invocations as well as any subsequent middleware.

---

### Task 5: UnifiedResult auto-wrap filter

**Files:**
- Create: `src/Sharkable/ExceptionHandler/UnifiedResultWrapFilter.cs`
- Modify: `src/Sharkable/Shark/Options/UseSharkOptions.cs`

- [ ] **Step 1: Create the endpoint filter**

```csharp
namespace Sharkable;

internal sealed class UnifiedResultWrapFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, 
        EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null || result is IResult)
            return result;

        var resultType = result.GetType();
        var unifiedResultType = typeof(UnifiedResult<>).MakeGenericType(resultType);
        var unifiedResult = Activator.CreateInstance(unifiedResultType);

        if (unifiedResult is not null)
        {
            var dataProperty = unifiedResultType.GetProperty("Data");
            dataProperty?.SetValue(unifiedResult, result);
            var statusCodeProperty = unifiedResultType.GetProperty("StatusCode");
            statusCodeProperty?.SetValue(unifiedResult, System.Net.HttpStatusCode.OK);
        }

        return unifiedResult;
    }
}
```

Note: This is a best-effort auto-wrap using reflection. It won't preserve the generic type properly in all scenarios. A more robust approach would require the user to consistently return a specific type from their endpoints, but for DX purposes this provides a reasonable default.

- [ ] **Step 2: Add EnableAutoWrap flag to UseSharkOptions**

Edit `src/Sharkable/Shark/Options/UseSharkOptions.cs`:

```csharp
/// <summary>
/// When true, endpoint return values that are not <see cref="IResult"/> are
/// automatically wrapped in <see cref="UnifiedResult{T}"/>.
/// Default is false (opt-in).
/// </summary>
public bool EnableAutoWrap { get; set; } = false;
```

- [ ] **Step 3: Wire auto-wrap filter in UseShark**

In `SharkableExtension.cs`, add after exception handler wiring:

```csharp
// Wire auto-wrap filter
if (Shark.UseSharkOptions?.EnableAutoWrap ?? false)
{
    // No built-in way to add a global filter in minimal APIs.
    // Instead, document that users add it per-group:
    // app.MapGroup("/api").AddEndpointFilter<UnifiedResultWrapFilter>();
}
```

Actually, .NET 8 minimal APIs don't have a built-in mechanism for global endpoint filters. The cleanest approach would be to add a helper extension:

Let me reconsider. For auto-wrap, I could modify `MapSharkEndpoints()` in `EndPointExtension.cs` to optionally add the filter when creating endpoint groups. That way all Shark-discovered endpoints get auto-wrap.

Edit the `MapSharkEndpoints` method where the group is created:

```csharp
var group = app.MapGroup(sharkEndpoint.baseApiPath)
    .WithDisplayName(groupName);

if (Shark.UseSharkOptions?.EnableAutoWrap ?? false)
{
    group.AddEndpointFilter<UnifiedResultWrapFilter>();
}
```

- [ ] **Step 4: Register UnifiedResultWrapFilter in DI**

The filter needs to be resolvable from DI. Add to the filter:

Wait — `IEndpointFilter` instances can be resolved from DI if registered. Or we can use `AddEndpointFilter<UnifiedResultWrapFilter>()` and register it. Actually, `AddEndpointFilter<T>()` where T : IEndpointFilter automatically resolves from DI. So we need to register it:

In `SharkExtension.cs` `AddCommon()`:

```csharp
// Only register if auto-wrap might be used
services.AddSingleton<UnifiedResultWrapFilter>();
```

But this registers it unconditionally. Better to register in `WireSharkEndpoint` when there are endpoints to wire. Actually, the simplest approach is to register it once in `AddCommon()`:

In `SharkExtension.cs` `AddCommon()`:
```csharp
services.AddSingleton<IEndpointFilter, UnifiedResultWrapFilter>();
```

Wait, let me reconsider. Instead of registering as `IEndpointFilter` (which could conflict with other filters), register as itself:

```csharp
services.AddSingleton<UnifiedResultWrapFilter>();
```

Hmm, but `AddEndpointFilter<T>()` actually needs the type to implement `IEndpointFilter` and be resolvable. Let me check... In ASP.NET Core, `RouteHandlerBuilder.AddEndpointFilter<T>()` requires `T` to implement `IEndpointFilter` and will be resolved from DI if registered, OR instantiated directly if it has a parameterless constructor.

Since `UnifiedResultWrapFilter` has no dependencies, we can just do `group.AddEndpointFilter<UnifiedResultWrapFilter>()` and it'll be created directly.

So no DI registration needed. Just update `EndPointExtension.cs` `MapSharkEndpoints`.

---

### Task 6: Verify build

- [ ] **Step 1: Build the main library**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeds with no errors

- [ ] **Step 2: Build the sample projects**

Run: `dotnet build src/Sharkable.Sample/Sharkable.Sample.csproj`
Run: `dotnet build src/Sharkable.AotSample/Sharkable.AotSample.csproj`
Run: `dotnet build src/Sharkable.NativeTest/Sharkable.NativeTest.csproj`
Expected: All build successfully

---

### Implementation order

1. Task 1: `ExceptionHandlerOptions`
2. Task 2: `SharkExceptionHandlerMiddleware` + update `UnifiedResultSourceContext`
3. Task 3: `ExceptionHandlerExtension` (`UseSharkExceptionHandler`)
4. Task 4: Wire into `UseSharkOptions`, `SharkOption`, and `UseShark()`
5. Task 5: `UnifiedResultWrapFilter` + wire into endpoint mapping
6. Task 6: Verify build

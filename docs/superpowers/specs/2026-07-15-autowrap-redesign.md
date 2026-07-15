# AutoWrap Redesign — Unified Result Interception

**Date:** 2026-07-15  
**Status:** draft  

## Motivation

`EnableAutoWrap` currently defaults to `false` — users must opt in to get unified response wrapping. Ideally the framework should produce consistent response shapes out of the box. Additionally, the existing `UnifiedResultWrapFilter` returns a bare `IUnifiedResult` POCO (not `IResult`), so:

- HTTP status code is not synced with `UnifiedResult.StatusCode`
- Both `UnifiedResult<object?>` and `Results.Json()` allocate — two heap objects per wrapped response
- No per-endpoint exclusion mechanism exists

## Goals

- **Default-on auto-wrapping**: `EnableAutoWrap` defaults to `true`, wrapping all plain return values in `UnifiedResult<T>`
- **HTTP status code sync**: wrapped response HTTP status code matches `UnifiedResult.StatusCode`
- **Reduced allocation**: custom internal `IResult` eliminates one heap allocation per wrapped response
- **Per-endpoint opt-out**: `[SharkDontWrap]` attribute (class-level) and `.DisableAutoWrap()` fluent API (route-level)
- **Backward compatible**: all existing `IUnifiedResultFactory` implementations and `UnifiedResult<T>` shapes unchanged

## Non-goals

- No new fields on `UnifiedResult<T>` (existing 5 fields unchanged)
- No middleware-based interception (stays on `IEndpointFilter`)
- No object pooling (left as future optimization)
- No change to `IUnifiedResult` interface

## Design

### 1. Custom IResult — `UnifiedResultResult`

Replaces the current pattern of "return bare `IUnifiedResult` POCO" + implicit ASP.NET serialization  
with an explicit `IResult` that sets HTTP status code and Content-Type in one step.

```
Before: filter returns IUnifiedResult → ASP.NET serializes → status code NOT synced
After:  filter returns UnifiedResultResult(IUnifiedResult).ExecuteAsync() → status code synced
```

```csharp
// New file: src/Sharkable/UnifiedReults/UnifiedResultResult.cs
namespace Sharkable;

internal sealed class UnifiedResultResult(IUnifiedResult data) : IResult
{
    public Task ExecuteAsync(HttpContext context)
    {
        context.Response.StatusCode = data.StatusCode;
        return context.Response.WriteAsJsonAsync(data, data.GetType());
    }
}
```

Compared to the alternative `Results.Json(wrapped, statusCode)` this saves one allocation  
because `Results.Json()` internally creates another `IResult` wrapper.

### 2. UnifiedResultWrapFilter rewrite

```csharp
internal sealed class UnifiedResultWrapFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var result = await next(context);

        if (result == null || result is IResult || result is IUnifiedResult)
            return result;

        var endpoint = context.HttpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<DisableAutoWrapMetadata>() is not null)
            return result;

        var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
        var wrapped = factory.Create(result, errorMessage: null, statusCode: 200);
        return new UnifiedResultResult(wrapped);
    }
}
```

Key changes from current:
- Check `DisableAutoWrapMetadata` on the endpoint
- Return `UnifiedResultResult` (IResult) instead of bare `IUnifiedResult`
- Status code for plain values defaults to 200

### 3. Per-endpoint exclusion

**Class-level attribute** — applied to `ISharkEndpoint` class, excludes the entire group:

```csharp
// New attribute
namespace Sharkable;

/// <summary>
/// Prevents auto-wrapping of return values for this endpoint class.
/// When applied to an ISharkEndpoint, ALL routes under it will skip auto-wrap.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SharkDontWrapAttribute : Attribute { }
```

Implementation: in `MapSharkEndpoints`, before adding `UnifiedResultWrapFilter` to the group,  
check if the endpoint class has `[SharkDontWrap]`.

**Route-level fluent API** — per-route exclusion:

```csharp
// New file: src/Sharkable/UnifiedReults/Extensions/DisableAutoWrapExtensions.cs
namespace Sharkable;

public static class DisableAutoWrapExtensions
{
    /// <summary>Excludes this route from auto-wrap.</summary>
    public static RouteHandlerBuilder DisableAutoWrap(this RouteHandlerBuilder builder)
    {
        builder.Add(endpointBuilder =>
            endpointBuilder.Metadata.Add(new DisableAutoWrapMetadata()));
        return builder;
    }
}

internal sealed class DisableAutoWrapMetadata { }
```

Usage:

```csharp
app.MapGet("raw", () => "plain text").DisableAutoWrap();
```

`DisableAutoWrapMetadata` is an internal marker class — no public API surface.

### 4. EnableAutoWrap default change

```diff
// SharkOption.cs
- public bool EnableAutoWrap { get; set; } = false;
+ public bool EnableAutoWrap { get; set; } = true;
```

`EndPointExtension.cs` — the group filter is added whenever `EnableAutoWrap` is true AND the  
endpoint class does NOT have `[SharkDontWrap]`:

```csharp
// In MapSharkEndpoints, per-group:
var hasDontWrap = classType.GetCustomAttribute<SharkDontWrapAttribute>() != null;
if (Shark.SharkOption.EnableAutoWrap && !hasDontWrap)
    group.AddEndpointFilter<UnifiedResultWrapFilter>();
```

The `EnableAutoWrap` check stays in `EndPointExtension.cs` rather than inside the filter,  
because the filter is added at `MapGroup` creation (startup) — cheaper to skip adding the filter  
than to add it and check metadata per-request.

Note: `UseSharkOptions.EnableAutoWrap` still provides a runtime override. The final effective  
value is `SharkOption.EnableAutoWrap || UseSharkOptions.EnableAutoWrap`, except `UseSharkOptions`  
cannot *disable* auto-wrap if `SharkOption` already enables it. For this redesign, the group  
filter check uses:
```csharp
var autoWrap = Shark.SharkOption.EnableAutoWrap || (Shark.UseSharkOptions?.EnableAutoWrap ?? false);
```
(The `||` semantics match the existing code — either can enable.)

For the `[SharkDontWrap]` class-level exclusion, it is ANDed with the auto-wrap check.

### 5. ProblemDetailsResult reuses UnifiedResultResult

Current `ProblemDetailsResult.WriteAsync` manually calls `WriteAsJsonAsync`. Update to use  
`UnifiedResultResult` for consistency:

```csharp
internal static async Task WriteAsync(HttpContext ctx, int statusCode, string detail)
{
    // ... existing ProblemDetails path unchanged ...

    // Unified result path — use custom IResult
    var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
    var result = factory.Create(data: null, errorMessage: detail, statusCode: statusCode);
    await new UnifiedResultResult(result).ExecuteAsync(ctx);
}
```

### 6. Files summary

| File | Action | Lines changed |
|------|--------|---------------|
| `SharkOption.cs` | Change default | 1 line |
| `UnifiedResultWrapFilter.cs` | Rewrite filter logic | ~15 lines |
| `EndPointExtension.cs` | Add `[SharkDontWrap]` check | ~2 lines |
| `ProblemDetailsResult.cs` | Use `UnifiedResultResult` | ~3 lines |
| **NEW** `UnifiedResultResult.cs` | Custom IResult | ~10 lines |
| **NEW** `DisableAutoWrapExtensions.cs` | Fluent API + attr + metadata | ~25 lines |

Total: ~55 lines of new/changed code across 6 files.

## Verification

- NativeTest project already sets `EnableAutoWrap = true` in Program.cs — verify build and run
- Confirm plain return values (e.g., `return products.ToList()`) are wrapped in `UnifiedResult<object?>`
- Confirm `.DisableAutoWrap()` route returns raw value unwrapped
- Confirm `[SharkDontWrap]` class has all routes unwrapped
- Confirm `IResult` and `IUnifiedResult` returns are not double-wrapped
- Confirm HTTP response status code matches the serialized `statusCode` field
- Confirm `dotnet build src/Sharkable/Sharkable.csproj` passes

# Endpoint Grouping + OpenAPI Tags Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add OpenAPI tags, OperationId auto-generation, and explicit URL prefix grouping to Sharkable endpoints.

**Architecture:** Three attributes (`[SharkTag]`, `[EndpointGroup]`) + refactor `MapSharkEndpoints()` to merge groups by name + `RouteGroupBuilder.Finally()` for post-processing metadata injection.

**Tech Stack:** .NET 10, ASP.NET Core, `Microsoft.AspNetCore.OpenApi`, `Scalar.AspNetCore`

---

### Task 1: Create `SharkTagAttribute`

**Files:**
- Create: `src/Sharkable/SharkEndpoint/Attributes/SharkTagAttribute.cs`
- No test project exists; verify via build + runtime test

- [ ] **Step 1: Create the attribute**

`src/Sharkable/SharkEndpoint/Attributes/SharkTagAttribute.cs`:

```csharp
namespace Sharkable;

/// <summary>
/// Overrides the OpenAPI tag for an endpoint class. Repeatable for multiple tags.
/// When absent, the tag is derived from the group name.
/// </summary>
/// <param name="tag">The OpenAPI tag value.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SharkTagAttribute(string tag) : Attribute
{
    /// <summary>The OpenAPI tag value.</summary>
    public string Tag { get; } = tag;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeded (0 errors)

### Task 2: Create `EndpointGroupAttribute`

**Files:**
- Create: `src/Sharkable/SharkEndpoint/Attributes/EndpointGroupAttribute.cs`

- [ ] **Step 1: Create the attribute**

`src/Sharkable/SharkEndpoint/Attributes/EndpointGroupAttribute.cs`:

```csharp
namespace Sharkable;

/// <summary>
/// Explicitly assigns an endpoint class to a URL prefix group.
/// Multiple classes with the same group name share the same route prefix and OpenAPI tag.
/// When absent, the group name is derived from the class name.
/// </summary>
/// <param name="name">The group name (becomes the URL prefix segment and default OpenAPI tag).</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EndpointGroupAttribute(string name) : Attribute
{
    /// <summary>The group name.</summary>
    public string Name { get; } = name;
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeded (0 errors)

### Task 3: Refactor `MapSharkEndpoints()` — group merging

**Files:**
- Modify: `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs`

- [ ] **Step 0: Add required using directives**

At the top of `EndPointExtension.cs`, replace existing usings with:

```csharp
using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Options;
```

- `System.Linq` — needed for `.Any()`, `.Select()`, `.Distinct()`, `.ToList()`
- `Microsoft.AspNetCore.Http.Metadata` — needed for `EndpointNameMetadata`
- `Microsoft.AspNetCore.Routing` — needed for `RouteEndpointBuilder`, `HttpMethodMetadata`, `RouteGroupBuilder`

- [ ] **Step 1: Add `[EndpointGroup]` support to `CreateSharkEndpoint()`**

After deriving `instance.groupName` (either from `SharkEndpointAttribute` or class name), check for `[EndpointGroup]` and override:

```csharp
// At end of CreateSharkEndpoint, before return instance:

var endpointGroupAttr = shark.GetType().GetCustomAttribute<EndpointGroupAttribute>();
if (endpointGroupAttr != null)
{
    instance.groupName = endpointGroupAttr.Name;
    instance.addPrefix = !string.IsNullOrWhiteSpace(apiPrefix);
}
```

Insert right before `instance.apiPrefix = apiPrefix;` (line ~143).

- [ ] **Step 2: Refactor `MapSharkEndpoints()` — collect then group**

Replace the current single-pass iteration with a two-phase approach:

```csharp
internal static WebApplication MapSharkEndpoints(this WebApplication? app)
{
    ArgumentNullException.ThrowIfNull(app);

    var endpointServices = app.Services.GetServices<ISharkEndpoint>();
    var options = app.Services.GetService<IOptions<SharkOption>>();
    ArgumentNullException.ThrowIfNull(options);

    // Phase 1: Collect all SharkEndpoint instances with metadata
    var collected = new List<(SharkEndpoint endpoint, Type classType)>();
    endpointServices.MyForEach(e =>
    {
        SharkEndpoint sharkEndpoint;

        if (e is SharkEndpoint endpoint)
        {
            sharkEndpoint = endpoint;
            sharkEndpoint.BuildAction = endpoint.AddRoutes;
        }
        else
        {
            sharkEndpoint = CreateSharkEndpoint(e);
        }

        // Check for [EndpointGroup] on interface-typed endpoints too
        var groupAttr = e.GetType().GetCustomAttribute<EndpointGroupAttribute>();
        if (groupAttr != null)
        {
            sharkEndpoint.groupName = groupAttr.Name;
        }

        if (string.IsNullOrWhiteSpace(sharkEndpoint.apiPrefix))
            sharkEndpoint.apiPrefix = options.Value.ApiPrefix;

        collected.Add((sharkEndpoint, e.GetType()));
    });

    // Phase 2: Group by resolved group name
    var grouped = new Dictionary<string, List<(SharkEndpoint, Type)>>();
    collected.MyForEach(item =>
    {
        var groupName = item.endpoint.groupName?.GetCaseFormat(options.Value.Format) ?? string.Empty;
        if (!grouped.ContainsKey(groupName))
            grouped[groupName] = [];
        grouped[groupName].Add(item);
    });

    // Phase 3: Create one MapGroup per unique group name
    foreach (var (groupName, endpoints) in grouped)
    {
        var first = endpoints.First().endpoint;

        if (string.IsNullOrWhiteSpace(first.apiPrefix))
        {
            endpoints.MyForEach(ep => ep.endpoint.BuildAction?.Invoke(app));
            continue;
        }

        var basePath = string.IsNullOrWhiteSpace(groupName)
            ? first.apiPrefix
            : $"{first.apiPrefix}/{groupName}";

        var group = app.MapGroup(basePath).WithDisplayName(groupName);

        // Shared filters (applied once per group)
        if (Shark.UseSharkOptions?.EnableAutoWrap ?? false)
            group.AddEndpointFilter<UnifiedResultWrapFilter>();
        if (Shark.SharkOption.EnableValidation)
            group.AddEndpointFilter<ValidationFilter>();

        // Resolve tags
        var tags = ResolveGroupTags(endpoints, groupName);
        if (tags.Count != 0)
            ((RouteGroupBuilder)group).WithTags([.. tags]);

        // Call AddRoutes for each endpoint in the group
        endpoints.MyForEach(ep => ep.endpoint.BuildAction?.Invoke(group));
    }

    return app;
}

private static List<string> ResolveGroupTags(List<(SharkEndpoint, Type)> endpoints, string defaultTag)
{
    var tags = endpoints
        .SelectMany(ep => ep.Item2.GetCustomAttributes<SharkTagAttribute>())
        .Select(attr => attr.Tag)
        .Distinct()
        .ToList();

    if (tags.Count == 0)
        tags.Add(defaultTag);

    return tags;
}
```

Note: The `((RouteGroupBuilder)group).WithTags([.. tags])` cast is needed because `MapGroup` returns `RouteGroupBuilder` via `IEndpointRouteBuilder` interface which doesn't expose `WithTags`. The underlying object is always a `RouteGroupBuilder`.

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeded (0 errors)

### Task 4: Add OperationId auto-generation

**Files:**
- Modify: `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs`

- [ ] **Step 1: Add `Finally()` callback for OperationId**

In the Phase 3 loop, before calling `BuildAction`, add a `Finally()` convention:

```csharp
// After filter setup, before BuildAction:
var capturedGroupName = groupName;
group.Finally(builder =>
{
    if (builder.Metadata.Any(m => m is EndpointNameMetadata))
        return;

    var routePattern = (builder as RouteEndpointBuilder)?.RoutePattern?.RawText ?? "unknown";
    var httpMethod = builder.Metadata
        .OfType<HttpMethodMetadata>()
        .FirstOrDefault()?.HttpMethods?.FirstOrDefault() ?? "Unknown";

    var opId = $"{capturedGroupName}_{httpMethod}_{routePattern}";
    builder.Metadata.Add(new EndpointNameMetadata(opId));
});
```

Insert right before `endpoints.MyForEach(ep => ep.endpoint.BuildAction?.Invoke(group));`.

Add required using directives at top of file if not present:
```csharp
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
```

- [ ] **Step 2: Add OperationId to old-style attribute endpoints**

In `MapAttributeEndpoints()`, modify the `MapMethods()` call:

```csharp
group.MapMethods(methodAttribute.Pattern!, [methodAttribute.Method.ToString()], methodDelegate)
     .WithOperationId($"{t.Name}_{methodInfo.Name}");
```

Replace existing `group.MapMethods(methodAttribute.Pattern!, [methodAttribute.Method.ToString()], methodDelegate);` with the above.

- [ ] **Step 3: Build to verify**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeded (0 errors)

### Task 5: Add OpenAPI tags to old-style endpoints

**Files:**
- Modify: `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs`

- [ ] **Step 1: Add `WithTags` to `MapAttributeEndpoints()`**

In the old-style attribute mapping, after creating the `group`:

```csharp
var group = app.MapGroup(endpointAttribute.Group!);

// Resolve tags for this endpoint class (support multiple [SharkTag])
var tagAttrs = t.GetCustomAttributes<SharkTagAttribute>();
var tags = tagAttrs.Any()
    ? tagAttrs.Select(a => a.Tag).ToArray()
    : [endpointAttribute.Group!];
group.WithTags(tags);
```

Find the `var group = app.MapGroup(endpointAttribute.Group!);` line (currently line 188) and add the tag logic after it.

- [ ] **Step 2: Build to verify**

Run: `dotnet build src/Sharkable/Sharkable.csproj`
Expected: Build succeeded (0 errors)

### Task 6: Full build + runtime verification

- [ ] **Step 1: Build all**

Run: `dotnet build`
Expected: Build succeeded (0 errors, only NU1902 warnings from Sample project's transitive deps)

- [ ] **Step 2: Run NativeTest and verify**

```bash
cd /Volumes/Doc/dev/Sharkable
dotnet run --project src/Sharkable.NativeTest/ --urls "http://localhost:5111" &
sleep 4
# Test endpoint response
curl -s http://localhost:5111/api/test/hello
# Test OpenAPI spec (check for tags field)
curl -s http://localhost:5111/openapi/v1.json | python3 -m json.tool | grep -A5 '"tags"'
# Test Scalar UI
curl -s -o /dev/null -w "%{http_code}" http://localhost:5111/scalar/v1
kill %1 2>/dev/null
wait 2>/dev/null
```

Expected: endpoint returns 200, OpenAPI spec has tags array, Scalar returns 200.

- [ ] **Step 3: Commit**

```bash
git add src/Sharkable/SharkEndpoint/Attributes/SharkTagAttribute.cs
git add src/Sharkable/SharkEndpoint/Attributes/EndpointGroupAttribute.cs
git add src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs
git commit -m "feat: add endpoint grouping, OpenAPI tags, and auto OperationId"
```

## Self-Review Checklist

1. **Spec coverage**: Every spec requirement has a task:
   - `[SharkTag]` attribute → Task 1 + Task 3 (tag resolution) + Task 5 (old-style)
   - Auto-tag from group name → Task 3 (`ResolveGroupTags` default)
   - OperationId via `Finally()` → Task 4
   - `[EndpointGroup]` attribute → Task 2 + Task 3 (group merging)
   - Multiple classes sharing URL prefix → Task 3 (Phase 2+3)
   - Old-style endpoint tags → Task 5
   - Old-style endpoint OperationId → Task 4 Step 2

2. **No placeholders**: All code is complete and typed explicitly.

3. **Type consistency**: `SharkTagAttribute.Tag` (string, singular) used throughout. `EndpointGroupAttribute.Name` used consistently.

# Endpoint Grouping + OpenAPI Tags Design

Date: 2026-06-08
Feature: C (from `docs/2026-06-08-dx-features-brainstorm.md`)
Branch: `feat/endpoint-grouping`

## Summary

Add OpenAPI tags, OperationId auto-generation, and explicit URL prefix grouping to Sharkable endpoints. Approach 1 (minimal intrusive — attributes + conventions).

## 1. OpenAPI Tags

### Auto-tag

Every endpoint group automatically gets `WithTags(groupName)`:

- New-style `ISharkEndpoint`: applied via `RouteGroupBuilder.Finally()` in `MapSharkEndpoints()`
- Old-style `[SharkEndpoint]`: applied in `MapAttributeEndpoints()` on each group

### Override via `[SharkTag]`

```
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SharkTagAttribute(params string[] tags) : Attribute
```

- Class-level, repeatable
- When present: replaces auto tag (group name) with specified tag(s)
- Scanned in `MapSharkEndpoints()` / `MapAttributeEndpoints()` before applying `WithTags()`

### Tag application logic

```
if class has [SharkTag("...")]
    use those tags
else
    use groupName as tag
```

## 2. OperationId Auto-Generation

Applied via `RouteGroupBuilder.Finally()` in `MapSharkEndpoints()` (new-style) and in `MapAttributeEndpoints()` (old-style).

### Pattern

`{ClassName}_{MethodName}`

- `ClassName`: full class name (without namespace)
- `MethodName`: resolved from the HTTP method + first path segment

### Implementation in new-style endpoints

```csharp
group.Finally(builder =>
{
    // auto tag (skip if already set)
    if (!builder.Metadata.Any(m => m is ITagsMetadata))
    {
        var tags = ResolveTags(sharkEndpointType, groupName);
        foreach (var tag in tags)
            builder.Metadata.Add(new TagMetadata(tag));
    }

    // auto operationId
    if (!builder.Metadata.Any(m => m is IOperationIdMetadata))
    {
        var methodName = ResolveOperationId(builder);
        builder.DisplayName = $"{className}_{methodName}";
    }
});
```

- `ITagsMetadata` / `IOperationIdMetadata`: checked before adding, so manual `WithTags()` / `WithOperationId()` in user code is respected and not overwritten
- `Finally()` runs when the app builds (not at Map-time), so all routes from `AddRoutes()` are visible

### Implementation in old-style endpoints

`WithOperationId()` added directly to `MapMethods()` call in `MapAttributeEndpoints()`.

## 3. Explicit URL Prefix Grouping (分组前缀)

### Problem

Each `ISharkEndpoint` class currently gets its own `MapGroup()`. Multiple classes cannot share a URL prefix.

### Solution — `[EndpointGroup]`

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class EndpointGroupAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}
```

### Routing logic change in `MapSharkEndpoints()`

Before: iterate each endpoint → create one `MapGroup()` → call `AddRoutes()`.

After:

```
1. Collect all SharkEndpoint instances
2. Group by resolved group name (from [EndpointGroup] or class name derivation)
3. For each unique group name:
   a. Create one MapGroup(prefix)
   b. Apply filters (auto-wrap, validation) once
   c. Apply OpenAPI tag once (from [SharkTag] or group name)
   d. Apply Finally() callback once
   e. Call AddRoutes() for EACH endpoint in the group on the same MapGroup
```

### Key effect

Multiple `ISharkEndpoint` classes with the same `[EndpointGroup("same-name")]` will:
- Share a single URL prefix (`api/same-name/...`)
- Share the same OpenAPI tag
- Be mergable under the same middleware/filter pipeline
- Each class's `AddRoutes()` is still called independently on the same `RouteGroupBuilder`

### No attribute

If no `[EndpointGroup]` attribute: behavior unchanged — group name derived from class name, each class gets its own unique group.

## Files to change

| File | Change |
|------|--------|
| `SharkEndpoint/Attributes/SharkTagAttribute.cs` | NEW — `[SharkTag]` attribute |
| `SharkEndpoint/Attributes/EndpointGroupAttribute.cs` | NEW — `[EndpointGroup]` attribute |
| `SharkEndpoint/Extensions/EndPointExtension.cs` | Refactor `MapSharkEndpoints()` — merge groups by name, add `Finally()` for tags + OperationId; add tag support to `MapAttributeEndpoints()` |
| `Constants/EndpointFormat.cs` | No change |
| `SharkEndpoint/ISharkEndpoint.cs` | No change (but update XML doc mentioning group behavior) |
| `SharkEndpoint/SharkEndpoint.cs` | No change (Fields stay; groupName still populated) |

## Backward compatibility

- Existing code: zero breakage. Behavior identical when no new attributes are used.
- New attributes are opt-in.
- Old-style `[SharkEndpoint]` endpoints get auto-tag support for free.

## AOT compatibility

- No reflection added beyond existing patterns
- `RouteGroupBuilder.Finally()` is a standard AOT-safe API
- `[SharkTag]` and `[EndpointGroup]` attributes are static metadata, no new reflection

## Verification

1. Single endpoint without attributes: tag = groupName, URL prefix = class-derived
2. `[SharkTag("custom")]` on class: tag = "custom" in OpenAPI spec
3. `[EndpointGroup("admin")]` on 2 classes: both under same URL prefix, same tag
4. `[SharkTag("a","b")]` on class with `[EndpointGroup("admin")]`: tag = ["a", "b"], URL = `api/admin/...`
5. Auto OperationId appears in OpenAPI spec for each route
6. Manual `.WithTags()` / `.WithOperationId()` in user code: not overwritten

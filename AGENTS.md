# Sharkable — AGENTS.md

.NET 10 minimal API framework collection. NuGet package. Author: CharleyPeng.

## Coding standards

- All public API properties, methods, and classes **must** have XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`)
- Internal classes and methods should also have brief XML doc comments explaining purpose
- `SuppressMessage` attributes require a comment explaining why
- Only modify files and lines strictly required by the task — no incidental refactoring, reformatting, or unrelated changes
- Do not introduce new NuGet packages without justification — if needed, state the reason and ask first; only add after receiving explicit approval

## Solution layout

| Project | Path | Notes |
|---|---|---|
| Sharkable (lib) | `src/Sharkable/` | NuGet package, `Microsoft.NET.Sdk` |
| Sharkable.Sample | `src/Sharkable.Sample/` | Reference usage |
| Sharkable.AotSample | `src/Sharkable.AotSample/` | `PublishAot=true` |
| Sharkable.NativeTest | `src/Sharkable.NativeTest/` | `PublishAot=true` |

## Build & run

```bash
dotnet build
dotnet build src/Sharkable/Sharkable.csproj
dotnet pack src/Sharkable/Sharkable.csproj
dotnet run --project src/Sharkable.Sample/
dotnet run --project src/Sharkable.AotSample/
```

No test project exists. No linter/formatter beyond compiler warnings.

## Project conventions

- **All namespaces are `Sharkable`** — flat, not matching folder structure. `IDE0130` suppressed globally.
- `partial class Utils` is split across files in `Utils/`. Never add a new file to `Utils/` without reading existing ones.
- `EditorConfig` silences `CA1822` and `IDE0160`.

## Entry points (call order matters)

```csharp
// 1. Register services + discover endpoints + wire DI + OpenAPI
builder.Services.AddShark();                              // auto-discover assemblies
builder.Services.AddShark(opt => { ... });                 // with options
builder.Services.AddShark([typeof(Program).Assembly]);     // AOT: explicit assemblies
builder.Services.AddShark([typeof(Program).Assembly], opt => { ... });

// 2. After builder.Build():
app.UseShark();                                            // wire endpoints + OpenAPI + Scalar UI

// 3. Maps endpoints (called by UseShark internally)
app.MapSharkEndpoints();                                   // ISharkEndpoint implementations
```

`UseShark()` must be called before `app.Run()`. OpenAPI/Scalar is served at `/openapi/v1.json` and `/scalar/v1` by default.

Configure OpenAPI via `SharkOption.ConfigureOpenApi()`:
```csharp
builder.Services.AddShark(opt =>
{
    opt.ConfigureOpenApi(options => { /* configure OpenAPI options */ });
});
```

## Two endpoint styles

### New style (recommended, AOT-safe)
```csharp
public class TestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("hello", () => Results.Ok("hi"));
    }
}
```
URL becomes `api/test/hello` (prefix derived from class name, suffix `Endpoint`/`Service`/`Controller` stripped).

### Old style (attribute-based, NOT AOT-compatible)
```csharp
[SharkEndpoint]
public class TestEndpoint
{
    [SharkMethod("show", SharkHttpMethod.GET)]
    public void Show() { }
}
```
Requires `IDependencyReflectorFactory` + reflection. Only works when `SharkOption.AotMode == false`.

## Endpoint URL conventions

- Default prefix: `api` (configurable via `SharkOption.ApiPrefix`)
- Group name from class: strips trailing `Endpoint`/`Service`/`Controller`/`ApiController` (case-insensitive), converts `V\d+` to `@\d+`
- Format applied via `EndpointFormat` enum: `CamelCase` (default), `SnakeCase`, `ToLower`, `UnChanged`
- Set via: `opt.Format = EndpointFormat.SnakeCase` in `AddShark()` callback

## AOT requirements

- Pass assemblies explicitly: `builder.Services.AddShark([typeof(Program).Assembly])`
- Register `JsonSerializerContext` for source-generated serialization
- Attribute-based endpoints (`[SharkEndpoint]` + `[SharkMethod]`) will NOT work in AOT

## DI via attributes

Mark classes with `[ScopedService]`, `[TransientService]`, `[SingletonService]` to auto-register.

## Documentation site

- Docs repo: `~/dev/sharkableio.github.io/docs/` (docsify site)
- English: `docs/` root, Chinese: `docs/zh-cn/`
- New features need both EN and ZH docs + sidebar updates

## Changelog

- `CHANGELOG.md` at repo root **must** be updated in every PR, before merging
- English only; one entry per logical change under the appropriate version section
- Format: `- {type}: {description}`, where `{type}` is `feat`, `fix`, `refactor`, `test`, `docs`, or `chore`

## Release process

When told to bump version and publish a new release, execute the following steps **in order**:

1. **Determine the new version** from context (patch/minor/major). Read the current `<AssemblyVersion>` from `src/Sharkable/Sharkable.csproj`.
2. **Update `src/Sharkable/Sharkable.csproj`** — bump both `<AssemblyVersion>` and `<Version>` to the new version.
3. **Update `src/Sharkable/Sharkable.nuspec`** — bump `<version>` to the new version.
4. **Move `CHANGELOG.md` unreleased entries** to a new version section with today's date (e.g. `## [0.3.0] — 2026-06-27`).
5. **Update docs site QuickStart** (`~/dev/sharkableio.github.io/docs/quickstart.md` and `~/dev/sharkableio.github.io/docs/zh-cn/quickstart.md`) — replace the old NuGet version in the `dotnet add package` command with the new version.
6. **Commit all changes** to the Sharkable repo with message `chore: bump version to x.y.z`.
7. **Tag the release** — `git tag vx.y.z && git push origin vx.y.z`.
8. **Push the commit** — `git push`.
9. **Publish to NuGet** — `dotnet pack src/Sharkable/Sharkable.csproj -c Release && dotnet nuget push src/Sharkable/bin/Release/Sharkable.x.y.z.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json`.
   - The NuGet API key is assumed to be available in the environment. If not, ask the user.
10. **Update docs site repo** — commit and push the QuickStart changes there too.

# Sharkable — AGENTS.md

.NET 8 minimal API framework collection. NuGet package. Author: CharleyPeng.

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
// 1. Register services + discover endpoints + wire DI + swagger
builder.Services.AddShark();                              // auto-discover assemblies
builder.Services.AddShark(opt => { ... });                 // with options
builder.Services.AddShark([typeof(Program).Assembly]);     // AOT: explicit assemblies
builder.Services.AddShark([typeof(Program).Assembly], opt => { ... });

// 2. After builder.Build():
app.UseShark();                                            // wire endpoints + swagger
app.UseShark(opt => opt.ConfigureSwaggerOptions(s => { }));

// 3. Maps endpoints (called by UseShark internally)
app.MapSharkEndpoints();                                   // ISharkEndpoint implementations
```

`UseShark()` must be called before `app.Run()`.

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

## Branch & PR

- `main` requires PR — direct push rejected by repo rule
- After a PR is merged, sync local main: `git checkout main && git pull`
- If a feature branch is behind, rebase: `git checkout <branch> && git rebase origin/main && git push --force-with-lease origin <branch>`

## Documentation site

- Docs repo: `~/dev/sharkableio.github.io/docs/` (docsify site)
- English: `docs/` root, Chinese: `docs/zh-cn/`
- New features need both EN and ZH docs + sidebar updates

## NuGet publishing

- Version lives in `src/Sharkable/Sharkable.csproj` (`<AssemblyVersion>`) and `src/Sharkable/Sharkable.nuspec`
- Package icon: `src/Sharkable/sharkable.jpg`

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
- **Documentation language**: all project docs (ROADMAP, specs, plans, design docs) must be written in English — this is a global open-source project.

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

## ISharkEndpoint class-level OpenAPI metadata

Apply attributes on `ISharkEndpoint` classes to enrich the generated OpenAPI document:

| Attribute | Purpose | OpenAPI effect |
|---|---|---|
| `[SharkDescription("summary","description")]` | Set default summary/description | `summary` / `description` on all operations in the group |
| `[SharkResponseType(statusCode, typeof(T), "description")]` | Add response metadata | Additional response entries (repeatable) |
| `[SharkDeprecated]` | Mark as deprecated | Adds `ObsoleteAttribute` to endpoint metadata |
| `[SharkTag("tag")]` | Override OpenAPI tag | Replaces the auto-derived group name tag (repeatable) |

Individual per-endpoint overrides (`.WithSummary()`, `.WithDescription()`, `.WithOpenApi()`) take precedence.

## AOT requirements

- Pass assemblies explicitly: `builder.Services.AddShark([typeof(Program).Assembly])`
- Register `JsonSerializerContext` for source-generated serialization
- Attribute-based endpoints (`[SharkEndpoint]` + `[SharkMethod]`) will NOT work in AOT

## DI via attributes

Mark classes with `[ScopedService]`, `[TransientService]`, `[SingletonService]` to auto-register.

## Documentation site (Docusaurus)

- Docs repo: `~/dev/sharkableio.github.io/` (Docusaurus site)
- **Docusaurus versioning** — docs are version-snapshot-based. Every release has frozen docs under `versioned_docs/`; `docs/` is the latest (unreleased) version.
- English docs: `docs/` (current), `versioned_docs/version-<label>/` (released versions)
- Chinese docs (zh-cn): `i18n/zh-cn/docusaurus-plugin-content-docs/current/` (current), `i18n/zh-cn/docusaurus-plugin-content-docs/version-<label>/` (released versions)
- Sidebars: `sidebars.js` (current), `versioned_sidebars/version-<label>-sidebars.json` (released versions)
- **When adding new pages**, add files to `docs/` AND mirror in `versioned_docs/version-<label>/`; update both `sidebars.js` and `versioned_sidebars/version-<label>-sidebars.json`; translate in both `i18n/zh-cn/.../current/` and `i18n/zh-cn/.../version-<label>/`
- **Sidebar category labels must be fully translated** — every new or renamed sidebar category in `sidebars.js` must have a corresponding `"sidebar.docs.category.<Name>"` entry in ALL applicable ZH JSON files: `i18n/zh-cn/docusaurus-plugin-content-docs/current.json` AND `i18n/zh-cn/docusaurus-plugin-content-docs/version-<label>.json`. Missing a category translation in any version causes the EN label to appear in the ZH sidebar for that version.
- **Versioned docs are frozen release snapshots** — they must reflect the state at release time. Never backport version bumps or new features into versioned docs. The `QuickStart` version number in versioned docs stays at its release version forever.
- **When cutting a new version**, run `npm run docusaurus docs:version <label>` to snapshot `docs/` + `sidebars.js`; then verify and fix the ZH version label in `i18n/zh-cn/.../version-<label>.json` (Docusaurus defaults it to `"Next"` — must change to `"<label>"`); then add any missing sidebar category translations for the new version.

## Changelog

- `CHANGELOG.md` at repo root **must** be updated in every PR, before merging
- English only; one entry per logical change under the appropriate version section
- Format: `- {type}: {description}`, where `{type}` is `feat`, `fix`, `refactor`, `test`, `docs`, or `chore`

## Release process

**IMPORTANT: Never publish to NuGet without explicit permission.** This includes both the main `Sharkable` package and any plugin packages (e.g., `Sharkable.Cache.Redis`). Only run `dotnet nuget push` when explicitly instructed.

When told to bump version and publish a new release, execute the following steps **in order**:

0. **Commit all feature/fix work first** — `git status` must be clean before starting the release. Do NOT bundle feature changes with the version bump commit. The version bump commit should only touch version numbers, CHANGELOG, and docs QuickStart.

1. **Determine the new version** from context (patch/minor/major). Read the current `<AssemblyVersion>` from `src/Sharkable/Sharkable.csproj`.
2. **Update `src/Sharkable/Sharkable.csproj`** — bump both `<AssemblyVersion>` and `<Version>` to the new version.
3. **Update `src/Sharkable/Sharkable.nuspec`** — bump `<version>` to the new version.
4. **Move `CHANGELOG.md` unreleased entries** to a new version section with today's date (e.g. `## [0.3.0] — 2026-06-27`).
5. **Update docs site QuickStart** — replace **both** the `dotnet add package` and `<PackageReference>` version numbers in the **current** docs only (2 copies): `docs/quickstart.md` (EN) and `i18n/zh-cn/docusaurus-plugin-content-docs/current/quickstart.md` (ZH). Versioned docs (`versioned_docs/version-<label>/` and `i18n/zh-cn/.../version-<label>/`) are frozen release snapshots — **never change their version numbers**. When a new version is later cut via `npm run docusaurus docs:version`, the current docs (already carrying the new version) are snapshot automatically.
6. **Commit all changes** to the Sharkable repo with message `chore: bump version to x.y.z`.
7. **Tag the release** — `git tag vx.y.z && git push origin vx.y.z`.
8. **Push the commit** — `git push`.
9. **Publish to NuGet** — `dotnet pack src/Sharkable/Sharkable.csproj -c Release && dotnet nuget push src/Sharkable/bin/Release/Sharkable.x.y.z.nupkg --api-key <key> --source https://api.nuget.org/v3/index.json`.
   - The NuGet API key is assumed to be available in the environment. If not, ask the user.
10. **Update docs site repo** — after bumping version, ensure docs site (`~/dev/sharkableio.github.io/`) has all pending changes committed and pushed (at minimum the QuickStart version update, plus any new docs for the release).

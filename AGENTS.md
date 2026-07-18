# Sharkable — AGENTS.md

.NET 10 minimal API framework collection. NuGet package. Author: CharleyPeng.

## Design principles (every feature must satisfy all three)

Sharkable is a **general-purpose framework** used by many developers in their own services. Every feature, API, and new behavior must be designed from the **user's perspective**:

### 1. Convenience
- **Discoverable**: Public API surface must be obvious — `ConfigureXxx(Action<XxxOptions>)` pattern, attributes with clear names, DSL extension methods.
- **Sensible defaults**: Every option defaults to a safe, production-appropriate value. Users should get correct behavior without writing any configuration.
- **Minimal boilerplate**: One line to enable a feature (e.g., `opt.EnableETag = true`). Complex configuration is opt-in.
- **Consistent patterns**: Follow existing conventions — `ConfigureXxx()` delegate pattern, `XxxFactory` for DI replacement, attributes on `ISharkEndpoint` classes. Never introduce a new pattern when an existing one already serves the purpose.

### 2. Minimal Intrusion
- **Opt-in by default**: Adding a new feature must not change any existing behavior unless the user explicitly enables it. `EnableXxx = false` must produce identical output to the feature not existing at all.
- **No hijacking of shared services**: Never register a framework implementation against a broad interface (e.g., `IMemoryCache`) shared with user code. Own your own abstractions.
- **No silent response shape changes**: Never modify response bodies, status codes, or headers without explicit user opt-in.
- **Pipeline discipline**: Middleware must be conditional — registered only when the feature is enabled. An unconfigured feature adds zero overhead to the request path.
- **Backward compatible**: Existing user code must continue to compile and behave identically after upgrading. Breaking changes are batched into minor versions with migration docs.

### 3. Customizability
- **Factory pattern for every store/service**: Every built-in implementation (`IIdempotencyStore`, `IDistributedRateLimitStore`, `ISagaStore`, `ICronJobStore`, `ISharkMetrics`, `IErrorLocalizer`, `IAuditSink`, `IAuthorizationInterceptor`) exposes a `Func<IServiceProvider, T>` factory on `SharkOption`. Users replace the default without touching DI directly.
- **Public interfaces, internal implementations**: Every replaceable component has a public interface. The default implementation is `internal sealed`. Users can `new()` their own without reverse-engineering.
- **Attributes AND DSL for endpoint-level overrides**: Users who prefer annotations use `[SharkRateLimit(10, 60)]`; users who prefer fluent code use `.SharkRateLimit(10, 60)`. Both produce the same metadata.
- **Full OpenAPI integration**: Every new middleware that affects response semantics (error shapes, status codes, headers) must be reflected in the generated OpenAPI document or must not silently hide behavior from API consumers.

## Coding standards

- All public API properties, methods, and classes **must** have XML doc comments (`/// <summary>`, `/// <param>`, `/// <returns>`)
- Internal classes and methods should also have brief XML doc comments explaining purpose
- `SuppressMessage` attributes require a comment explaining why
- Only modify files and lines strictly required by the task — no incidental refactoring, reformatting, or unrelated changes
- Do not introduce new NuGet packages without justification — if needed, state the reason and ask first; only add after receiving explicit approval

### Security

- Never log or expose secrets, API keys, tokens, or connection strings — use redacted placeholders
- Validate all user-controllable input (headers, query strings, route parameters, request bodies) — assume hostile
- All cryptography must use constant-time comparison (`CryptographicOperations.FixedTimeEquals`) and strong algorithms (SHA-256 minimum for hashing, AES-256 for encryption)
- Header names and values written to `HttpResponse.Headers` must be validated against `[A-Za-z0-9\\-_]+` — block CR/LF injection
- Regex patterns must compile with a bounded timeout (100ms default) — prevent ReDoS
- Never trust `Content-Length` or caller-controlled limits — cap allocations to configured maximums
- Admin endpoints (`/_sharkable/*`) must require API key authentication by default; return 404 when no keys are configured

### Memory & lifetime

- Every `IDisposable` / `IAsyncDisposable` resource must be disposed — use `using` declarations or `try/finally` dispose blocks
- Stream replacements (e.g., `context.Response.Body = new XxxStream(...)`) must restore the original stream in a `finally` block
- `MemoryStream` / buffer-based response wrappers must use counting/capped streams — never allow unbounded growth from attacker-controlled input
- Background services (`CronScheduler`, `AuditLogBuffer`, `AdaptiveLimitMonitor`) must survive exceptions via try/catch in the consumer loop — a single failure must not tear down the service
- `CancellationTokenSource` instances must be disposed; linked tokens created per-operation must be disposed after use
- DI-registered singletons that hold `MemoryCache` must own a private cache instance — never hijack the app-wide `IMemoryCache`

### Do no harm

- Never change existing behavior unless the user explicitly opts in — `EnableXxx = false` must be identical to the feature not existing
- Never modify shared framework services — own your own abstractions, use `TryAddSingleton` so user registrations win
- Never change response body shape, status codes, or headers silently — all such changes require explicit configuration
- Before implementing a feature, read all adjacent code paths to understand the full impact — a new middleware must not break existing middleware ordering or assumptions
- Every behavior change must be reflected in the CHANGELOG in the same commit

### Performance

- Avoid allocations on hot paths — prefer `ArrayPool<byte>`, `StringBuilder` reuse, cached `HashSet<T>` / `ConcurrentDictionary<T>` patterns
- Check `ILogger.IsEnabled(logLevel)` before building log strings — avoid allocations when the category is filtered
- Use `Convert.ToHexString` over per-byte `ToString("x2")` string building — one allocation vs many
- Source-generated JSON serialization over reflection-based — required for AOT, faster for JIT
- Lazy-initialize expensive resources (counters, caches, compiled regex) — don't allocate what isn't used
- `IEndpointFilter` implementations must be stateless or share cached state — instantiated once per route group, not per request
- Prefer `CultureInfo.InvariantCulture` for all internal string formatting — avoid culture-sensitive allocations on the hot path

## Solution layout

| Project | Path | Notes |
|---|---|---|
| Sharkable (lib) | `src/Sharkable/` | NuGet package, `Microsoft.NET.Sdk` |
| Sharkable.NativeTest | `src/Sharkable.NativeTest/` | `PublishAot=true` |

## Build & run

```bash
dotnet build
dotnet build src/Sharkable/Sharkable.csproj
dotnet pack src/Sharkable/Sharkable.csproj

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
 10. **Cut Docusaurus version** — in the docs site repo, run `npm run docusaurus docs:version <label>` (e.g. `npm run docusaurus docs:version 0.6.0`) to snapshot `docs/` + `sidebars.js` into `versioned_docs/version-<label>/` + `versioned_sidebars/version-<label>-sidebars.json`.
 11. **Fix ZH version label** — Docusaurus defaults the newly created `i18n/zh-cn/docusaurus-plugin-content-docs/version-<label>.json` to `"Next"`; change to `"<label>"`.
 12. **Commit and push docs site** — `git add -A && git commit -m "chore: cut docs version <label> (Docusaurus snapshot)" && git push`.

**NOTE:** The Docusaurus version cut must happen AFTER the QuickStart version update (step 5) but BEFORE the docs site repo is pushed. This ensures the versioned snapshot carries the correct QuickStart version number, and the ZH version label fix is applied before the push.

## Documentation checklist (apply after EVERY feature or version change)

### QuickStart — 2 files always updated, versioned files NEVER touched

After ANY version bump, update BOTH version strings (`dotnet add package` line AND `<PackageReference>` line) in the **current** docs only:

| File | Rule |
|------|------|
| `docs/quickstart.md` | Update to new version |
| `i18n/zh-cn/.../current/quickstart.md` | Update to new version |

**Never touch** any `versioned_docs/version-<label>/quickstart.md` or `i18n/zh-cn/.../version-<label>/quickstart.md` — they are frozen snapshots. Each versioned QuickStart keeps its own release version forever. When `npm run docusaurus docs:version <new-label>` is later run, the current docs (already carrying the new version) are snapshot automatically.

**Check command:** `grep -r "add package\|PackageReference" ~/dev/sharkableio.github.io/docs ~/dev/sharkableio.github.io/versioned_docs ~/dev/sharkableio.github.io/i18n`

### Sidebar translations — update ALL applicable JSON files

Every time `sidebars.js` adds or renames a category, the ZH label MUST be added to:
- `i18n/zh-cn/.../current.json`
- `i18n/zh-cn/.../version-<label>.json` **for every existing version**

The rule: `ls i18n/zh-cn/docusaurus-plugin-content-docs/version-*.json` → update every one of them. A missing category translation in ANY version will cause the EN label to appear in the ZH sidebar for that version.

**Check command:** Compare `grep "label:" sidebars.js` categories against `jq 'keys'` of each version JSON file.

### Version labels — cut-time fix

After `npm run docusaurus docs:version <label>`, Docusaurus defaults the ZH version label to `"Next"`. Immediately fix:
- `i18n/zh-cn/.../version-<label>.json` → `"version.label": { "message": "<label>" }`

### CHANGELOG — every feature

- Update `CHANGELOG.md` in the **same commit** as the feature implementation
- `Unreleased` section for ongoing work; move to dated version section at release time

### Deprecation warnings in docs

When an API is marked `[Obsolete]`, the corresponding doc page MUST include a prominent deprecation warning with the version of deprecation and the target removal version. Example:

> **Deprecated since v0.4.0. Will be removed in v0.5.0.** Migrate to `ISharkEndpoint` immediately.

### NuGet publish restriction

**NEVER run `dotnet nuget push` without explicit user permission.** This applies to ALL packages: Sharkable, Sharkable.Cache.Redis, Sharkable.AutoCrud.SqlSugar, and any future plugin packages.

A "release" or "publish" instruction means: bump version numbers, update CHANGELOG, tag git, push code, update docs site. It does **NOT** include `dotnet nuget push` unless the user explicitly says so.

### Pre-publish gate

Before publishing ANY package to NuGet, the agent **must** run the `Sharkable.NativeTest` project and verify it starts without errors or warnings:

```bash
cd src/Sharkable.NativeTest && dotnet run &
sleep 5
curl -sf http://localhost:5245/healthz
# Must return HTTP 200 or 503 (unhealthy is OK — startup failed is NOT)
# Must NOT contain "Application startup exception" in stderr
kill %1
```

If NativeTest has ANY build error, runtime exception, or startup crash, the publish **must be blocked** until the issue is resolved. This gate applies to all packages — companion packages (AutoCrud.SqlSugar, Cache.Redis, Testing) as well as the core Sharkable package.

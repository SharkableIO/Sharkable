# Sharkable Plugin System Design

**Date:** 2026-07-18
**Status:** Draft for review

## 1. Motivation

Sharkable already has a plugin pattern — `AutoCrudExtension.AddAutoCrud()` uses reflection to discover `Sharkable.AutoCrud.SqlSugar` and invoke its registration method. This works but is ad-hoc: each plugin uses its own discovery mechanism, there's no standard contract, and no lifecycle hooks.

A formal plugin system would let third-party developers ship NuGet packages that integrate seamlessly — middleware, stores, health checks, OpenAPI transforms — all wired automatically when the package is referenced.

## 2. Design goals

| Goal | Why |
|---|---|---|
| **Zero-config by default** | Reference the NuGet package → it works. No manual `AddSharkableXxx()` calls required |
| **Hot-plug via folder** | Drop `.dll`/`.so` into `./plugins/`, restart → auto-loaded. No recompile |
| **Opt-out is one line** | `opt.DisablePlugin("Sharkable.Cache.Redis")` removes a plugin |
| **Consistent with existing patterns** | `ConfigureXxx()`, `XxxFactory`, `ISharkEndpoint` — plugins use the same conventions |
| **AOT-compatible** | NuGet/manual paths work in AOT. Folder scanning is JIT-only (`AssemblyLoadContext`) |
| **No hijacking** | Plugins own their services, use `TryAddSingleton` so host wins |
| **Graceful failure** | A bad `.dll` logs a warning and is skipped — never crashes startup |

## 3. The plugin contract

Every plugin implements one interface:

```csharp
namespace Sharkable;

/// <summary>
/// A Sharkable plugin. Implement this to ship a NuGet package that auto-integrates.
/// Discovered by assembly scanning at startup.
/// </summary>
public interface ISharkPlugin
{
    /// <summary>
    /// Human-readable name for diagnostics and opt-out. Must be unique per plugin.
    /// Example: "Sharkable.Cache.Redis"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called during <c>AddShark()</c>. Register services, options, stores here.
    /// Uses <c>TryAdd*</c> so the host application wins on conflicts.
    /// </summary>
    void ConfigureServices(IServiceCollection services, SharkOption option);

    /// <summary>
    /// Called during <c>UseShark()</c>. Wire middleware, map endpoints here.
    /// Return <c>null</c> if no pipeline changes are needed.
    /// </summary>
    void ConfigurePipeline(WebApplication app, SharkOption option);

    /// <summary>
    /// Called during OpenAPI document generation. Add schemas, transformers here.
    /// Called only when <c>UseOpenApi</c> is enabled.
    /// </summary>
    void ConfigureOpenApi(OpenApiOptions openApiOptions, SharkOption option);
}
```

## 4. Usage — plugin author

A Redis cache plugin author writes:

```csharp
namespace Sharkable.Cache.Redis;

public sealed class RedisCachePlugin : ISharkPlugin
{
    public string Name => "Sharkable.Cache.Redis";

    public void ConfigureServices(IServiceCollection services, SharkOption option)
    {
        // Register Redis as IDistributedRateLimitStore (TryAdd — host can override)
        services.TryAddSingleton<IDistributedRateLimitStore, RedisRateLimitStore>();
        services.TryAddSingleton<IIdempotencyStore, RedisIdempotencyStore>();

        // Add a health check
        if (option.EnableHealthChecks)
            services.AddHealthChecks().AddCheck<RedisHealthCheck>("redis");
    }

    public void ConfigurePipeline(WebApplication app, SharkOption option)
    {
        // Plugins should rarely add middleware — but it's available
    }

    public void ConfigureOpenApi(OpenApiOptions openApiOptions, SharkOption option)
    {
        // Add Redis-specific schema transformations
    }
}
```

## 5. Usage — host application

**Default (zero-config):**
```csharp
// RedisCachePlugin auto-discovered from the referenced assembly
builder.Services.AddShark();
```

**Opt-out:**
```csharp
builder.Services.AddShark(opt =>
{
    opt.DisablePlugin("Sharkable.Cache.Redis");
});
```

**Custom plugin (in-app):**
```csharp
builder.Services.AddShark(opt =>
{
    opt.RegisterPlugin(new MyCustomPlugin());
});
```

**Disable all auto-discovery:**
```csharp
builder.Services.AddShark(opt =>
{
    opt.AutoDiscoverPlugins = false;
    opt.RegisterPlugin(new RedisCachePlugin());
});
```

## 6. Discovery mechanism

There are three discovery paths, tried in order. Each plugin instance is deduplicated by `Name`.

### 6a. NuGet / assembly scanning (JIT mode)

```csharp
// In AddCommon(), after WireSharkEndpoint:
foreach (var assembly in Shark.Assemblies)
{
    foreach (var type in assembly.GetTypes())
    {
        if (type is { IsAbstract: false, IsInterface: false } &&
            typeof(ISharkPlugin).IsAssignableFrom(type) &&
            type.GetConstructor(Type.EmptyTypes) != null)
        {
            var plugin = (ISharkPlugin)Activator.CreateInstance(type)!;
            RegisterPluginInternal(plugin);
        }
    }
}
```

This replaces the ad-hoc `AutoCrudExtension.AddAutoCrud()` reflection — plugins self-register via `ISharkPlugin` instead of being discovered by magic strings.

### 6b. Hot-plug folder scanning (JIT only, opt-in)

Dropping a plugin folder into the configured plugins directory auto-loads it at startup — no NuGet reference, no recompile, no `RegisterPlugin()` call needed.

**Directory structure — each plugin in its own subfolder:**

```
./plugins/                        ← PluginOptions.Directory (default)
  ├── MyAuthPlugin/               ← one folder per plugin
  │   ├── MyAuthPlugin.dll        ← contains ISharkPlugin impl
  │   ├── MyAuthPlugin.deps.json  ← dependency manifest
  │   └── BouncyCastle.Crypt.dll  ← plugin's own dependency
  │
  └── MyLogPlugin/
      ├── MyLogPlugin.dll
      └── Serilog.Sinks.File.dll
```

Each subfolder is treated as an independent unit — its own `AssemblyLoadContext` with its own dependency resolver. A subfolder without a valid `ISharkPlugin` is skipped with a warning.

**Configuration:**

```csharp
opt.ConfigurePlugins(p =>
{
    p.Directory = "./extensions";       // default: "./plugins"
    p.ScanOnStartup = true;             // default: false (opt-in)
});
```

**How it works:**

```
Startup
  ├── Resolve plugins directory  (Path.GetFullPath("./plugins"))
  ├── Enumerate subdirectories
  ├── For each subdirectory:
  │     ├── Find .dll containing ISharkPlugin   (scan .dll files in folder)
  │     ├── If no ISharkPlugin found → log warning, skip folder
  │     ├── Create isolated AssemblyLoadContext for this folder
  │     │     └── AssemblyDependencyResolver uses <folder>/<name>.deps.json
  │     ├── Load assembly, instantiate plugin (parameterless constructor)
  │     └── RegisterPlugin(plugin)
  └── Proceed with normal plugin lifecycle
```

**Constraints:**

| Constraint | Reason |
|---|---|
| One `ISharkPlugin` per subfolder | One folder = one plugin. If a folder has 0 or 2+ plugin implementations, log warning and skip |
| Parameterless constructor required | `Activator.CreateInstance` — no DI during plugin instantiation. Plugins receive `IServiceCollection` in `ConfigureServices` |
| JIT only | `AssemblyLoadContext.LoadFromAssemblyPath` is `[RequiresDynamicCode]`. In AOT, only NuGet/assembly + manual registration work |
| No hot-unload in v1 | `AssemblyLoadContext.Unload()` is fragile; unload is not in scope |

**Failure handling:**

```
Subfolder has no .dll with ISharkPlugin:
  → Log warning, skip folder, continue. Never crashes startup.

Plugin constructor throws:
  → Log warning with plugin folder and exception message, skip, continue.

Two plugins export same ISharkPlugin.Name (across any discovery path):
  → First wins (NuGet > folder scan > manual). Log warning about duplicate.

**Example — a third-party ships `MyAuthPlugin`:**

```csharp
// In MyAuthPlugin.dll — no NuGet dependency on Sharkable beyond ISharkPlugin
public sealed class MyAuthPlugin : ISharkPlugin
{
    public string Name => "MyOrg.Auth";

    public void ConfigureServices(IServiceCollection services, SharkOption option)
    {
        services.TryAddSingleton<IAuthorizationInterceptor, MyAuthInterceptor>();
    }

    public void ConfigurePipeline(WebApplication app, SharkOption option) { }
    public void ConfigureOpenApi(OpenApiOptions openApiOptions, SharkOption option) { }
}
```

Host deploys:
```bash
dotnet publish -c Release
mkdir -p ./plugins/MyAuthPlugin/
cp MyAuthPlugin.dll ./plugins/MyAuthPlugin/
cp MyAuthPlugin.deps.json ./plugins/MyAuthPlugin/
# restart → plugin auto-loads
```

### 6c. Manual registration (AOT + explicit)

Source generator emits a `[SharkPlugin]` attribute and generates a registration entry point:

```csharp
// Generated by Sharkable.SourceGenerator
internal static class SharkPluginRegistration
{
    public static void RegisterAll(SharkOption option)
    {
        option.RegisterPlugin(new Sharkable.Cache.Redis.RedisCachePlugin());
    }
}
```

Or, the simpler path: the host explicitly calls `opt.RegisterPlugin(new RedisCachePlugin())` in AOT mode.

## 7. Plugin lifecycle

```
AddShark() called
  ├── SharkOption created
  ├── User callback invoked  (opt => { ... })
  ├── Plugin discovery (auto or explicit)
  │     for each plugin:
  │       ├── Register plugin instance
  │       ├── opt.DisablePlugin check → skip
  │       └── plugin.ConfigureServices(services, opt)
  ├── WireSharkEndpoint()  (user endpoints)
  ├── AddCommon services   (framework defaults)
  └── ConfigurationValidator.Validate()

UseShark() called
  ├── Framework middleware  (tracing, compression, security headers, ...)
  │
  ├── Plugin pipeline hooks (order = registration order)
  │     for each plugin:
  │       └── plugin.ConfigurePipeline(app, opt)
  │
  ├── Auth
  ├── Exception handler
  ├── Endpoints
  └── Warmup / readiness gate
```

## 8. Existing code migration

| Today | After |
|---|---|
| `AutoCrudExtension.AddAutoCrud()` uses `Assembly.Load` + `GetType` by name | `ISharkPlugin` implementing type in `Sharkable.AutoCrud.SqlSugar` |
| `SharkExtension.AddCommon()` hardcodes `AddAutoCrud()` call | Loop over registered plugins, call `ConfigureServices` |
| Cache.Redis registers via `AddSharkableRedis()` extension | `RedisCachePlugin : ISharkPlugin` — auto-discovered |

`AddSharkableRedis()` and `AddSharkableAutoCrudSqlSugar()` extension methods remain for backward compat — they internally call `opt.RegisterPlugin(...)`.

## 9. New types

### `ISharkPlugin` (already in §3)

### `PluginOptions`

```csharp
/// <summary>
/// Options for the plugin hot-plug folder scanning system.
/// Passed via <c>opt.ConfigurePlugins(p => ...)</c>.
/// </summary>
public sealed class PluginOptions
{
    /// <summary>
    /// Root directory containing plugin subfolders. Each subfolder is one plugin.
    /// Relative paths are resolved against the app's content root.
    /// Default is <c>"./plugins"</c>.
    /// </summary>
    public string Directory { get; set; } = "./plugins";

    /// <summary>
    /// When true, each subfolder under <see cref="Directory"/> is scanned
    /// at startup for a <c>.dll</c> containing an <see cref="ISharkPlugin"/>
    /// implementation. Default is false (opt-in).
    /// </summary>
    public bool ScanOnStartup { get; set; } = false;
}
```

### `SharkOption` additions

```csharp
public sealed class SharkOption : ISharkOption
{
    // --- Plugin system ---

    /// <summary>
    /// Configures the plugin hot-plug folder scanning system.
    /// </summary>
    public void ConfigurePlugins(Action<PluginOptions> configure);

    /// <summary>
    /// When true (default), automatically discovers ISharkPlugin implementations
    /// in all scanned assemblies (NuGet references + project assemblies).
    /// Set false in AOT mode.
    /// </summary>
    public bool AutoDiscoverPlugins { get; set; } = true;

    /// <summary>
    /// Registers a plugin instance manually. Use in AOT mode or for in-app plugins.
    /// Deduplicated by ISharkPlugin.Name.
    /// </summary>
    public void RegisterPlugin(ISharkPlugin plugin);

    /// <summary>
    /// Prevents a plugin from loading. Use the plugin's Name property.
    /// Effective across all three discovery paths.
    /// </summary>
    public void DisablePlugin(string pluginName);

    internal PluginOptions? PluginOptions { get; set; }
    internal List<ISharkPlugin> RegisteredPlugins { get; }
    internal HashSet<string> DisabledPlugins { get; }
}
```

## 10. Open questions

1. **Plugin ordering**: If PluginA and PluginB both add middleware, who runs first? Proposal: registration order (alphabetical by Name for auto-discovered; file sort order for folder-scanned; explicit order for `RegisterPlugin` calls). A `Priority` property on `ISharkPlugin` (int, default 0) could allow overrides.

2. **Plugin dependencies**: If RedisCachePlugin needs ConnectionMultiplexer registered, who registers it? Proposal: the plugin calls `services.TryAddSingleton<IConnectionMultiplexer>(...)` in its own `ConfigureServices`. No cross-plugin dependency graph.

3. **Shared dependencies between plugins**: What if `PluginA` and `PluginB` both need `Newtonsoft.Json.dll`? With per-folder isolation, each ships its own copy. This wastes disk but prevents version conflicts. A shared `./plugins/_shared/` folder could be added later as an optimization.

4. **Signature / trust verification for hot-plug dlls**: Should Sharkable verify a checksum or signature before loading a `.dll` from disk? Proposal: opt-in via `PluginOptions.RequireSignature = false` (default). If enabled, the `.dll` must be signed with a trusted certificate. Out of scope for v1.

5. **Backward compat for `SqlSugarOptionsConfigure`**: The `SharkOption.SqlSugarOptionsConfigure` property triggers AutoCrud setup. After migration, this property becomes a hint read by the AutoCrud plugin. No breaking change needed.

6. **File locking on Windows**: `AssemblyLoadContext.LoadFromAssemblyPath` locks the `.dll`. To update a plugin while the app is running, the host must stop → replace folder → restart. Accept as by-design for v1. Shadow-copy to temp directory before loading could be added later if hot-reload is needed.

## 11. AOT considerations

NativeAOT compiles the entire application into a single native binary. There is no JIT compiler at runtime, so loading arbitrary IL from a `.dll` file is impossible. This has direct consequences for the plugin system:

| Path | JIT | AOT |
|---|---|---|
| **6a. NuGet / assembly scanning** | Works — `Assembly.GetTypes()` + `Activator.CreateInstance` | Works — but types must be preserved via rd.xml or source-gen |
| **6b. Hot-plug folder scanning** | Works — `AssemblyLoadContext.LoadFromAssemblyPath` | **Does not work.** No JIT to compile IL. Plugin APIs must be called directly |
| **6c. Manual registration** | Works | Works — `opt.RegisterPlugin(new MyPlugin())` |

**AOT plugin workflow:**

插件开发者发布 NuGet 包（不是 `.dll` 文件）。宿主在项目里添加引用，和主程序一起编译为 AOT 二进制。

```xml
<!-- Host's .csproj -->
<PackageReference Include="Sharkable.Plugin.MyAuth" Version="1.0.0" />
```

```csharp
// Host's Program.cs — plugin auto-discovered via path 6a
builder.Services.AddShark();  // MyAuthPlugin automatically found and registered
```

插件源码结构：
```csharp
// Plugin author's class library project (net10.0, references Sharkable NuGet)
public sealed class MyAuthPlugin : ISharkPlugin
{
    public string Name => "Sharkable.Plugin.MyAuth";
    // ... ConfigureServices / ConfigurePipeline / ConfigureOpenApi
}
```

编译方式：普通 class library (`dotnet pack` → NuGet)，不特殊处理。AOT 模式下宿主引用后一起编译即可。

## 12. Hot-reload (no restart)

用户上传插件文件后不重启就能生效——这是三个不同层次的能力：

### 12a. Cold reload — 需要重启（✅ 支持，JIT 模式）

```bash
# 1. 把插件放到子文件夹
cp MyPlugin.dll ./plugins/MyPlugin/
cp MyPlugin.deps.json ./plugins/MyPlugin/

# 2. 重启应用
systemctl restart myapp
# → 插件生效
```

### 12b. Warm reload — 不重启，手动触发（⚠️ 部分支持，JIT 模式）

通过管理端点 `/_sharkable/plugins/reload` 触发，扫描新文件夹、加载新 `AssemblyLoadContext`。但新加载的插件**不能注册新的 DI 服务**（`IServiceCollection` 在 `app.Build()` 时已固化）。只能做请求时生效的事情 —— 端点、过滤器、中间件。

```
能够动态添加的：
  ✅ MapGet/MapPost 等端点路由         (IEndpointRouteBuilder 支持运行时注册)
  ✅ IEndpointFilter（端点过滤器）      (可在运行时附加到路由组)
  ✅ 中间件                            (app.Use() 在 Build 后端不可用...)

不能动态添加的：
  ❌ DI 服务（IServiceCollection.ConfigureServices）
  ❌ 替换已有 Store 实现
  ❌ OpenAPI schema 变更
```

这意味着 warm reload 对大多数插件类型（Store 替换、HealthCheck 注册）**不起作用**。实际价值有限。

### 12c. True hot-reload — 零停机（❌ 不支持）

`.NET` 的 `AssemblyLoadContext.Unload()` 可以卸载程序集，但要求所有对 ALC 内类型的引用都被 GC 回收——框架级插件（Store、Middleware、Filter）被 DI 容器和管道持有，几乎无法完全释放。

**建议：v1 只支持 cold reload。Warm reload 留到后续版本评估。**

实际生产环境中，滚动更新（先起新实例，再停旧实例）比进程内热重载更可靠、更简单。

## 13. What does NOT change

- `ISharkEndpoint` discovery and registration — unchanged
- All existing `ConfigureXxx()` methods on `SharkOption` — unchanged
- `UseShark()` pipeline ordering — unchanged
- Middleware, stores, factories — unchanged
- `ISharkOption` marker interface — unchanged

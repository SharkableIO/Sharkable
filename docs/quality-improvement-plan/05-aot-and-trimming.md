# 05 — AOT & Trimming

Sharkable's AOT story is a core selling point (`Sharkable.NativeTest` publishes with `PublishAot=true`). The gaps below are where a user publishing AOT today would hit warnings-as-errors or runtime `NotSupportedException`s. Goal: **zero IL2xxx/IL3xxx warnings** in the library itself (PROC-03 gate).

---

### AOT-01 — JSON responses serialize via runtime type outside the source-generated context
**Severity** P1 · **Effort** M · **Breaking** No (for default factory; documented requirement for custom factories)
**Location:** `src/Sharkable/UnifiedReults/UnifiedResultResult.cs:13`, `src/Sharkable/Middleware/ProblemDetailsResult.cs:30`, `src/Sharkable/Validation/ValidationFilter.cs:44`, `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs:220`, `src/Sharkable/Middleware/HealthCheckEndpoint.cs` (all `Results.Json(...)` calls)
**Problem:** `WriteAsJsonAsync(value, value.GetType())` resolves serialization metadata for the **runtime** type through the `TypeInfoResolverChain`. The chain contains `UnifiedResultSourceContext` (covers `UnifiedResult<object?>` and primitives) plus whatever the user registers. Consequently:
- a **custom** `IUnifiedResultFactory` returning its own DTO → runtime type unknown to any resolver → reflection fallback (IL3050 warning) → `NotSupportedException` under PublishAot;
- `ProblemDetailsData`, `HealthCheckResponse`, cron admin's anonymous projection are **not in any source context** — same failure mode when `UseProblemDetails` / health checks / cron admin are used in an AOT app.
**Proposal:**
1. Add all framework-owned response DTOs (`ProblemDetailsData`, `HealthCheckResponse`, `HealthCheckEntry`, cron admin projection — replace the anonymous type with a named `CronJobStateDto`) to `UnifiedResultSourceContext`.
2. Introduce `public partial class SharkJsonContext : JsonSerializerContext` guidance: users deriving their context register it through a new `SharkOption.JsonTypeInfoResolver` hook that gets inserted into `TypeInfoResolverChain` (factory-style, consistent with the framework's other hooks).
3. When resolution fails at runtime, throw a descriptive error telling the user exactly which type to add to their context (better than a bare `NotSupportedException`).

### AOT-02 — AutoCrud endpoint instantiation uses `Activator.CreateInstance`
**Severity** P1 · **Effort** M · **Breaking** No (internal behavior)
**Location:** `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:237-257`
**Problem:** `Activator.CreateInstance(classType)` to probe `IAutoCrudEntityMarker` — reflection instantiation (trim-unsafe, requires public parameterless ctor, bypasses DI) at endpoint-mapping time. In an AOT app with trimming, the ctor may be removed → `MissingMethodException` → silently falls back to `CrudOperations.All`, generating **more** endpoints than intended.
**Proposal:** The endpoint classes are already registered in DI (`WireSharkEndpoint`) — resolve the registered instance instead of creating a new one. If a marker interface check is needed without instantiation, do a static interface-map check (`classType.GetInterfaceMap`/direct `IsAssignableFrom` on the marker) before falling back.

### AOT-03 — Library does not enable trim/AOT analyzers on itself
**Severity** P1 · **Effort** S (enable) + M (fix fallout) · **Breaking** No
**Location:** `src/Sharkable/Sharkable.csproj`
**Problem:** No `<IsAotCompatible>`, `<IsTrimmable>`, or analyzer properties — IL warnings in the library only surface inside *consumer* apps, where they are noisy and hard to attribute. We currently find AOT bugs by reading code (this audit) instead of by compiler.
**Proposal:** Set `<IsAotCompatible>true</IsAotCompatible>` (implies `IsTrimmable`, enables IL2026/IL3050/IL2070 analyzers) on the library; annotate the intentional dynamic paths (legacy `[SharkEndpoint]` mapper, `Reflector`, non-AOT `AddShark()`) with `RequiresDynamicCode`/`RequiresUnreferencedCode` (partially done) until the build is clean. Add a CI leg that publishes `Sharkable.NativeTest` and fails on new warnings (PROC-03).

### AOT-04 — Validation filter reflection in the hot path
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Validation/ValidationFilter.cs:26`
**Problem:** `typeof(IValidator<>).MakeGenericType(arg.GetType())` per request — `MakeGenericType` on open generics is a trimming hazard (`IL2055`) and slow (PERF-07).
**Proposal:** Cache closed `IValidator<T>` lookups in a `ConcurrentDictionary<Type, ...>` with proper annotations; negative-cache types with no validator. Long-term (FEAT-14): a source generator emits the validator map at compile time.

### AOT-05 — OpenAPI sensitive-property transformer reflects per schema without caching
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/OpenApi/SwaggerExtension.cs:82-111`
**Problem:** `type.GetMembers(...)` + `GetCustomAttribute` for every schema at document-generation time. Startup-only, but uncached and unannotated; under aggressive trimming the attributes may be stripped, silently re-exposing ignored properties.
**Proposal:** Cache per-`Type` results in a `ConcurrentDictionary`; add `[DynamicallyAccessedMembers(PublicProperties | PublicFields)]` propagation or a `[RequiresUnreferencedCode]` note on the attribute path; add a NativeTest endpoint proving `[SharkOpenApiIgnore]` survives PublishAot.

### AOT-06 — Legacy `WriteJsonAsync` helpers use reflection-based serialization
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/JsonHelper/Extensions/HttpResponseExtension.cs`
**Problem:** `JsonSerializer.Serialize(data, type, options)` — non-generic, reflection-based, plus per-call options allocation (PERF-11).
**Proposal:** Obsolete (`[Obsolete(..., false)]` first, error later) or annotate `[RequiresUnreferencedCode]`; route internal callers through the source-generated context.

### AOT-07 — Legacy attribute-endpoint subsystem is guarded but still compiled into AOT apps
**Severity** P3 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:401-467`, `src/Sharkable/Reflector/`
**Problem:** `MapAttributeEndpoints` is correctly `[RequiresDynamicCode]` and skipped when `AotMode == true`, but the code (and `Reflector`, `IDependencyReflectorFactory`) still ships and is registered unconditionally (`AddDiFactory`).
**Proposal:** Register `IDependencyReflectorFactory` only when `AotMode == false`; document that the attribute system is excluded from AOT support; consider a future feature-switch (`<SharkableEnableLegacyEndpoints>`) to trim it out entirely.

---

## Verification strategy

1. `dotnet publish src/Sharkable.NativeTest -c Release` must stay green (existing gate).
2. After AOT-01: NativeTest exercises wrapped results, `UseProblemDetails=true`, health checks, cron admin → assert all return JSON under AOT.
3. After AOT-03: CI treats any new IL2xxx/IL3xxx in `Sharkable.csproj` as an error (`<WarningsAsErrors>IL2026;IL3050;IL2070;IL2072;IL2055</WarningsAsErrors>` in Release).

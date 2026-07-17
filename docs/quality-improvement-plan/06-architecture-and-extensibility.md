# 06 — Architecture & Extensibility

The maintainer's standing direction: **general-purpose, minimal-intrusion, default-first but fully replaceable**. This file covers (a) design inconsistencies to fix and (b) every place where a `XxxFactory` / hook is missing today. Items marked 🏭 are factory-gap additions.

---

## A. Consistency & design-debt fixes

### ARCH-01 — Three divergent `IUnifiedResultFactory` resolution paths
**Severity** P1 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/DependencyInjection/Extensions/DependencyInjectionExtension.cs:11` (DI singleton), `src/Sharkable/Shark/Options/SharkOption.cs:67` (static option), five `new DefaultUnifiedResultFactory()` call sites (PERF-01)
**Problem:** DI registration, static option, and per-call `new` disagree. A user registering a custom factory **in DI** is ignored by every call site; setting the option property works but the DI singleton still points at the default.
**Proposal:** One resolution helper (`SharkResultFactory.Resolve()`), precedence: explicit `SharkOption.UnifiedResultFactory` → DI-registered → shared default instance. Register the *effective* instance in DI at `AddCommon` so both views agree.

### ARCH-02 — Options are configured twice and live in two divergent instances
**Severity** P1 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Shark/Extensions/SharkExtension.cs:21-26`
**Problem:** `setupOptions` is invoked once on the static `Shark.SharkOption` and again inside `services.Configure<SharkOption>` — user callbacks with side effects (list adds, registrations) run twice, and the static instance and the `IOptions<SharkOption>` instance are different objects that can drift apart (some code reads the static, some reads `IOptions`).
**Proposal:** Build **one** instance, invoke the callback once, then `services.AddSingleton(instance)` + `services.AddSingleton<IOptions<SharkOption>>(Options.Create(instance))` (or `services.Configure` with a copy-over action). All internal consumers read from that single source. Prerequisite step toward MEM-05.

### ARCH-03 — `ISharkEndpoint` implementations are registered as singletons
**Severity** P1 · **Effort** L · **Breaking** Maybe (opt-in)
**Location:** `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:324-356`
**Problem:** `services.AddSingleton(typeof(ISharkEndpoint), e)` — endpoints cannot constructor-inject scoped services (DbContext etc.); users must fall back to method-injected parameters or `IServiceProvider`, which is non-obvious and a captive-dependency trap (a scoped dependency captured in a singleton ctor is silently reused across requests).
**Proposal:** Add `SharkOption.EndpointLifetime` (`Singleton` default for compat | `Transient` | `Scoped`): register endpoint *types* with the chosen lifetime and resolve per-route-builder from a scope factory at map time (mapping is startup work, so `Transient` resolved once still captures; document that per-request state must come from method parameters). Also ship a Roslyn analyzer rule (we already ship `Sharkable.Analyzers`) warning when a singleton endpoint ctor-injects a scoped service.

### ARCH-04 🏭 — No `appsettings.json` binding for `SharkOption`
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Shark/Options/SharkOption.cs:13` (`Default = "Sharkable"` section name exists but nothing binds it)
**Problem:** Configuration is code-only. The `Default` section constant suggests binding was planned but never implemented.
**Proposal:** New overload `AddShark(Assembly[]?, IConfiguration, Action<SharkOption>? = null)`: binds the `"Sharkable"` section first, then invokes the callback (callback wins). Add `IValidateOptions<SharkOption>` bridging to the existing `ConfigurationValidator` so misconfiguration fails fast in both paths.

### ARCH-05 🏭 — Missing factory / hook surface (the explicit ask)

| # | Missing abstraction | Current hard-wiring | Location | Proposed hook |
|---|---|---|---|---|
| a | `ITenantDataSource` implementation | `DefaultTenantDataSource` registered directly | `SharkExtension.cs:208-212` | `TenantOptions.DataSourceFactory: Func<IServiceProvider, ITenantDataSource>` |
| b | `ITenant` implementation | `Tenant` registered directly | `SharkExtension.cs:206` | `TenantOptions.TenantFactory` (rarely needed, cheap to add) |
| c | Audit output sink | `ILogger` only, sync or via `AuditLogBuffer` | `AuditTrailMiddleware`, `AuditLogBuffer` | `IAuditSink` (`WriteAsync(IReadOnlyList<AuditLogEntry>)`) + `SharkOption.AuditSinkFactory`; default = logger sink (FEAT-03) |
| d | API-key validation | static `string[]` comparison | `ApiKeyMiddleware`, admin gates | `IApiKeyValidator` (`Task<ApiKeyValidationResult> ValidateAsync(string key, HttpContext)`) + `ApiKeyValidatorFactory`; enables DB-backed keys, per-key metadata/limits (FEAT) |
| e | Health-check response writer | hard-coded `HealthCheckResponse` JSON | `HealthCheckEndpoint.cs:97` | `SharkOption.HealthCheckResponseWriter: Func<HttpContext, HealthReport, Task>` (mirrors `HealthCheckOptions.ResponseWriter`); plus make the 10 s timeout and `/livez` path real options (`HealthCheckTimeoutSeconds` is referenced in a comment but doesn't exist) |
| f | Cron job handler resolution | `CronJob.Handler` is a captured `Func<CancellationToken, Task>` — no DI scope per run | `CronScheduler.cs:146` | `ICronJobHandler` / `CronJob.HandlerFactory: Func<IServiceProvider, Func<CancellationToken, Task>>`; scheduler creates an `IServiceScope` per execution (ARCH-06) |
| g | Saga step resolution | steps are captured delegates — no DI scope per saga | `SagaExecutor.cs:175` | optional `SagaOptions.ServiceProvider` → scope per `ExecuteAsync` (ARCH-07) |
| h | JSON serializer options for framework writes | ad-hoc per call site | `ProblemDetailsResult`, `UnifiedResultResult`, admin endpoints | `SharkOption.JsonSerializerOptions` (or resolver hook from AOT-01) used consistently by every framework-emitted payload |
| i | Profiler storage/sink | static `ProfilerStore` ring buffer | `ProfilerMiddleware.cs:65` | `IProfilerSink` (`Record(ProfilerEntry)`) + factory; default keeps ring buffer; enables exporters |

**Severity** P2 · **Effort** M each (a–e), M–L (f–i) · **Breaking** No (all additive, defaults preserve behavior)

### ARCH-06 — Cron handlers execute without a DI scope
**Severity** P2 · **Effort** M · **Breaking** No (additive overload)
**Location:** `src/Sharkable/Cron/CronScheduler.cs:121-179`
**Problem:** Handlers capture root services or create scopes manually — every user re-implements the same boilerplate, and captured scoped services become accidental singletons.
**Proposal:** In `ExecuteJobAsync`, create `await using var scope = _scopeFactory.CreateAsyncScope()` and expose `scope.ServiceProvider` to the handler (new `CronJob.HandlerFactory` from ARCH-05f, or a `CronJobContext { Services, CancellationToken }` overload). Existing `Func<CancellationToken, Task>` handlers keep working unchanged.

### ARCH-07 — Saga steps execute without a DI scope
**Severity** P2 · **Effort** M · **Breaking** No (additive)
**Location:** `src/Sharkable/DistributedTx/SagaExecutor.cs`
**Problem:** Same pattern as ARCH-06 — saga steps capturing scoped services are unsafe.
**Proposal:** Optional `SagaExecutionOptions.CreateScopePerSaga` (default `false`); when enabled, `ExecuteAsync` wraps execution in a scope exposed to steps via `SagaStepContext`.

### ARCH-08 — Built-in middleware order is fixed; no per-middleware opt-out
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/SharkableExtension.cs:52-223` (`UseShark`)
**Problem:** The pipeline order (tracing → compression → graceful shutdown → tenant → rate limit → output cache → CORS → auth → exception handler → audit → idempotency → ETag → profiler → endpoints) is hard-coded. Users can inject *between* phases (`AddBeforeAuth` etc.) but cannot reorder or exclude a single built-in (e.g. keep idempotency but drop ETag ordering relative to audit) — except the exception handler (`EnableExceptionHandler`).
**Proposal:** Add exclusion flags on `UseSharkOptions` for each optional middleware (`DisableETagMiddleware`, …) and document the canonical order with a diagram in the docs site. Full custom ordering stays "call the middleware yourself" — expose the middleware types `public` (they are currently `internal`) so advanced users can compose manually.

### ARCH-09 — No per-group convention hook
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:88-265`
**Problem:** Groups get auto-tags, filters, and metadata, but a user who wants "every group gets `RequireAuthorization("staff")`" or a custom filter must edit each endpoint class.
**Proposal:** `SharkOption.GroupConvention: Action<RouteGroupBuilder, SharkGroupContext>` (context = group name, version, endpoint types) invoked once per group after the built-ins. One line for users, big ergonomics win.

### ARCH-10 — Distributed rate limiter and idempotency are global-only
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/SharkRateLimiterMiddleware.cs`, `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs`
**Problem:** When enabled, both middlewares apply to *all* requests (rate limiter) or all unsafe methods with a key header (idempotency). No per-endpoint policy metadata in the Sharkable stack (the built-in `AddRateLimiter` path has named policies but is a separate mechanism).
**Proposal:** Metadata-driven: `[SharkRateLimit(limit, windowSeconds)]` / `[SharkIdempotent(ttlSeconds)]` / `[SharkNoIdempotency]` attributes read from `context.GetEndpoint()?.Metadata` in the middleware; global options remain the fallback (FEAT-05/06).

### ARCH-11 — DI auto-registration: first-wins hides multiple implementations; no keyed support
**Severity** P3 · **Effort** M · **Breaking** No (opt-in)
**Location:** `src/Sharkable/DependencyInjection/Extensions/AttributeServiceExtension.cs:55-62, 120-145`
**Problem:** `TryAdd` first-wins: with two implementations of one interface, the second silently never registers, and `IEnumerable<T>` injection yields one item. Also no `FromKeyedServices` support.
**Proposal:** Document the semantics; add `[SingletonService(Key = "name")]` → `AddKeyedXxx`; add opt-in `SharkOption.DiRegistrationMode = First | Enumerable` (uses `TryAddEnumerable`).

### ARCH-12 — Assembly scanning is unguarded against `ReflectionTypeLoadException`
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** every `assembly.GetTypes()` call site (`AttributeServiceExtension`, `EndPointExtension`, `ValidationExtension`, `Utils`)
**Problem:** One assembly with an unloadable type (missing optional dependency) throws `ReflectionTypeLoadException` and kills startup with an opaque error.
**Proposal:** Central `SafeTypeLoader.GetTypes(assembly, ILogger?)` helper: catches `ReflectionTypeLoadException`, returns the loadable subset, logs a warning listing failing types. Used by all scanners (fits naturally into PERF-03's single-pass index).

### ARCH-13 — Admin surface is fragmented
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `CronAdminEndpoint` (`/_sharkable/jobs`, hard-coded path), `SharkProfilerEndpoint` (configurable), health endpoints (`/healthz` configurable, `/livez` hard-coded)
**Problem:** Paths, auth gates, and redaction rules are configured per-endpoint with slightly different mechanisms.
**Proposal:** `SharkOption.AdminBasePath` (default `/_sharkable`) + per-endpoint overrides; one shared gate (PERF-06/SEC-04); document the full admin surface in one docs page.

### ARCH-14 — Tracing middleware creates a second `Activity` per request
**Severity** P3 · **Effort** M · **Breaking** Maybe (span topology changes)
**Location:** `src/Sharkable/Middleware/Tracing/TracingMiddleware.cs:16-51`
**Problem:** ASP.NET Core already starts an `Activity` per request when diagnostics are enabled; Sharkable adds another one → duplicate spans under OpenTelemetry. Tags use the old semconv names (`http.method`, `http.target`) vs current (`http.request.method`, `url.path`). The static `ActivitySource` also captures the configured name once at type-init.
**Proposal:** Default to enriching `Activity.Current` (tags, `X-Trace-Id` header) and only create a new activity when none exists (`TracingOptions.CreateActivityIfMissing`, default `false`); update tag names to semconv 1.x; make the `ActivitySource` instance-based (part of MEM-05).

---

## Dependency-direction note

Keep the dependency rule intact: **core has zero third-party deps beyond ASP.NET Core + FluentValidation**; Redis/SqlSugar live in plugin packages. Every factory above must therefore accept user-provided implementations via `Func<IServiceProvider, T>` (existing pattern) — never via new package references (AGENTS.md: no new NuGet packages without approval).

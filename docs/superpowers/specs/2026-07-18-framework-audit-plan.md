# Sharkable Framework Audit & Improvement Plan

**Date:** 2026-07-18
**Baseline:** post 07-feature-suggestions implementation
**Scope:** Full framework (`src/Sharkable/` + `src/Sharkable.Testing/`)
**Method:** Expert audit from .NET framework/library perspective + product manager perspective, reviewed against AGENTS.md design principles (Convenience, Minimal Intrusion, Customizability).

## 1. Guiding principles (from AGENTS.md)

Every fix must satisfy:

| Principle | Check |
|---|---|
| **Convenience** | Is the fix discoverable? Does it improve the developer experience? |
| **Minimal Intrusion** | Does the fix change existing behavior only when the user opts in? Will existing code continue to compile? |
| **Customizability** | Is the replacement path clear (factory/interfaces)? |

Plus the coding standards: **Security**, **Memory & lifetime**, **Do no harm**, **Performance**.

## 2. Audit results summary

| Severity | Count | Topics |
|---|---|---|
| **HIGH** | 5 | Resource leaks (2), duplicate type definitions (1), exception handler placement (1), AOT breakage (1) |
| **MED** | 7 | Missing interfaces (2), API inconsistency (4), thread-safety concern (1) |
| **LOW** | 4 | Unsealed public classes (1), dead code (2), obsolete DSL (1) |

## 3. Phase 1 — Reliability (target: v0.6.2)

Fix all HIGH-severity items. No feature changes — pure bugfixes. Existing code must continue to compile and behave identically.

### P1.1 — Dispose `AuditLogBuffer` properly

**Location:** `Middleware/AuditLogBuffer.cs:145`, `Middleware/AuditTrailMiddleware.cs:29`, `SharkableExtension.cs:119`

**Problem:** `AuditLogBuffer` implements `IDisposable` (disposes `CancellationTokenSource`) but nobody calls `Dispose()`. The buffer is created via `new` in the middleware constructor, stored in `InternalShark.AuditLogBuffer`, flushed on shutdown via `FlushRemaining()`, but never disposed. The `CancellationTokenSource` leaks.

**Fix:** Call `Dispose()` on the buffer after `FlushRemaining()` in the stopping callback at `SharkableExtension.cs`. The buffer should be `IDisposable`-traced through the middleware's lifetime via DI or explicit disposal in the stopping handler.

**Verification:** CTS instance count does not grow under repeated startup/shutdown cycles (monitor via `dotnet-counters`).

**Files:** `SharkableExtension.cs`

### P1.2 — Dispose `CronScheduler` `renewCts`

**Location:** `Cron/CronScheduler.cs:127`

**Problem:** `renewCts` (`CancellationTokenSource`) is created but never disposed. It is only cancelled via `renewCts?.Cancel()`. `CancellationTokenSource` implements `IDisposable` and must be cleaned up.

**Fix:** Wrap `renewCts` creation in a `using var` block, or dispose it in the `finally` block alongside the cancel operation.

**Verification:** Code review + CTS instance count stable under cron job stress test.

**Files:** `Cron/CronScheduler.cs`

### P1.3 — Unify `ResponseSizeExceededException`

**Location:** `Middleware/ETag/ETagMiddleware.cs:238` and `Middleware/Idempotency/SharkIdempotencyMiddleware.cs:325`

**Problem:** Two separate inner-class definitions of `ResponseSizeExceededException`. Both caught via `catch (ResponseSizeExceededException)` at their respective call sites, so there's no runtime conflict, but this is a maintenance hazard — adding a using or changing namespace resolution order could silently break one catch path. It also violates DRY — same concept, two definitions.

**Fix:** Extract a single `internal sealed class ResponseSizeExceededException` to a shared file (e.g., `Middleware/ResponseSizeExceededException.cs`). Both ETag and Idempotency middleware import it. Remove inner class definitions.

**Verification:** Both ETag and idempotency oversize-response tests pass. No namespace ambiguity.

**Files:** New `Middleware/ResponseSizeExceededException.cs`, modify `Middleware/ETag/ETagMiddleware.cs`, `Middleware/Idempotency/SharkIdempotencyMiddleware.cs`

### P1.4 — Move exception handler to end of pipeline

**Location:** `SharkableExtension.cs:164`

**Problem:** `app.UseSharkExceptionHandler()` is placed after authentication/authorization middleware. This means exceptions thrown inside auth middleware (custom interceptor bugs, JWT validation edge cases) propagate unhandled, producing a raw 500 with no structured error response. Exception handler should be LAST in the pipeline (just before endpoint mapping) to catch ALL exceptions.

**Fix:** Move `app.UseSharkExceptionHandler()` from line 164 to just before `app.MapEndpoints()` (line 200). This ensures ALL middleware exceptions are caught.

**Verification:** Inject a throwing middleware via `AddBeforeAuth()` and verify the response is a structured 500, not a raw exception.

**Files:** `SharkableExtension.cs`

### P1.5 — Guard AutoCrud reflection behind AOT check

**Location:** `AutoCrud/SqlSugar/Extensions/AutoCrudExtension.cs:17-25`

**Problem:** `AddAutoCrud()` uses `Assembly.Load("Sharkable.AutoCrud.SqlSugar")` and `method?.Invoke(null, ...)` unconditionally, even in AOT mode. In AOT, this will throw `FileNotFoundException` because the assembly isn't referenced.

**Fix:** This code is a plugin-loading pattern — it dynamically loads the AutoCrud.SqlSugar package if installed. In AOT, the package must be referenced explicitly (or the feature is disabled). Wrap in `if (!Shark.SharkOption.AotMode)` and log a warning when in AOT mode explaining that AutoCrud.SqlSugar requires an explicit project reference.

**Verification:** AOT publish succeeds with AutoCrud configured. AOT publish without the SqlSugar package doesn't throw at startup.

**Files:** `AutoCrud/SqlSugar/Extensions/AutoCrudExtension.cs`

## 4. Phase 2 — API Consistency (target: v0.7.0)

Fix all MED-severity items. MAY include minor breaking changes with migration docs (batching into minor version per AGENTS.md).

### P2.1 — Add `ISagaExecutor` interface

**Location:** `DistributedTx/SagaExecutor.cs:11`

**Problem:** `SagaExecutor` is `public sealed` with no interface. Users cannot mock it in tests and cannot replace it via DI. All other major components follow the factory pattern (`ISagaStore`, `ICronJobStore`, etc.) but the executor itself does not.

**Fix:** Extract `ISagaExecutor` interface with `ExecuteAsync` and `CompensateAsync`. Register `ISagaExecutor` via `TryAddSingleton<ISagaExecutor, SagaExecutor>()`. Add `SagaExecutorFactory` to `SharkOption` for custom executors.

**Breaking:** No. `SagaExecutor` remains registered as the default. Custom implementations win via `TryAddSingleton`.

**Files:** New `ISagaExecutor` interface, modify `DistributedTx/SagaExecutor.cs`, `SharkOption.cs`, `SharkExtension.cs`

### P2.2 — Expose `CronLockTtl` on `ICronScheduler`

**Location:** `Cron/CronScheduler.cs:34`, `Cron/CronJob.cs:19`

**Problem:** `CronLockTtl` is a public property on `CronScheduler` (sealed class) but not on `ICronScheduler`. Users consuming via the interface must cast to set the lock TTL.

**Fix:** Add `TimeSpan CronLockTtl { get; set; }` to `ICronScheduler`. Default implementation in `CronScheduler` already exists.

**Files:** `Cron/CronJob.cs` (ICronScheduler)

### P2.3 — Normalize `ConfigureJwt` to `ConfigureXxx(Action<XxxOptions>)` pattern

**Location:** `SharkOption.cs:158`

**Problem:** `ConfigureJwt(string authority, string[] audiences, Action<JwtBearerOptions>?)` is the ONLY `Configure*` method that takes positional parameters instead of an `Action<XxxOptions>` delegate. This breaks the discoverability pattern — users don't know it exists unless they read the source.

**Fix:** Create `JwtOptions` class with `Authority`, `Audiences`, `BearerOptions` properties. Change `ConfigureJwt` to `ConfigureJwt(Action<JwtOptions>)`. Keep the old method as `[Obsolete]` for backward compat.

**Breaking:** Minor. Old callers get an `[Obsolete]` warning. New callers use the consistent pattern.

**Files:** New `JwtOptions`, modify `SharkOption.cs`, update all internal callers.

### P2.4 — Normalize `ConfigureAuthorization` to method call

**Location:** `SharkOption.cs:190`

**Problem:** `ConfigureAuthorization` is an `Action<AuthorizationOptions>?` property assigned by `opt.ConfigureAuthorization = opts => ...`. Every other configuration point uses a method call: `opt.ConfigureXxx(opts => ...)`. This is an inconsistency in the API surface.

**Fix:** Change to a method: `public void ConfigureAuthorization(Action<AuthorizationOptions> configure)`. Keep the property as `internal` for backward compat during the transition.

**Files:** `SharkOption.cs`

### P2.5 — Add default implementation to `ICronJobStore.RenewJobLockAsync`

**Location:** `Cron/CronJobState.cs:53`

**Problem:** `ICronJobStore.RenewJobLockAsync` has NO default implementation, unlike `ISagaStore.RenewLockAsync` which is `=> Task.CompletedTask`. Every custom cron store must implement renewal, even when not needed (e.g., in-process stores where locking is irrelevant).

**Fix:** Add default interface method `Task RenewJobLockAsync(string name, TimeSpan lockTtl) => Task.CompletedTask;`.

**Breaking:** None. All existing implementations continue to compile. New implementations can override if needed.

**Files:** `Cron/CronJobState.cs`

### P2.6 — Seal unsealed public classes

**Location:** `SqlSugarOptions.cs:5`, `Shark.cs:11`, `AssemblyContext.cs:8`, `UnifiedResult.cs:11`

**Problem:** `SqlSugarOptions`, `Shark`, `AssemblyContext`, `UnifiedResult<T>` are public but not sealed. Users inheriting them would break the framework's `ConfigureXxx(Action<T>)` callback pattern.

**Fix:** Add `sealed` keyword.

**Breaking:** Technically breaking if any external code inherits these types, but inheritance is not a documented or supported pattern.

**Files:** `AutoCrud/SqlSugar/Options/SqlSugarOptions.cs`, `Shark/Shark.cs`, `AssemlyContext/AssemblyContext.cs`, `UnifiedReults/UnifiedResult.cs`

### P2.7 — Remove dead code

**Location:** `Utils/AssemblyUtil.cs:44-54`, `Middleware/SharkEndpointDsl.cs:48`

**Problem A:** `Utils.SetupModules()` is defined but never called. Dead code adds binary size and confusion.

**Problem B:** `.SharkRequestTimeout(int, string?)` is `[Obsolete]` — it ships broken code in the NuGet package.

**Fix A:** Remove `SetupModules()`. If needed for future module system, add back when the feature is implemented.

**Fix B:** Remove the `[Obsolete]` overload. Keep only the working `.SharkRequestTimeout(string policyName)`.

**Files:** `Utils/AssemblyUtil.cs`, `Middleware/SharkEndpointDsl.cs`

## 5. Phase 3 — Product & Polish (target: v0.8.0)

Improve onboarding, defaults, and documentation. These are product-level decisions with technical implementation.

### P3.1 — Enable health checks by default

**Change:** `SharkOption.EnableHealthChecks` default from `false` to `true`.

**Rationale:** Every orchestrator (k8s, ECS, Nomad) expects `/healthz`. A framework that requires an explicit opt-in for health checks creates a bad first experience — deploy, probe fails, debug. This is the #1 friction point in onboarding.

**Risk:** Slight startup overhead from `services.AddHealthChecks()`. Mitigated by the fact that no checks are added unless the user calls `HealthChecksConfigure`.

**Breaking:** Technically yes — apps that previously had no `/healthz` endpoint will now have one. Mitigated by: `/healthz` returns `{ "status": "Healthy" }` with no data exposure.

**Files:** `SharkOption.cs`

### P3.2 — Change cron concurrency default to `SkipIfRunning`

**Change:** `CronJobOptions.Concurrency` default from `AllowConcurrent` to `SkipIfRunning`.

**Rationale:** `AllowConcurrent` is dangerous as a default. A cron job that runs every minute but takes 90 seconds would stack overlapping runs, consuming threads and potentially corrupting state. `SkipIfRunning` is the safe default — most cron implementations (Linux cron, k8s CronJob) use this behavior.

**Breaking:** Minor. Users who relied on concurrent execution must explicitly set `AllowConcurrent`.

**Files:** `Cron/CronJobOptions.cs`

### P3.3 — Fix `WrapSchemaFactory` coupling with `IUnifiedResultFactory`

**Problem:** When a user replaces `IUnifiedResultFactory`, the OpenAPI document still generates the default `UnifiedResult<T>` schema unless the user ALSO sets `WrapSchemaFactory`. This coupling is undocumented — users discover it when their API consumers get schema mismatches.

**Fix:** Document this explicitly in the `WrapSchemaFactory` XML doc, in the unified-result docs page, and in a CHANGELOG entry. Add a startup warning when `IUnifiedResultFactory` is replaced without `WrapSchemaFactory`.

**Files:** `SharkOption.cs` (XML docs), `ConfigurationValidator.cs` (warning)

### P3.4 — Improve `UnifiedResult<T>` vs `IUnifiedResult` documentation

**Problem:** Three abstractions (`UnifiedResult<T>`, `IUnifiedResult`, `IUnifiedResultFactory`) for one concept. Users don't know when to use each.

**Fix:** Add comprehensive XML doc cross-references between all three types. Document the typical use case: "Use `UnifiedResult.Ok(data)` static methods unless you need custom serialization, in which case implement `IUnifiedResultFactory`."

**Files:** XML docs on `UnifiedResult<T>`, `IUnifiedResult`, `IUnifiedResultFactory`

## 6. Acceptance criteria

### Per-phase gates

| Phase | Gate |
|---|---|
| **Phase 1** | Zero resource leaks under `dotnet-counters` stress test. AOT publish succeeds. All existing tests pass. |
| **Phase 2** | Every `Configure*` method follows the `Action<XxxOptions>` pattern. Every replaceable component has a public interface. Dead code removed. |
| **Phase 3** | New user from `dotnet new` to deployed `/healthz` in <5 minutes. Cron jobs default to safe concurrency. OpenAPI schema matches actual response shape. |

### Cross-cutting checks (every phase)

- `dotnet build` — zero errors, zero warnings
- `dotnet pack` — succeeds
- CHANGELOG updated in the same commit
- No behavior change without explicit opt-in (unless Phase 2 breaking changes, which are documented)
- Existing endpoints in `Sharkable.NativeTest` continue to respond identically

## 7. Risk assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| Phase 1 exception handler move breaks middleware that depended on specific ordering | Low — middleware ordering is not documented as a contract | Test with NativeTest full pipeline |
| Phase 2 `ConfigureJwt` migration is annoying for existing users | Medium — JWT is widely used | Keep old method as `[Obsolete]`, provide migration guide |
| Phase 3 health checks default change surprises existing deployments | Low — `/healthz` returns Healthy, no data exposure | Document in migration notes |
| Phase 3 cron concurrency change breaks long-running cron jobs | Low-Medium — existing concurrent jobs would need explicit config | Call out in CHANGELOG with migration instructions |

## 8. Files affected by phase

### Phase 1 (~6 files)
- `SharkableExtension.cs`
- `Cron/CronScheduler.cs`
- `Middleware/ResponseSizeExceededException.cs` (NEW)
- `Middleware/ETag/ETagMiddleware.cs`
- `Middleware/Idempotency/SharkIdempotencyMiddleware.cs`
- `AutoCrud/SqlSugar/Extensions/AutoCrudExtension.cs`

### Phase 2 (10 files)
- `DistributedTx/SagaExecutor.cs`
- `Cron/CronJob.cs` (ICronScheduler)
- `Shark/Options/SharkOption.cs`
- `Cron/CronJobState.cs` (ICronJobStore)
- `AutoCrud/SqlSugar/Options/SqlSugarOptions.cs`
- `Shark/Shark.cs`
- `AssemlyContext/AssemblyContext.cs`
- `UnifiedReults/UnifiedResult.cs`
- `Utils/AssemblyUtil.cs`
- `Middleware/SharkEndpointDsl.cs`
- `Shark/Extensions/SharkExtension.cs`

### Phase 3 (4 files)
- `Shark/Options/SharkOption.cs`
- `Cron/CronJobOptions.cs`
- `ConfigurationValidation/ConfigurationValidator.cs`

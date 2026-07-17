# 03 — Memory & Resource Lifetime

Leaks here are mostly slow-burn (timers, handles, caches, statics) rather than per-request churn. Per-request allocation issues live in [02-performance.md](02-performance.md).

---

### MEM-01 — Enabling idempotency hijacks the application-wide `IMemoryCache`
**Severity** P1 · **Effort** S · **Breaking** No (behavior-preserving for Sharkable; frees user code)
**Location:** `src/Sharkable/Shark/Extensions/SharkExtension.cs:60-76`
**Problem:** `services.AddSingleton<IMemoryCache>(_ => new MemoryCache(... SizeLimit = idempotencyOptions.MaxEntries))` adds a **second** app-wide `IMemoryCache` registration. Single-service resolution takes the *last* registration, so every user component injecting `IMemoryCache` silently receives Sharkable's private, size-limited idempotency cache — or, if the user registers theirs later, the idempotency store silently loses its size cap. This violates the minimal-intrusion principle. It is also inconsistent with `MemoryRateLimitStore`, which correctly owns a private cache.
**Proposal:** Remove the global registration; give `MemoryIdempotencyStore` its own private `MemoryCache` (same pattern as `MemoryRateLimitStore`, size from `SharkIdempotencyOptions.MaxEntries`), and make both stores `IDisposable` (see MEM-04). If a shared cache is ever desired, expose it explicitly via `IdempotencyStoreFactory`.

### MEM-02 — `AdaptiveLimitMonitor` is never disposed
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/SharkRateLimiterMiddleware.cs:24-28`, `src/Sharkable/Middleware/RateLimiting/AdaptiveLimitMonitor.cs:11-80`
**Problem:** The middleware creates the monitor in its ctor and nothing ever calls `Dispose` — the `Timer` and the `Process` handle live until process exit. Harmless for a single app run, but leaks per pipeline rebuild (test hosts, `WebApplicationFactory` suites, dynamic middleware rebuilds).
**Proposal:** Register the monitor as a singleton in DI and hook disposal to the container (container disposes `IDisposable` singletons automatically), or subscribe to `IHostApplicationLifetime.ApplicationStopped`.

### MEM-03 — Audit buffer: consumer dies silently on flush failure; dropped entries are invisible
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/AuditLogBuffer.cs:23-27, 57-121`
**Problem:** (a) `ConsumeAsync` only handles `OperationCanceledException`; an exception from `_logger.Log` (custom provider failure) or `FormatEntry` faults the consumer task permanently — all subsequent audit entries are silently dropped into a channel nobody reads. (b) The bounded channel uses `DropWrite`; when the 4096-entry buffer is full, entries vanish with no counter or log — operators cannot tell audit data is incomplete.
**Proposal:** Wrap the loop body in try/catch that logs (once, throttled) and continues. Track a dropped-entry counter (`Interlocked`) and emit it periodically / on shutdown; expose it on the future metrics surface (FEAT-02).

### MEM-04 — Private `MemoryCache` instances are not disposed
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/MemoryRateLimitStore.cs:42`, `src/Sharkable/Middleware/Idempotency/MemoryIdempotencyStore.cs` (after MEM-01)
**Problem:** `MemoryCache` is `IDisposable` (its expiration-scan timer otherwise runs forever); the stores hold one for the app lifetime and never dispose it.
**Proposal:** Implement `IDisposable` on both stores; DI disposes singletons that implement it. After MEM-01 the idempotency store owns its cache and can dispose it directly.

### MEM-05 — Global static mutable state prevents multi-app isolation and parallel tests
**Severity** P2 · **Effort** L · **Breaking** Yes (internal surface; public statics kept as façade)
**Location:** `src/Sharkable/Shark/Shark.cs:42-44`, `src/Sharkable/Shark/Internal/InternalShark.cs:12-25`, `src/Sharkable/AssemlyContext/AssemblyContext.cs:15`, `src/Sharkable/Middleware/Profiler/ProfilerMiddleware.cs:65-89`, `src/Sharkable/Middleware/Tracing/TracingMiddleware.cs:16-17`
**Problem:** `Shark.SharkOption`, `Shark.UseSharkOptions`, all of `InternalShark`, `AssemblyContext.Instance`, `ProfilerStore`, and the tracing `ActivitySource` are process-wide statics. Two apps (or two test hosts) in one process overwrite each other's options, service provider, buffers and profiler data. This is the root design debt behind several bugs (options double-invoke, DI/static divergence — ARCH-01/02).
**Proposal:** Introduce an internal `SharkAppContext` (options, lifetime state, buffers, profiler store, assembly list) registered as a singleton and injected into middleware/filters; keep the public statics as a façade bound to the *most recent* app for backward compatibility. Phase carefully (Phase 4): (1) create context + internal consumers migrate; (2) statics delegate to context; (3) document isolation guarantees for tests.

### MEM-06 — Warmup blocks the startup thread with sync-over-async
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/SharkableExtension.cs:199-204`
**Problem:** `warmup.WarmupAsync(...).GetAwaiter().GetResult()` during `UseShark()`. Startup-only, so no deadlock in practice, but it serializes warmup and cannot overlap with other startup work.
**Proposal:** Convert to an `IHostedService` (`SharkWarmupHostedService`) whose `StartAsync` awaits the warmup with the existing timeout; readiness gate (`StartupCompleted`) opens when it completes. Also enables FEAT-10 (multiple parallel warmups).

### MEM-07 — ETag spool `MemoryStream` is never disposed
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/ETag/ETagMiddleware.cs:147-225`
**Problem:** `CountingResponseBody` owns a `MemoryStream _spool` that is never disposed (GC reclaims it, but the pattern is inconsistent with the idempotency middleware, which `await using`s its buffer). The wrapper itself is also not disposed by the middleware.
**Proposal:** Dispose the spool in the middleware's `finally` (or make the wrapper `IDisposable` and `using` it). Cosmetic; bundle with PERF-08.

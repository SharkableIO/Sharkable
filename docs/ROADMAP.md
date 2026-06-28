# Roadmap

> **Core principle**: features auto-discover via conventions or activate via config. Users never change their `ISharkEndpoint` coding pattern, never implement new interfaces, and never call framework APIs in business code.

## v0.4.0 — 2026-06-28 ✅

- [x] Deprecate `[SharkEndpoint]` / `[SharkMethod]` / `SharkHttpMethod` and related reflection infrastructure (`IDependencyReflectorFactory`, `DependencyReflectorFactory`, `Reflector`, `ReflectorExtension`) — migrate to `ISharkEndpoint`
- [x] **Startup configuration self-check** — `ConfigurationValidator` validates JWT, multi-tenant configs at `AddShark()` time
- [x] **Graceful shutdown** — K8s-friendly SIGTERM handling: health check → 503, drain requests, then shutdown
- [x] **Audit trail batch/async write** — Channel-based buffer + background flush (`AsyncWrite`, `BatchSize`, `FlushInterval`)
- [x] **Idempotency distributed store** — `IIdempotencyStore` swappable via `TryAddSingleton` or `IdempotencyStoreFactory`
- [x] **Rate limiting distributed store** — `IDistributedRateLimitStore` + `SharkRateLimiterMiddleware`, `MemoryRateLimitStore` default, `RateLimitStoreFactory`
- [x] **Sharkable.Cache.Redis NuGet plugin** — Redis-backed `IIdempotencyStore` + `IDistributedRateLimitStore`, `AddSharkableRedis()`

---

## Phase 1 — Existing feature hardening (zero-intrusion)

| # | Feature | Value | Intrusion | Status |
|---|---------|-------|-----------|--------|
| 1 | **Startup configuration self-check** — validate JWT, rate limiting, multi-tenant configs at `AddShark()` time with clear error messages | Prevent misconfiguration | Zero — auto-runs | ✅ v0.4.0 |
| 2 | **Graceful shutdown** — K8s-friendly SIGTERM handling: health check → 503, drain requests, then shutdown | Production necessity | Zero — K8s-native | ✅ v0.4.0 |
| 3 | **Audit trail batch/async write** — buffer + background flush instead of sync writes | Performance | Zero — internal mechanism only | ✅ v0.4.0 |
| 4 | **Compile-time route conflict detection** — Roslyn Analyzer that catches `GET /api/orders/{id}` registered twice before runtime | Quality assurance | Zero — ships with NuGet | |

## Phase 2 — Observability

| # | Feature | Value | Intrusion |
|---|---------|-------|-----------|
| 5 | **Built-in distributed tracing** — ActivitySource + W3C `traceparent` propagation, auto `X-Trace-Id` header | Observability | Zero — `ActivitySource` |
| 6 | **Extensible health checks** — auto-register DB connectivity (SqlSugar), JWT config validity, custom checks | Operations | Config only |
| 7 | **Lightweight profiler panel** — per-request latency, memory delta, slow-request TOP10, dev-only endpoint | Debugging | Config only |

## Phase 3 — Distributed / cluster support

| # | Feature | Value | Intrusion | Status |
|---|---------|-------|-----------|--------|
| 8 | **Idempotency distributed store interface** — `IIdempotencyStore` + `TryAddSingleton`, `MemoryIdempotencyStore` default, users plug Redis/DB | Cluster HA | Config only | ✅ v0.4.0 |
| 9 | **Multi-tenant data source isolation** — `ISqlSugarClient` auto-switches connection string per tenant via DI scope | SaaS | Config only | |
| 10 | **Rate limiting distributed store interface** — `IDistributedRateLimitStore` + `MemoryRateLimitStore` default, `SharkRateLimiterMiddleware`, `Sharkable.Cache.Redis` plugin | Cluster HA | Config only | ✅ v0.4.0 |
| 11 | **Adaptive rate limiting** — dynamically adjust permit limit based on CPU/GC metrics | Robustness | Config only | |

## Phase 4 — Developer experience & polish

| # | Feature | Value | Intrusion |
|---|---------|-------|-----------|
| 12 | **Auto ETag / conditional requests** — SHA256 content hashing, `304 Not Modified` for GET endpoints | Cache optimization | Zero — auto |
| 13 | **Response compression** — auto-enable for GET endpoints, skip already-compressed content | Performance | Config only |
| 14 | **OpenAPI example generation** — infer realistic examples from type names + XML docs | DX | Zero — auto |
| 15 | **Error message localization** — `Accept-Language` driven `UnifiedResult` error messages via resource files | i18n | Config only |
| 16 | **AutoCrud AOT zero rd.xml** — Source Generator emits rd.xml content at compile time, user never touches it | AOT experience | Zero — Source Generator |
| 17 | **Soft-delete global filter** — entity implements `ISoftDeletable`, AutoCrud auto-filters `IsDeleted = false` | Data layer | Entity marker interface |
| 18 | **BackgroundService enhancement** — auto health reporting, graceful stop, retry policy, execution tracing | Background jobs | Zero — auto |
| 19 | **ProblemDetails (RFC 7807) compatibility** — `UnifiedResult<T>` auto-maps to ProblemDetails format | Interop | Zero — auto |

## Excluded (high-intrusion)

| Feature | Reason |
|---------|--------|
| CQRS-lite (ICommand/IQuery) | Requires rewriting every endpoint class |
| Modular ISharkModule | Requires large restructure |
| Strongly-typed IDs (OrderId, UserId) | Requires changing all method signatures |
| Smart enums | Requires changing all enum definitions |
| Cache tag invalidation | Requires changing write endpoints |
| API test runner | Requires writing new test code |
| Lightweight gateway | Requires creating a new project |
| Source Generator SDK | Requires new package + client code |

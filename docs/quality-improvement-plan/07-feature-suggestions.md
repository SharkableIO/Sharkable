# 07 — Feature Suggestions

New capabilities, all designed to be **opt-in, AOT-safe, additive** (no breaking changes; defaults preserve current behavior). Ordered by expected user value. Each lists a proposed public surface — final naming decided at implementation time. FEAT-01…08 are recommended for the v0.9.0 wave; FEAT-09…15 are triage candidates for v1.0.

---

### FEAT-01 — Security headers middleware (opt-in)
**Value:** High · **Effort** M
Every API needs `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, and often CSP / HSTS / `Permissions-Policy`; today users hand-roll it.
**Surface:**
```csharp
opt.ConfigureSecurityHeaders(h => {
    h.ContentTypeOptions = true;              // default true once enabled
    h.FrameOptions = "DENY";
    h.ReferrerPolicy = "no-referrer";
    h.ContentSecurityPolicy = "default-src 'none'";
    h.PermissionsPolicy = "geolocation=()";
});
```
All values configurable, header names validated by the existing `ConfigurationValidator` header-name rule.

### FEAT-02 — Framework metrics via `System.Diagnostics.Metrics` (zero-dependency)
**Value:** High · **Effort** M
Tracing exists, but there are no counters. A `Meter("Sharkable")` with counters/histograms is free under AOT and flows into OpenTelemetry automatically.
**Counters:** `sharkable.requests`, `sharkable.ratelimit.rejected`, `sharkable.idempotency.{hit,miss,conflict}`, `sharkable.auth.failures`, `sharkable.audit.dropped` (feeds MEM-03), `sharkable.cron.{runs,failures}`, `sharkable.saga.{completed,compensated}`.
**Surface:** `opt.ConfigureMetrics(m => m.Enabled = true)` — default off; meter name configurable.

### FEAT-03 — `IAuditSink` abstraction for the audit trail
**Value:** High · **Effort** M · Depends on ARCH-05c
Audit today goes only to `ILogger`. A sink interface lets users ship entries to Seq/Elastic/Kafka without replacing the middleware.
**Surface:** `IAuditSink.WriteBatchAsync(IReadOnlyList<AuditLogEntry>, CancellationToken)`; `SharkOption.AuditSinkFactory`; default sink = current logger behavior (structured + formats preserved).

### FEAT-05 — Per-endpoint distributed rate-limit policies
**Value:** High · **Effort** M · Depends on ARCH-10
```csharp
[SharkRateLimit(limit: 10, windowSeconds: 60)]   // on ISharkEndpoint class or via .SharkRateLimit(...) DSL
app.MapPost("/login", ...);
```
Middleware reads endpoint metadata; falls back to global options. Complements (does not replace) the built-in named-policy path (`SharkRequireRateLimiting`).

### FEAT-06 — Idempotency metadata attributes
**Value:** Medium · **Effort** S–M · Fixes BUG-10 properly
`[SharkIdempotent(ttlSeconds: 3600)]` to opt in per endpoint (instead of global), `[SharkNoIdempotency]` to opt out (streaming/SSE endpoints). Middleware consults metadata before buffering.

### FEAT-07 — Response cache profile attribute
**Value:** Medium · **Effort** S
ETag handles validation; nothing sets `Cache-Control` max-age per endpoint today.
```csharp
[SharkCacheProfile(durationSeconds: 60, varyByHeader: "Accept-Language", privateOnly: true)]
```
Emits `Cache-Control` (+ optional `Vary`) via endpoint metadata; composes with ETag and output cache.

### FEAT-08 — Request-timeout DSL
**Value:** Medium · **Effort** S
Thin mapping over `AddRequestTimeouts` (built into ASP.NET Core): `opt.ConfigureRequestTimeouts(t => t.DefaultPolicy = ...)` + `.SharkRequestTimeout(ms)` endpoint DSL, mirroring `SharkRequireRateLimiting` / `SharkCacheOutput`.

### FEAT-09 — Unified validation error shape
**Value:** Medium · **Effort** S
Validation failures currently join messages with `"; "`. Offer `ValidationErrorMode.Messages | ProblemDetails` — ProblemDetails mode emits RFC 7807 with an `errors` object (field → messages), consistent with `UseProblemDetails`.

### FEAT-10 — Parallel warmup services
**Value:** Low–Medium · **Effort** S · Depends on MEM-06
Allow multiple `IWarmupService` registrations, run in parallel with individual timeouts, aggregate failures into the readiness gate.

### FEAT-11 — `Sharkable.Testing` helpers
**Value:** Medium · **Effort** M
`WebApplicationFactory` wiring helpers, fakes for `IIdempotencyStore`/`ICronJobStore`/`ISagaStore`, assertion helpers for unified-result shape. Separate package so the core stays dependency-free.

### FEAT-12 — Group/endpoint lifecycle hooks
**Value:** Low–Medium · **Effort** S · Extends ARCH-09
`SharkOption.GroupConvention` (per group) plus `EndpointConvention` (per endpoint) — single place for auth policies, filters, `ProducesResponseType` conventions.

### FEAT-13 — OpenAPI ergonomics
**Value:** Low–Medium · **Effort** S
Simplified transformer registration (`opt.AddOpenApiOperationTransformer(...)` without touching `OpenApiOptions`), auto-generated error-response schemas from `ExceptionHandlerOptions.Map<T>` registrations, example generation hooks.

### FEAT-14 — Source generator for discovery (long-term)
**Value:** High for AOT purity · **Effort** L (multi-week)
Generate the endpoint/DI/validator registration code at compile time — eliminates all remaining startup reflection (including `AddShark(Assembly[])` scanning), gives instant startup and full trim-safety. Incremental generator; runtime scanning stays as fallback. Track as a separate RFC before starting.

### FEAT-15 — `IApiKeyValidator` with per-key principals
**Value:** Medium · **Effort** M · Depends on ARCH-05d
Validation result carries claims/roles + optional per-key rate-limit multiplier; default implementation = current static array. Enables multi-tenant API-key scenarios without JWT infrastructure.

---

## Adoption notes

- Every feature above is disabled/absent by default; enabling one must not change any other behavior (minimal intrusion).
- Every options type gets `ConfigurationValidator` coverage from day one (existing pattern).
- Every feature ships with: XML docs, CHANGELOG entry, docs-site page (EN + zh-cn), and a NativeTest endpoint when it touches the request path (AOT proof).

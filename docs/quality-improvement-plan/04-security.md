# 04 — Security

The SHARK-SEC rounds (001–024, see `local/security-audit.md` and CHANGELOG) already closed the Critical/High classes: algorithm confusion, timing oracles, unbounded buffers, unauthenticated admin endpoints, credential leakage via Scalar, header injection, log injection. What remains is smaller — mostly consistency gaps and safer defaults.

---

### SEC-01 — `/healthz` leaks per-check exception messages and data to anonymous callers
**Severity** P1 · **Effort** S · **Breaking** No (opt-out)
**Location:** `src/Sharkable/Middleware/HealthCheckEndpoint.cs:78-85`
**Problem:** The response includes `e.Value.Exception?.Message` and `e.Value.Data` for every registered check. SHARK-SEC-M004 already removed exactly this leak from `JwtHealthCheck`'s *description* — but the endpoint re-exposes it for **any** check (DB drivers, Redis, SqlSugar health checks are chatty: server addresses, SQLSTATE codes, connection-string fragments). `/healthz` is anonymous by design.
**Proposal:** Add `SharkOption.HealthCheckDetails` (`HealthCheckDetailLevel.StatusOnly | Description | Full`, default `Description` in Development / `StatusOnly` otherwise). Never emit `Exception.Message` outside `Full`. Optionally support `HealthCheckRequireApiKey` mirroring the profiler/cron gates (share the validator from PERF-06).

### SEC-02 — Production error responses include raw `exception.Message`
**Severity** P2 · **Effort** S · **Breaking** Maybe (response body change; opt-out keeps compat)
**Location:** `src/Sharkable/ExceptionHandler/ExceptionHandlerOptions.cs:70-85`
**Problem:** `GetErrorMessage` returns `exception.Message` whenever `IsDevelopment == false`. Messages from deep layers (SQL, file system, HttpClient) routinely contain server names, query text, absolute paths. Stack traces were gated by SHARK-SEC-M003; the message channel was not.
**Proposal:** Default production behavior → generic message (`"An error occurred."`) with the real message logged; add `ExceptionHandlerOptions.IncludeExceptionMessage` (default `false`) for opt-in passthrough; `IsDevelopment` continues to show the stripped detail. Ship in a minor version with a migration note.

### SEC-03 — OpenAPI document + Scalar UI are served in every environment by default
**Severity** P2 · **Effort** S · **Breaking** Maybe (deployment-visible; opt-in restores)
**Location:** `src/Sharkable/OpenApi/SwaggerExtension.cs:129-142`, `src/Sharkable/Shark/Options/SharkOption.cs:28`
**Problem:** `UseOpenApi` defaults to `true` and both `/openapi/v1.json` and `/scalar/v1` are mapped unconditionally — full API surface disclosure in production unless the user explicitly opts out. Credential pre-fill was already gated (SHARK-SEC-009); the surface itself was not.
**Proposal:** Add `SharkOption.OpenApiEnvironments` (default: all environments — no breaking change) or a boolean `ScalarDevelopmentOnly` (default `false` now, flip to `true` at the next major). At minimum, emit a startup warning when Scalar is mapped outside Development. Document the hardening recipe.

### SEC-04 — Admin endpoints duplicate (and drift from) the API-key validation logic
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Cron/CronAdminEndpoint.cs:75-93`, `src/Sharkable/Middleware/Profiler/SharkProfilerEndpoint.cs` (gate), `src/Sharkable/Middleware/ApiKeyMiddleware.cs:38-47`
**Problem:** The constant-time hash comparison is copy-pasted across three sites; the cron copy reads static options (no hot-reload) while the filter uses `IOptionsMonitor`. Future hardening must be applied N times (it already drifted once).
**Proposal:** Single internal `ApiKeyValidator` service (PERF-06) used by all gates. Also unify the admin surface under one configurable base path (ARCH-13).

### SEC-05 — `X-Forwarded-For` / proxy trust is documented but not enforced or validated
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/SharkRateLimiterOptions.cs:53-64`, tenant `FromHost` resolution
**Problem:** The default rate-limit partition uses `RemoteIpAddress`; behind a proxy it collapses to the proxy IP (one limiter bucket for all users) unless the app configures `ForwardedHeadersMiddleware` with `KnownProxies`. Today this is only an XML-doc remark; nothing at runtime detects the misconfiguration.
**Proposal:** Startup warning (via `ConfigurationValidator`) when distributed rate limiting is enabled and `ForwardedHeadersOptions.KnownProxies/KnownNetworks` are empty **and** the app resolves `RemoteIpAddress`-based keys. Pure guidance; no behavior change.

### SEC-06 — JWT health check hits the OIDC discovery endpoint unthrottled on every probe
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/HealthChecks.cs:52-83`
**Problem:** Each `/healthz` call triggers an outbound HTTPS GET to the authority. k8s `readinessProbe` at 5 s intervals × N replicas = steady load on the IdP; an IdP rate-limit/ban then flaps every pod unhealthy (self-inflicted DoS). There is a 5 s timeout but no result caching.
**Proposal:** Cache the probe result for a configurable TTL (`JwtHealthCheckCacheDuration`, default 30 s). Outbound auth checks belong in readiness, not in the hot path.

---

## Explicitly out of scope (already handled)

- Constant-time API-key comparison — SHARK-SEC-008.
- Scalar credential pre-fill outside Development — SHARK-SEC-009.
- Audit header/query redaction + query length cap — SHARK-SEC-010 / M009.
- Correlation-id log injection — SHARK-SEC-M020.
- Profiler/cron admin unauthenticated — SHARK-SEC-015/016 (404 fail-closed).
- Idempotency key length, response-size caps, fingerprint body cap — SHARK-SEC-L002 / M008 / M021.
- Mass-assignment on AutoCrud — SHARK-SEC-006 (SqlSugar repo, `[CrudAllow]`).

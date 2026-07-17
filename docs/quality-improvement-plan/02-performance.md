# 02 — Performance

Hot-path (per-request) items first, startup-path items second. Validate each with the BenchmarkDotNet suite (PROC-02) before/after.

---

### PERF-01 — `DefaultUnifiedResultFactory` is allocated per call at five call sites
**Severity** P1 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/UnifiedReults/Extensions/UnifiedResultExtension.cs:15`, `src/Sharkable/ExceptionHandler/UnifiedResultWrapFilter.cs:20`, `src/Sharkable/Middleware/ProblemDetailsResult.cs:34`, `src/Sharkable/Validation/ValidationFilter.cs:39`, `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs:215`
**Problem:** `Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory()` — the `new` runs on every wrapped response / error. Also see ARCH-01: DI already registers `IUnifiedResultFactory` but no call site uses it.
**Proposal:** One static `DefaultUnifiedResultFactory.Instance` (stateless, thread-safe) + resolve the configured factory once at pipeline build and cache it. Single resolution helper, e.g. `internal static IUnifiedResultFactory ResolveFactory()`.

### PERF-02 — Source-generated JSON context forces indented output for every wrapped response
**Severity** P1 · **Effort** S · **Breaking** No (payload whitespace only)
**Location:** `src/Sharkable/UnifiedReults/Context/UnifiedResultSourceContext.cs:9`
**Problem:** `[JsonSourceGenerationOptions(WriteIndented = true)]` contradicts the SHARK-SEC-L001 decision (compact JSON by default). Every auto-wrapped response is ~2× the bytes it should be.
**Proposal:** `WriteIndented = false`. Users who want pretty JSON can set it on their own `JsonOptions`.

### PERF-03 — Attribute-based DI scanning is O(attributed types × all types)
**Severity** P1 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/DependencyInjection/Extensions/AttributeServiceExtension.cs:96-118`
**Problem:** For **each** type carrying `[ScopedService]` etc., the code re-enumerates `assembliesToBeScanned.SelectMany(a => a.GetTypes())` to find implementations — quadratic in assembly type count. On large solutions this is seconds of startup and large LOH allocations. Also `assembly.GetTypes()` is called repeatedly across the three attribute passes and the marker-interface pass.
**Proposal:** Materialize the type list once per `AddShark` call; build interface → implementations lookup tables in a single pass; then do all registrations from the index. Side benefit: one place to add the `ReflectionTypeLoadException` guard (ARCH-12).

### PERF-04 — Audit middleware allocates a redaction `HashSet` per request
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/AuditTrailMiddleware.cs:206`
**Problem:** `new HashSet<string>(_options.RedactQueryParams, ...)` runs inside `RedactQueryString` on every audited request, while the header equivalent (`_redactHeaders`) was already cached in the ctor.
**Proposal:** Cache `_redactQueryParams` in the ctor the same way.

### PERF-05 — Audit middleware builds log strings even when the log level is disabled
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/AuditTrailMiddleware.cs:50-69`
**Problem:** `RedactQueryString(...)` and `CaptureHeaders(...)` (a full JSON serialization of every request header) run unconditionally in the `finally` block; `_logger.IsEnabled` is only checked later inside `LogRequest`. With audit enabled but the category filtered out, every request still pays the allocations.
**Proposal:** Compute the effective level first (status code known in `finally`), check `IsEnabled` before capturing query/headers. Optionally add `AuditTrailOptions.IncludeHeaders` (default `true`) to skip header capture entirely.

### PERF-06 — API-key filter re-hashes every stored key on every request
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/ApiKeyMiddleware.cs:40-47`, `src/Sharkable/Cron/CronAdminEndpoint.cs:84-92`
**Problem:** `SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]))` inside the per-request loop — O(keys) hashes per request, and the same logic is duplicated in the cron admin endpoint (which also misses the `IOptionsMonitor` hot-reload that the filter has).
**Proposal:** Extract a shared `ApiKeyValidator` helper that caches the stored-key hashes and invalidates via `IOptionsMonitor<SharkOption>.OnChange`. Use it from both call sites (also fixes the DRY/hot-reload gap for cron admin and profiler gates).

### PERF-07 — Validation filter does `MakeGenericType` + DI resolution per argument per request
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Validation/ValidationFilter.cs:21-33`
**Problem:** `typeof(IValidator<>).MakeGenericType(arg.GetType())` and `RequestServices.GetService(...)` run for every non-null argument on every request. Reflection + DI lookup in the hot path; also an AOT annotation risk (AOT-04).
**Proposal:** Cache per argument type: `ConcurrentDictionary<Type, IValidator?>` (null = no validator, negative cache). Resolution is scoped, so cache `Func<IServiceProvider, IValidator?>` factories instead of instances.

### PERF-08 — ETag hex encoding allocates 33 strings per response
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/ETag/ETagMiddleware.cs:132-138`
**Problem:** `StringBuilder` + per-byte `b.ToString("x2")` — 32 intermediate strings per hashed response.
**Proposal:** `Convert.ToHexString(hashBytes)` (one allocation, vectorized).

### PERF-09 — Idempotency middleware: per-request `HttpMethod` allocation and double body copy
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs:52, 136-144`
**Problem:** (a) `_options.UnsafeMethods.Contains(new HttpMethod(context.Request.Method))` allocates an `HttpMethod` per request — compare method strings against a `HashSet<string>` (ordinal-ignore-case) instead. (b) `buffer.ToArray()` copies the entire buffered body a second time before storing; with the default 1 MiB cap that is up to 2 MiB extra per cacheable response.
**Proposal:** (a) string-set membership. (b) Store the buffer's internal array segment (`GetBuffer()` + length) or write into a pooled buffer; copy once.

### PERF-10 — Profiler samples GC memory twice per request by default
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/Profiler/ProfilerMiddleware.cs:22-31`
**Problem:** `GC.GetTotalMemory(false)` before and after every request. Cheap individually, but it serializes against GC bookkeeping and adds noise under high RPS; the numbers are approximate anyway (shared heap).
**Proposal:** Add `ProfilerOptions.TrackMemory` (default `false` — breaking-ish only for profiler consumers, note in changelog). Keep `Stopwatch` timing always on.

### PERF-11 — `HttpResponseExtension.WriteJsonAsync` allocates `JsonSerializerOptions` per call
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/JsonHelper/Extensions/HttpResponseExtension.cs:19-24, 33-38`
**Problem:** `options ?? new JsonSerializerOptions { WriteIndented = false }` per call — plus the non-generic `Serialize(data, type, options)` path is reflection-based (AOT-06).
**Proposal:** Cache a static readonly default options instance; route through the source-generated context where possible; mark the helpers `[RequiresUnreferencedCode]` or obsolete them in favor of `WriteAsJsonAsync` with the resolver chain.

---

## Startup-path notes (fold into PERF-03)

- `WireSharkEndpoint`, `AddServicesWithAttributeOfType` (×3 passes), `AddServicesWithInterfaceMarker`, `AddValidators` each call `assembly.GetTypes()` independently — one shared materialized array per assembly removes most of the repeated reflection (with the ARCH-12 exception guard).
- Non-AOT `Utils.GetAssemblies()` loads **every referenced assembly** (`Assembly.Load` per reference) — document that startup cost scales with dependency graph; AOT path (explicit assemblies) is unaffected.

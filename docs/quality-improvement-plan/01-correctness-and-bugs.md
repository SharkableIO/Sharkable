# 01 — Correctness & Bugs

Verified against baseline `ecb25c0`. Each item is independently fixable; ship with a regression test (PROC-01).

---

### BUG-01 — Cron shutdown cancellation token is created but never wired into the job
**Severity** P1 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Cron/SharkCronHostedService.cs:44-63`, `src/Sharkable/Cron/CronScheduler.cs:121-179`
**Problem:** The hosted service creates `jobCts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken)` with a comment (SHARK-SEC-M012) claiming in-flight jobs are cancelled on shutdown — but the token is never passed to `ExecuteJobAsync`. It only cancels *scheduling* of the `Task.Run`, and is disposed in `finally`. `ExecuteJobAsync` builds its own standalone CTS from `job.Options.Timeout`, so a long-running job **survives `app.StopAsync()`**, exactly the bug the comment says was fixed.
**Proposal:** Add a `CancellationToken shutdownToken` parameter to `ExecuteJobAsync`; link it with the per-job timeout CTS (`CreateLinkedTokenSource`) and pass the linked token to `job.Handler(...)`. Also forward it into `Task.Delay(RetryDelay, token)` (see BUG-04).

### BUG-02 — Exception handler writes to started/aborted responses and treats client disconnects as errors
**Severity** P1 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/ExceptionHandler/SharkExceptionHandlerMiddleware.cs:17-38`
**Problem:** (a) `catch (Exception)` also catches `OperationCanceledException` thrown when the client aborts — logged as an error and followed by an attempt to write a response on a dead connection. (b) If the exception escapes after `Response.HasStarted` (e.g. mid-stream), `Response.StatusCode = ...` throws `InvalidOperationException`, masking the original error with an unhandled secondary exception.
**Proposal:** Before handling, rethrow when `exception is OperationCanceledException && context.RequestAborted.IsCancellationRequested`; when `context.Response.HasStarted`, log and rethrow instead of writing. Add tests for both paths.

### BUG-03 — Saga compensation runs with the already-cancelled token
**Severity** P1 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/DistributedTx/SagaExecutor.cs:168, 188, 200-218`
**Problem:** On cancellation (or step failure after caller cancellation) `CompensateAsync(..., ct, ...)` receives the cancelled token. Compensation steps that honor the token abort immediately, so the saga neither completes nor rolls back — the worst possible outcome for a consistency primitive.
**Proposal:** Give compensation its own CTS (configurable `CompensationTimeout`, default e.g. 60 s) that is **not** linked to the execution token. Keep passing the execution token to forward steps only. Add `SagaExecutor.CompensationTimeout` property with the same setter validation style as `LockTtl`.

### BUG-04 — Cron job timeout is reported as failure and retried; retry delay ignores shutdown
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Cron/CronScheduler.cs:138-157`
**Problem:** (a) `catch (Exception ex)` catches the `OperationCanceledException` produced by the job's own timeout CTS, records it as `LastError`, and counts it as a retryable failure — a timed-out job immediately re-runs up to `RetryCount` times. (b) `Task.Delay(job.Options.RetryDelay)` has no token, so shutdown waits for the full delay.
**Proposal:** Distinguish timeout-cancellation (`catch (OperationCanceledException) when (cts.IsCancellationRequested)` → record timeout, do not retry) from shutdown-cancellation (rethrow). Pass the shutdown-linked token (BUG-01) into the retry delay.

### BUG-05 — Graceful shutdown middleware ignores the configured status code
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/GracefulShutdownMiddleware.cs:19-25`
**Problem:** `statusCode` is read from `GracefulShutdownOptions.ShutdownStatusCode` but `ProblemDetailsResult.WriteAsync(context, 503, ...)` is hard-coded — the configured value only affects the (unused) local variable.
**Proposal:** Pass `statusCode` through. Regression test with a non-default code.

### BUG-06 — Auto-wrap enable/disable semantics are inconsistent and docs contradict the code
**Severity** P2 · **Effort** M · **Breaking** Maybe (doc-only if we keep behavior)
**Location:** `src/Sharkable/Shark/Options/SharkOption.cs:42-47`, `src/Sharkable/Shark/Options/UseSharkOptions.cs:12-17`, `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:110-113`
**Problem:** (a) `SharkOption.EnableAutoWrap` defaults to `true` but its XML doc says *"Default is false (opt-in)"*; `UseSharkOptions.EnableAutoWrap` doc also says opt-in. (b) Effective value is `SharkOption.EnableAutoWrap || UseSharkOptions.EnableAutoWrap` — once enabled at `AddShark` time there is **no way to turn it off** at `UseShark` time, and the OR-semantics are undocumented. (c) `[SharkDontWrap]` is class-wide per group: if any endpoint class in a merged group carries it, wrapping is disabled for the whole group (`hasDontWrap` is computed per merged group, not per class).
**Proposal:** Define one precedence chain: `UseSharkOptions.EnableAutoWrap` (nullable tri-state: `bool?`) overrides `SharkOption.EnableAutoWrap`; fix both XML docs; evaluate `SharkDontWrapAttribute` per endpoint class inside the convention instead of per merged group. If behavior changes, note it in the migration guide.

### BUG-07 — `SharkRateLimiterOptions.MaxEntries` doc promises `-1` disables the cap; the store throws for `<= 0`
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/SharkRateLimiterOptions.cs:130-139`, `src/Sharkable/Middleware/RateLimiting/MemoryRateLimitStore.cs:38-43`
**Problem:** The XML doc says *"Set to -1 to disable the cap"*, but the ctor throws `ArgumentOutOfRangeException` for `<= 0`, and `ConfigurationValidator` also rejects it — a user following the doc gets a startup crash.
**Proposal:** Either implement `-1` = uncapped (create `MemoryCache` without `SizeLimit`) or remove the sentence from the doc. Recommendation: implement it — it is useful for trusted-internal services, and the validator already warns for large values.

### BUG-08 — Route formatting uses culture-sensitive `ToLower()`
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Extensions/StringExtension.cs:169`
**Problem:** `EndpointFormat.ToLower => str.ToLower()` is culture-sensitive: under `tr-TR`, `"I"` lowercases to `"ı"` (dotless i), producing different route URLs depending on the server's locale.
**Proposal:** `str.ToLowerInvariant()`. Also audit `ToCamelCase` (`char.ToLower(str[0])`, same issue) — use `char.ToLowerInvariant`.

### BUG-09 — Rate-limit headers are formatted with the current culture
**Severity** P2 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Middleware/RateLimiting/SharkRateLimiterMiddleware.cs:48-50`
**Problem:** `limit.ToString()` / `remaining.ToString()` / `TotalSeconds.ToString()` use the ambient culture; under cultures with non-ASCII digits the `X-RateLimit-*` headers contain non-ASCII numerals, breaking HTTP header semantics and clients.
**Proposal:** `CultureInfo.InvariantCulture` for all header values (also `Retry-After` in `SharkIdempotencyMiddleware.cs:66`).

### BUG-10 — Idempotency middleware silently breaks streaming / SSE endpoints
**Severity** P2 · **Effort** M · **Breaking** No
**Location:** `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs:95-154`
**Problem:** The middleware replaces `Response.Body` with a `MemoryStream` and only copies it back after `_next` completes. For SSE / streaming endpoints (which rely on flush-to-client), the client receives nothing until the stream ends — a silent behavioral break when idempotency is enabled globally. `FlushAsync` calls are swallowed by the buffer.
**Proposal:** Detect streaming responses early (Content-Type `text/event-stream`, or endpoints marked with a new `[SharkNoIdempotency]` metadata attribute — see FEAT-06) and pass through without buffering, releasing the in-flight slot. Document the limitation either way.

### BUG-11 — Cron job state mutated without synchronization
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/Cron/CronScheduler.cs:98-113, 133-134, 160-162, 223-231`
**Problem:** `CronJobState` fields (`NextRun`, `IsRunning`, `LastRun`, `Paused`…) are mutated from the scheduler loop, `ExecuteJobAsync`, and admin pause/resume paths with no lock — the `_entries` lock only guards the dictionary. Torn reads on the admin endpoint are benign but confusing; `IsRunning` races can skip a legitimate run.
**Proposal:** Guard state mutation with the entry lock (or make state updates go through small interlocked helpers). Low impact; fold into Phase 3.

### BUG-12 — Legacy attribute mapper mutates runtime-cached attribute instances
**Severity** P3 · **Effort** S · **Breaking** No
**Location:** `src/Sharkable/SharkEndpoint/Extensions/EndPointExtension.cs:418-422, 451-456`
**Problem:** `endpointAttribute.Group ??= t.Name; endpointAttribute.Group = ...FormatAsGroupName()...` mutates the attribute object returned by `GetCustomAttributes`, which the runtime may cache/share. Subsequent enumerations see the already-formatted value (idempotency bug for SnakeCase pipelines).
**Proposal:** Copy to locals instead of mutating. Legacy (non-AOT) path, low priority.

---

## Regression-test checklist (PROC-01)

- [ ] BUG-01: host shutdown cancels an in-flight cron job (assert handler observes cancellation ≤ 1 s).
- [ ] BUG-02: client abort mid-request produces no error log entry and no secondary exception.
- [ ] BUG-03: saga cancelled mid-flight still runs all compensations to completion.
- [ ] BUG-04: timed-out job records timeout, performs zero retries.
- [ ] BUG-05: custom `ShutdownStatusCode` (e.g. 502) is honored.
- [ ] BUG-08/09: run formatting + header tests under `tr-TR` culture.

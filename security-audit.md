# Security Audit Log

## SHARK-SEC-002: OOM via attacker-controlled Content-Length in idempotency middleware

| Field | Value |
|---|---|
| **ID** | SHARK-SEC-002 |
| **Severity** | High |
| **Introduced in** | v0.3.0 (idempotency middleware) |
| **Fixed in** | Unreleased (next release) |
| **CWE** | [CWE-400: Uncontrolled Resource Consumption](https://cwe.mitre.org/data/definitions/400.html) |

### Description

The `SharkIdempotencyMiddleware` computed a SHA-256 fingerprint of the request body by allocating a `byte[]` buffer sized by the `Content-Length` request header with no upper bound:

```csharp
// vulnerable code path (removed)
var bodyLength = (int)(context.Request.ContentLength ?? 0);
byte[] body = bodyLength > 0
    ? await ReadBodyBytes(context.Request.Body, bodyLength)  // allocates bodyLength bytes
    : Array.Empty<byte>();
```

An attacker could send a request with a large `Content-Length` (e.g., `int.MaxValue`, ~2 GiB), causing the server to allocate an enormous buffer that exhausts available memory and triggers an OOM crash.

### Impact

Remote denial of service via memory exhaustion. No authentication required — the vulnerability is in middleware that processes every request carrying an `Idempotency-Key` header.

### Fix

1. **Added** `SharkIdempotencyOptions.MaxFingerprintBodySize` (default 64 KiB) — configurable upper bound for body bytes included in the fingerprint hash.
2. **Introduced** `IdempotencyFingerprint.ComputeAsync()` — reads the body incrementally in 4 KiB chunks via `IncrementalHash`, never materializing the full body. Stops reading after `min(Content-Length, MaxFingerprintBodySize)` bytes.
3. **Replaced** the vulnerable `Compute` + `ReadBodyBytes` call path with the bounded `ComputeAsync` path.
4. **Fixed chunked-transfer bypass** — when `Content-Length` is absent (chunked encoding) the body is now read incrementally via `ComputeAsync` with `contentLength = -1` sentinel, instead of falling through to the empty-body path. This prevents all chunked requests from sharing one fingerprint (which previously let an attacker replay any chunked request under any other chunked request's idempotency key).
5. **Added validation** — `SharkIdempotencyOptions.MaxFingerprintBodySize` setter throws `ArgumentOutOfRangeException` for `<= 0`.

### Verification

- `Content-Length: 2147483647` with a small body → only 64 KiB read, single 4 KiB buffer allocated, no OOM.
- `Content-Length: 0` → no body read, falls back to `Compute()` with empty span (unchanged).
- `Content-Length` absent (chunked) with body "abc" vs body "xyz" → distinct fingerprints (the `-1` contentLength sentinel is mixed into the hash, and bodies are hashed incrementally).
- `Content-Length` absent with no body → distinct from `Content-Length: 0` (sentinel `-1` differs from no length marker).
- `SharkIdempotencyOptions.MaxFingerprintBodySize = 0` → throws `ArgumentOutOfRangeException`.
- Normal requests with bodies ≤ 64 KiB → hashed in full, identical fingerprint behavior.

### Timeline

| Date | Event |
|---|---|
| 2026-06-30 | Vulnerability discovered during review of idempotency middleware resource usage |
| 2026-06-30 | Fix implemented and committed |

## SHARK-SEC-003: Shutdown drain `Thread.Sleep` blocks `ApplicationStopping` thread

| Field | Value |
|---|---|
| **ID** | SHARK-SEC-003 |
| **Severity** | Critical |
| **Introduced in** | v0.5.0 (graceful shutdown drain) |
| **Fixed in** | Unreleased (next release) |
| **CWE** | [CWE-400: Uncontrolled Resource Consumption](https://cwe.mitre.org/data/definitions/400.html) |

### Description
`SharkableExtension.cs` registered a sync `ApplicationStopping` callback that polled
`InternalShark.ActiveRequests` with `Thread.Sleep`. The polling thread was held by the
callback for up to `DrainTimeout` (default 30 s), preventing graceful shutdown from
returning and causing k8s `terminationGracePeriodSeconds` violations with abrupt SIGKILL
of in-flight requests.

### Fix
- Moved polling loop into `Task.Run(async () => ...)` so `ApplicationStopping` returns immediately
- Replaced `Thread.Sleep` with `await Task.Delay(..., drainCts.Token)`
- Fire-and-forget via `_ = Task.Run(...)` — the callback MUST NOT block the shutdown thread
- `OperationCanceledException` swallowed to avoid shutdown-time noise

> **Note:** Initial implementation chained `.GetAwaiter().GetResult()` on the `Task.Run`, which is sync-over-async and still blocked the `ApplicationStopping` callback thread for the full `DrainTimeout` window. Corrected to fire-and-forget so the callback returns immediately. The host no longer waits for the drain to finish; in-flight requests are covered by the deployment's shutdown grace period (e.g. k8s `terminationGracePeriodSeconds`).

### Verification
- `ApplicationStopping.Register` returns immediately (no thread blocking)
- `Task.Delay` honors `drainCts` and exits the loop on cancel
- Long-running endpoints (>5 min) no longer cause SIGKILL cascade

## SHARK-SEC-004: Saga `LockTtl` hard-coded and never renewed (split-brain)

| Field | Value |
|---|---|
| **ID** | SHARK-SEC-004 |
| **Severity** | High |
| **Introduced in** | v0.5.0 (distributed transactions / SAGA) |
| **Fixed in** | Unreleased (next release) |
| **CWE** | [CWE-662: Improper Synchronization](https://cwe.mitre.org/data/definitions/662.html) |
| **Cross-repo** | Companion change in `Sharkable.Cache.Redis` (C-2) |

### Description
`SagaExecutor.LockTtl` was hard-coded to 5 minutes and there was no renewal path. Any
saga whose steps ran longer than 5 minutes would see its Redis (or other TTL-based)
distributed lock expire mid-flight; another node could then `TryAcquireLockAsync` the
same `sagaId` and start a parallel execution, producing split-brain — duplicate side
effects and double-charges. The unconditional `KeyDelete` semantics in
`Sharkable.Cache.Redis` (also being fixed in parallel) made the collision even more
likely because a check-then-delete race window opened on every crash-recovery.

### Fix
- Made `LockTtl` configurable via a new `SagaExecutor(ISagaStore, ILogger, TimeSpan lockTtl)` constructor overload. Default remains 5 minutes.
- Added `SagaExecutor.LockRenewalInterval` (default `LockTtl / 3`) which triggers periodic `ISagaStore.RenewLockAsync` while work is in progress.
- Added `ISagaStore.RenewLockAsync(sagaId, ttl)` to the contract. Implemented as a **default interface method** (DIM, requires C# 8 / net8.0+) returning `Task.CompletedTask` so existing third-party `ISagaStore` implementations continue to compile without modification. `MemorySagaStore` uses the default; `RedisSagaStore` (cross-repo) overrides it to call `StringSetAsync(_lockPrefix + sagaId, token, LockTtl)`.
- Wrapped the step loop in a `Task` that renews the lock at `LockRenewalInterval` until the linked cancellation token fires.

> **Note:** Initial implementation added `RenewLockAsync` as a required interface member, breaking compile for any third-party `ISagaStore` implementer. Converted to a default interface method so `ISagaStore` is backward-compatible — old implementations keep working without source changes (they just don't get renewal, which is fine for in-process stores).

### Verification
- Saga execution with step duration `> LockTtl` no longer expires its lock.
- `MemorySagaStore` continues to behave identically (locks held until released or process exit).
- Existing third-party `ISagaStore` implementations compiled against Sharkable v0.5.4 continue to compile against the patched library unchanged.
- Cancellation / step failure / compensation all trigger `renewCts.Cancel()` and a clean `ReleaseLockAsync` in the `finally` block.
- Redis-based deployments require the cross-repo companion fix in `Sharkable.Cache.Redis` for renewal to actually extend TTL.

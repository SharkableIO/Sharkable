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

### Verification

- `Content-Length: 2147483647` with a small body → only 64 KiB read, single 4 KiB buffer allocated, no OOM.
- `Content-Length: 0` or absent → no body read, falls back to `Compute()` with empty span (unchanged).
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
- `OperationCanceledException` swallowed to avoid shutdown-time noise

### Verification
- `ApplicationStopping.Register` returns immediately (no thread blocking)
- `Task.Delay` honors `drainCts` and exits the loop on cancel
- Long-running endpoints (>5 min) no longer cause SIGKILL cascade

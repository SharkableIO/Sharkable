# Idempotency Middleware Design

Date: 2026-06-26
Feature: Idempotency middleware for unsafe HTTP methods (per `ROADMAP.md` item #3)
Branch: `feat/idempotency-middleware`

## Summary

Add an opt-in idempotency middleware that lets clients safely retry unsafe HTTP
requests (`POST` / `PUT` / `PATCH` / `DELETE`) by supplying an
`Idempotency-Key` header. The middleware caches the first response and replays
it on subsequent requests with the same key, blocks concurrent execution of
the same key, and rejects reuse of a key with a different request payload.
Activation is header-driven and gated by a global option flag; the feature
adds no new third-party dependencies.

## Goals

- Allow clients to safely retry non-idempotent operations (payments, orders,
  bookings) without risk of duplicate execution.
- Match the `Idempotency-Key` industry convention (Stripe / Square / PayPal /
  IETF draft `draft-ietf-httpapi-idempotency-key-header`).
- Single-instance in-memory store for v1, with a clean interface
  (`IIdempotencyStore`) so a distributed backend can be added later.
- AOT-compatible (no reflection, no dynamic code).

## Non-Goals

- Cross-instance / distributed idempotency (v1 is single-process only).
- Streaming responses (responses > 1 MiB are rejected).
- Per-endpoint opt-in attributes (the global flag + header is the only
  activation path in v1).

## 1. Public API

### 1.1 Option flag

Added to `SharkOption` in `src/Sharkable/Shark/Options/SharkOption.cs`:

```csharp
/// <summary>
/// When <c>true</c>, wires the idempotency middleware into the pipeline.
/// Requests carrying an <c>Idempotency-Key</c> header on an unsafe HTTP
/// method are deduplicated and replayed. Default is <c>false</c>.
/// </summary>
public bool EnableIdempotency { get; set; } = false;
```

### 1.2 Optional configuration callback

```csharp
/// <summary>
/// Configures the idempotency middleware. Called only when
/// <see cref="SharkOption.EnableIdempotency"/> is <c>true</c>.
/// </summary>
public void ConfigureIdempotency(Action<SharkIdempotencyOptions> configure)
```

### 1.3 `SharkIdempotencyOptions`

`src/Sharkable/Middleware/Idempotency/SharkIdempotencyOptions.cs`:

| Field | Type | Default | Description |
|---|---|---|---|
| `Ttl` | `TimeSpan` | `24h` | How long completed records are kept. |
| `InFlightTtl` | `TimeSpan` | `30s` | Auto-eviction for in-flight placeholders (crash safety). |
| `MaxKeyLength` | `int` | `255` | Reject keys longer than this with 400. |
| `MaxResponseSize` | `int` | `1_048_576` (1 MiB) | Responses whose buffered body exceeds this size are replaced with a 500 error. |
| `HeaderName` | `string` | `Idempotency-Key` | Request header name. |
| `ReplayedHeaderName` | `string` | `X-Idempotent-Replayed` | Response header set on replays. |
| `UnsafeMethods` | `IReadOnlySet<HttpMethod>` | `{POST, PUT, PATCH, DELETE}` | Methods that activate the middleware when the header is present. |

### 1.4 Integration point

`UseShark()` in `src/Sharkable/SharkableExtension.cs` is extended:

```csharp
if (Shark.SharkOption.EnableIdempotency)
    app.UseMiddleware<SharkIdempotencyMiddleware>();
```

`AddShark()` registers `IIdempotencyStore` → `MemoryIdempotencyStore` as
singleton when the flag is on.

## 2. File layout

```
src/Sharkable/Middleware/Idempotency/
├── SharkIdempotencyOptions.cs       # configuration record
├── IIdempotencyStore.cs             # storage interface
├── MemoryIdempotencyStore.cs        # IMemoryCache-backed implementation
├── IdempotencyRecord.cs             # completed-response record (sealed record)
├── IdempotencyFingerprint.cs        # SHA-256 helper
└── SharkIdempotencyMiddleware.cs    # the state machine
```

## 3. Storage

### 3.1 `IIdempotencyStore`

```csharp
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically reserves the key. Returns <c>true</c> if this caller
    /// now owns the in-flight slot; <c>false</c> if the key is already
    /// reserved or completed.
    /// </summary>
    bool TryReserve(string key, TimeSpan inFlightTtl);

    /// <summary>
    /// Returns the in-flight reservation owner, a completed record, or null.
    /// </summary>
    IdempotencyLookup Get(string key);

    /// <summary>
    /// Stores the completed response under the key. Replaces any in-flight
    /// placeholder. Applies the configured TTL.
    /// </summary>
    void Store(string key, IdempotencyRecord record, TimeSpan ttl);

    /// <summary>
    /// Removes the in-flight placeholder without storing a record. Called
    /// when the response should not be cached (5xx, 429, oversized).
    /// </summary>
    void Release(string key);
}

public abstract record IdempotencyLookup;
public sealed record IdempotencyInFlight() : IdempotencyLookup;
public sealed record IdempotencyHit(IdempotencyRecord Record) : IdempotencyLookup;
```

### 3.2 `MemoryIdempotencyStore`

- Backed by `IMemoryCache` (resolved from DI).
- `TryReserve` uses `cache.GetOrCreate` semantics with an `InFlight` marker;
  if the key exists it returns `false`.
- `Store` uses `cache.CreateEntry` + `SetValue(record)` + `AbsoluteExpirationRelativeToNow`.
- `Release` uses `cache.Remove(key)`.

### 3.3 `IdempotencyRecord`

```csharp
public sealed record IdempotencyRecord(
    string Key,
    string Fingerprint,           // SHA-256 hex (64 chars)
    int StatusCode,
    string ContentType,
    ReadOnlyMemory<byte> Body,
    DateTimeOffset CompletedAt);
```

`ReadOnlyMemory<byte>` avoids an extra copy when buffering; the byte array
is owned by the record and lives for the TTL.

## 4. Fingerprint

Computed in `IdempotencyFingerprint.Compute`:

```csharp
public static string Compute(string method, PathString path, ReadOnlySpan<byte> body)
{
    using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
    sha.AppendData(Encoding.ASCII.GetBytes(method.ToUpperInvariant()));
    sha.AppendData((byte)'\n');
    sha.AppendData(Encoding.ASCII.GetBytes(path.Value ?? "/"));
    sha.AppendData((byte)'\n');
    sha.AppendData(body);
    return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
}
```

- Lower-case hex, 64 characters.
- `method` is upper-case (`POST`, `PUT`, …).
- `path` is the unescaped `PathString.Value` (e.g. `/api/orders`).
- `body` is the raw request body bytes; if `Content-Length` is 0 the body
  is empty.

## 5. Middleware state machine

Pseudocode for `SharkIdempotencyMiddleware.InvokeAsync`:

```
header = request.Headers[options.HeaderName]
if header is missing or whitespace:
    await next(context); return

key = header.ToString()
if !IsValidKey(key, options):
    await WriteUnifiedResult(context, 400, "invalid_idempotency_key",
        $"Key must be 1..{options.MaxKeyLength} printable ASCII characters.");
    return

if !options.UnsafeMethods.Contains(request.Method):
    await next(context); return   # safe methods pass through, header ignored

store = context.RequestServices.GetRequiredService<IIdempotencyStore>()

if !store.TryReserve(key, options.InFlightTtl):
    match store.Get(key):
        case IdempotencyInFlight:
            context.Response.Headers["Retry-After"] = "1"   # 1 second (delta-seconds)
            await WriteUnifiedResult(context, 409, "idempotency_in_progress", null);
            return
        case IdempotencyHit hit:
            if hit.Record.Fingerprint != Compute(request):
                await WriteUnifiedResult(context, 422, "idempotency_key_conflict",
                    "Idempotency-Key was reused with a different request payload.");
                return
            await Replay(context, hit.Record, options);
            return

# We own the in-flight slot. Execute downstream with response buffering.
originalBody = context.Response.Body
await using var buffer = new MemoryStream()
context.Response.Body = buffer
try:
    await next(context)

    if buffer.Length > options.MaxResponseSize:
        logger.LogWarning(
            "Idempotency response for key {Key} exceeds {Max} bytes; " +
            "rejecting and releasing in-flight slot.",
            key, options.MaxResponseSize);
        store.Release(key);
        context.Response.StatusCode = 500;
        await WriteUnifiedResult(context, 500, "idempotency_response_too_large",
            $"Response body exceeded {options.MaxResponseSize} bytes; " +
            "idempotent replay is not available for this response.");
        return;

    buffer.Position = 0
    var bytes = buffer.ToArray()
    await buffer.CopyToAsync(originalBody)

    if ShouldCache(response.StatusCode):
        store.Store(key, new IdempotencyRecord(
            key, Compute(request), response.StatusCode,
            response.ContentType ?? "application/octet-stream",
            bytes, DateTimeOffset.UtcNow), options.Ttl)
    else:
        store.Release(key)
finally:
    context.Response.Body = originalBody
```

`ShouldCache(status)`:
- `>= 200 && < 300` → cache
- `>= 300 && < 400` → cache
- `>= 400 && < 500 && status != 429` → cache
- `429` → do not cache
- `>= 500` → do not cache

`Replay(context, record, options)`:

```
context.Response.StatusCode = record.StatusCode
context.Response.ContentType = record.ContentType
context.Response.Headers[options.ReplayedHeaderName] = "true"
await context.Response.Body.WriteAsync(record.Body)
```

## 6. Error response shape

All middleware-emitted errors follow the existing `UnifiedResult<T>` format
already wired into the framework (same envelope as exception-handler
responses). Status code is set on `HttpResponse`, body is written via the
shared `UnifiedResult` helper.

| Trigger | Status | `code` | Extra headers |
|---|---|---|---|
| Key missing | pass-through | — | — |
| Key empty / too long | 400 | `invalid_idempotency_key` | — |
| Safe method with key | pass-through | — | — |
| In-flight conflict | 409 | `idempotency_in_progress` | `Retry-After: 1` |
| Reused with different payload | 422 | `idempotency_key_conflict` | — |
| Response > 1 MiB | 500 | `idempotency_response_too_large` | — |
| Anything else | pass-through | — | — |

## 7. AOT considerations

- `UnsafeMethods` is a compile-time `HashSet<HttpMethod>` of literals.
- No reflection; `IIdempotencyStore` is registered as a concrete type
  resolved at DI time.
- `IncrementalHash.CreateHash(HashAlgorithmName.SHA256)` is AOT-safe in
  .NET 10 (no `Create()` factory calls).
- The `DateTimeOffset.UtcNow` call and `MemoryStream` are AOT-safe.
- Verified by `Sharkable.AotSample` and `Sharkable.NativeTest` builds.

## 8. Limitations (documented in XML doc + README)

- **Single-instance only.** `IMemoryCache` is per-process. Multi-instance
  deployments must use a distributed backend (not in v1).
- **No streaming responses.** Responses > 1 MiB are not cached; the original
  caller receives a 500 `idempotency_response_too_large` instead, and the
  in-flight slot is released so a subsequent retry will re-execute.
- **No idempotency for 5xx.** The client should retry with the same key
  and the middleware will re-execute.
- **No 429 caching.** Clients should honor `Retry-After`.

## 9. Testing

### 9.1 Unit tests (`Sharkable.Tests`)

| Test class | Cases |
|---|---|
| `IdempotencyFingerprintTests` | same inputs → same hash; method/path/body differences → different hashes. |
| `SharkIdempotencyKeyValidatorTests` | empty, whitespace, 256 chars, normal UUID — all correct. |
| `MemoryIdempotencyStoreTests` | TryReserve atomicity, Get returns correct lookup, Store replaces placeholder, Release removes key, TTL expires. |

### 9.2 Integration tests (`WebApplicationFactory<>`)

A test endpoint `TestSharkIdempotencyEndpoint` is added to the test
application; the integration tests use `HttpClient` against it.

| # | Scenario | Expected |
|---|---|---|
| 1 | No header | Business logic runs once; 200. |
| 2 | Same key, same body, twice | Business logic runs once; second response has `X-Idempotent-Replayed: true`. |
| 3 | Same key, different body | First 200; second 422. |
| 4 | 5 concurrent same-key requests | 1 × 200; 4 × 409 with `Retry-After: 1`. |
| 5 | First 500, same-key retry | Business logic runs twice (5xx not cached). |
| 6 | First 400, same-key retry | Business logic runs once; second response replays 400. |
| 7 | GET with header | Business runs; header ignored. |
| 8 | Key length 256 | 400. |
| 9 | Response > 1 MiB | 500 `idempotency_response_too_large`; in-flight released. |
| 10 | `EnableIdempotency = false` | Header ignored; business runs every time. |

### 9.3 AOT verification

`dotnet build src/Sharkable.AotSample` and `dotnet build src/Sharkable.NativeTest`
must succeed without trimming warnings related to the new middleware.

## 10. Out of scope (future work)

- `IIdempotencyStore` Redis implementation.
- Per-endpoint opt-in attribute (`[SharkIdempotent]`).
- Configurable fingerprint algorithm (e.g., include `Content-Type`).
- Metrics for hit / miss / conflict / in-flight counts.
- `Idempotency-Replayed` response body field (per IETF draft revision).

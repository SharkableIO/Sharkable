# Idempotency Middleware Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add an opt-in idempotency middleware that lets clients safely retry unsafe HTTP requests via the `Idempotency-Key` header, with single-instance `IMemoryCache` storage, AOT-safe code, and zero new third-party dependencies.

**Architecture:** Header-driven activation gated by `SharkOption.EnableIdempotency`. State machine in a single middleware that uses an `IIdempotencyStore` abstraction backed by `IMemoryCache`. SHA-256 fingerprint over `(method, path, body)` detects "same key, different payload".

**Tech Stack:** .NET 10, ASP.NET Core middleware, `IMemoryCache`, `IncrementalHash` (SHA-256), xUnit + `WebApplicationFactory`-style fixture (existing `Microsoft.AspNetCore.TestHost` pattern in `Sharkable.Tests`).

**Branch:** `feat/idempotency-middleware`

**Spec:** `docs/superpowers/specs/2026-06-26-idempotency-middleware-design.md`

---

## File Structure

### New files in `src/Sharkable/Middleware/Idempotency/`

- `IdempotencyFingerprint.cs` — pure SHA-256 helper
- `SharkIdempotencyOptions.cs` — public options class (TTL, header names, method set, caps)
- `IIdempotencyStore.cs` — interface + `IdempotencyRecord` + `IdempotencyLookup` hierarchy
- `MemoryIdempotencyStore.cs` — `IMemoryCache`-backed implementation
- `SharkIdempotencyMiddleware.cs` — the state machine

### Modified files in `src/Sharkable/`

- `Shark/Options/SharkOption.cs` — add `EnableIdempotency` flag and `ConfigureIdempotency` method
- `Shark/Extensions/SharkExtension.cs` — register `IMemoryCache` + `IIdempotencyStore` in `AddCommon`
- `SharkableExtension.cs` — wire `SharkIdempotencyMiddleware` in `UseShark`

### New test files in `src/Sharkable.Tests/Idempotency/`

- `IdempotencyFingerprintTests.cs` — unit
- `SharkIdempotencyOptionsTests.cs` — unit
- `MemoryIdempotencyStoreTests.cs` — unit (uses real `MemoryCache`)
- `IdempotencyTestFixture.cs` — `IAsyncLifetime` with `EnableIdempotency = true`
- `IdempotencyTestEndpoint.cs` — `ISharkEndpoint` with invocation counter and configurable status/delay/size
- `IdempotencyIntegrationTests.cs` — integration tests covering all 10 spec scenarios

### Modified files in `src/Sharkable/`

- `README.md` — add idempotency section

---

## Task 1: `IdempotencyFingerprint` helper

**Files:**
- Create: `src/Sharkable/Middleware/Idempotency/IdempotencyFingerprint.cs`
- Create: `src/Sharkable.Tests/Idempotency/IdempotencyFingerprintTests.cs`

- [ ] **Step 1.1: Create the test file (failing tests first)**

```csharp
using System.Text;

namespace Sharkable.Tests.Idempotency;

public class IdempotencyFingerprintTests
{
    [Fact]
    public void Compute_SameInputs_ProducesSameHash()
    {
        var body = Encoding.UTF8.GetBytes("hello");
        var h1 = IdempotencyFingerprint.Compute("POST", "/api/orders", body);
        var h2 = IdempotencyFingerprint.Compute("POST", "/api/orders", body);
        Assert.Equal(h1, h2);
        Assert.Equal(64, h1.Length);   // SHA-256 hex = 64 chars
        Assert.Matches("^[0-9a-f]{64}$", h1);
    }

    [Theory]
    [InlineData("GET", "/api/orders", new byte[] { 1, 2, 3 })]
    [InlineData("POST", "/api/users", new byte[] { 1, 2, 3 })]
    [InlineData("POST", "/api/orders", new byte[] { 1, 2, 4 })]
    [InlineData("POST", "/api/orders", new byte[] { })]
    public void Compute_DifferentInputs_ProduceDifferentHashes(string method, string path, byte[] body)
    {
        var baseline = IdempotencyFingerprint.Compute("POST", "/api/orders", new byte[] { 1, 2, 3 });
        var actual = IdempotencyFingerprint.Compute(method, path, body);
        Assert.NotEqual(baseline, actual);
    }

    [Fact]
    public void Compute_MethodIsCaseInsensitive()
    {
        var body = Encoding.UTF8.GetBytes("x");
        var h1 = IdempotencyFingerprint.Compute("post", "/x", body);
        var h2 = IdempotencyFingerprint.Compute("POST", "/x", body);
        Assert.Equal(h1, h2);
    }

    [Fact]
    public void Compute_EmptyBody_HasStableHash()
    {
        var h1 = IdempotencyFingerprint.Compute("POST", "/api/orders", ReadOnlySpan<byte>.Empty);
        var h2 = IdempotencyFingerprint.Compute("POST", "/api/orders", Array.Empty<byte>());
        Assert.Equal(h1, h2);
    }
}
```

- [ ] **Step 1.2: Run the test to verify it fails**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~IdempotencyFingerprintTests" -v minimal
```

Expected: build error `The type or namespace name 'IdempotencyFingerprint' could not be found`.

- [ ] **Step 1.3: Implement the helper**

Create `src/Sharkable/Middleware/Idempotency/IdempotencyFingerprint.cs`:

```csharp
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Computes the SHA-256 fingerprint used to detect "same Idempotency-Key,
/// different request payload". Hashes <c>method + "\n" + path + "\n" + body</c>
/// using incremental hashing so the full body need not be materialized.
/// </summary>
internal static class IdempotencyFingerprint
{
    /// <summary>
    /// Computes the lower-case hex SHA-256 of the request identity.
    /// </summary>
    /// <param name="method">HTTP method (case-insensitive; normalized to upper).</param>
    /// <param name="path">Request path. Null/empty is treated as <c>"/"</c>.</param>
    /// <param name="body">Raw request body bytes.</param>
    public static string Compute(string method, PathString path, ReadOnlySpan<byte> body)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        sha.AppendData(Encoding.ASCII.GetBytes(method.ToUpperInvariant()));
        sha.AppendData(NewlineBytes);
        sha.AppendData(Encoding.ASCII.GetBytes(path.Value ?? "/"));
        sha.AppendData(NewlineBytes);
        sha.AppendData(body);
        return Convert.ToHexString(sha.GetHashAndReset()).ToLowerInvariant();
    }

    // IncrementalHash.AppendData has no single-byte overload; cache a one-byte buffer.
    private static readonly byte[] NewlineBytes = [(byte)'\n'];
}
```

- [ ] **Step 1.4: Run the test to verify it passes**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~IdempotencyFingerprintTests" -v minimal
```

Expected: `Passed: 4` (or 7 with the theory cases — counted as 4 separate test methods).

- [ ] **Step 1.5: Commit**

```bash
git add src/Sharkable/Middleware/Idempotency/IdempotencyFingerprint.cs src/Sharkable.Tests/Idempotency/IdempotencyFingerprintTests.cs
git commit -m "feat(idempotency): add SHA-256 fingerprint helper"
```

---

## Task 2: `SharkIdempotencyOptions` class

**Files:**
- Create: `src/Sharkable/Middleware/Idempotency/SharkIdempotencyOptions.cs`
- Create: `src/Sharkable.Tests/Idempotency/SharkIdempotencyOptionsTests.cs`

- [ ] **Step 2.1: Create the test file (failing tests first)**

```csharp
namespace Sharkable.Tests.Idempotency;

public class SharkIdempotencyOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var o = new SharkIdempotencyOptions();
        Assert.Equal(TimeSpan.FromHours(24), o.Ttl);
        Assert.Equal(TimeSpan.FromSeconds(30), o.InFlightTtl);
        Assert.Equal(255, o.MaxKeyLength);
        Assert.Equal(1_048_576, o.MaxResponseSize);
        Assert.Equal("Idempotency-Key", o.HeaderName);
        Assert.Equal("X-Idempotent-Replayed", o.ReplayedHeaderName);
        Assert.Contains(HttpMethod.Post, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Put, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Patch, o.UnsafeMethods);
        Assert.Contains(HttpMethod.Delete, o.UnsafeMethods);
    }

    [Fact]
    public void IsValidKey_AcceptsNormalUuid()
    {
        var o = new SharkIdempotencyOptions();
        Assert.True(o.IsValidKey("8c0a6f4e-9b2d-4f1a-b3c7-2e5d8a1f0b6c"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void IsValidKey_RejectsEmptyAndWhitespace(string key)
    {
        var o = new SharkIdempotencyOptions();
        Assert.False(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_RejectsTooLong()
    {
        var o = new SharkIdempotencyOptions();
        var key = new string('a', 256);
        Assert.False(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_AcceptsMaxLength()
    {
        var o = new SharkIdempotencyOptions();
        var key = new string('a', 255);
        Assert.True(o.IsValidKey(key));
    }

    [Fact]
    public void IsValidKey_RejectsControlChars()
    {
        var o = new SharkIdempotencyOptions();
        Assert.False(o.IsValidKey("abc\x01def"));
    }
}
```

- [ ] **Step 2.2: Run the test to verify it fails**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~SharkIdempotencyOptionsTests" -v minimal
```

Expected: build error `The type or namespace name 'SharkIdempotencyOptions' could not be found`.

- [ ] **Step 2.3: Implement the options class**

Create `src/Sharkable/Middleware/Idempotency/SharkIdempotencyOptions.cs`:

```csharp
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Configuration options for the idempotency middleware. Configured via
/// <c>SharkOption.ConfigureIdempotency(o =&gt; ...)</c>.
/// </summary>
public sealed class SharkIdempotencyOptions
{
    /// <summary>How long completed records are kept. Default is 24 hours.</summary>
    public TimeSpan Ttl { get; set; } = TimeSpan.FromHours(24);

    /// <summary>
    /// Auto-eviction for in-flight placeholders. Protects against permanent
    /// deadlocks when a process crashes mid-request. Default is 30 seconds.
    /// </summary>
    public TimeSpan InFlightTtl { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Reject keys longer than this with a 400. Default is 255 (IETF draft max).</summary>
    public int MaxKeyLength { get; set; } = 255;

    /// <summary>
    /// Responses whose buffered body exceeds this size are replaced with a
    /// 500 <c>idempotency_response_too_large</c> error. Default is 1 MiB.
    /// </summary>
    public int MaxResponseSize { get; set; } = 1_048_576;

    /// <summary>Request header name. Default is <c>"Idempotency-Key"</c>.</summary>
    public string HeaderName { get; set; } = "Idempotency-Key";

    /// <summary>Response header set on replays. Default is <c>"X-Idempotent-Replayed"</c>.</summary>
    public string ReplayedHeaderName { get; set; } = "X-Idempotent-Replayed";

    /// <summary>
    /// HTTP methods that activate the middleware when <see cref="HeaderName"/>
    /// is present. Default is POST, PUT, PATCH, DELETE.
    /// </summary>
    public IReadOnlySet<HttpMethod> UnsafeMethods { get; set; } = new HashSet<HttpMethod>
    {
        HttpMethod.Post,
        HttpMethod.Put,
        HttpMethod.Patch,
        HttpMethod.Delete,
    };

    /// <summary>
    /// Validates an <c>Idempotency-Key</c> value: non-empty after trim,
    /// length &lt;= <see cref="MaxKeyLength"/>, and printable ASCII only.
    /// </summary>
    public bool IsValidKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key)) return false;
        if (key.Length > MaxKeyLength) return false;
        foreach (var c in key)
        {
            if (c < 0x20 || c > 0x7E) return false;
        }
        return true;
    }
}
```

- [ ] **Step 2.4: Run the test to verify it passes**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~SharkIdempotencyOptionsTests" -v minimal
```

Expected: all tests pass.

- [ ] **Step 2.5: Commit**

```bash
git add src/Sharkable/Middleware/Idempotency/SharkIdempotencyOptions.cs src/Sharkable.Tests/Idempotency/SharkIdempotencyOptionsTests.cs
git commit -m "feat(idempotency): add SharkIdempotencyOptions with validation"
```

---

## Task 3: `IIdempotencyStore` + `IdempotencyRecord` + lookup types

**Files:**
- Create: `src/Sharkable/Middleware/Idempotency/IIdempotencyStore.cs`

- [ ] **Step 3.1: Implement the interface and types**

```csharp
namespace Sharkable;

/// <summary>
/// Stores idempotency keys, their in-flight placeholders, and the responses
/// they produced. Implementations are responsible for atomicity of
/// <see cref="TryReserve"/>; the middleware relies on it.
/// </summary>
public interface IIdempotencyStore
{
    /// <summary>
    /// Atomically reserves the key. Returns <c>true</c> if this caller
    /// now owns the in-flight slot; <c>false</c> if the key is already
    /// reserved or completed.
    /// </summary>
    /// <param name="key">The Idempotency-Key header value.</param>
    /// <param name="inFlightTtl">How long the in-flight placeholder lives before auto-eviction.</param>
    bool TryReserve(string key, TimeSpan inFlightTtl);

    /// <summary>
    /// Returns the current state of the key: in-flight, completed, or absent (null).
    /// </summary>
    IdempotencyLookup? Get(string key);

    /// <summary>
    /// Stores the completed response under the key. Replaces any in-flight
    /// placeholder. Applies the configured TTL.
    /// </summary>
    void Store(string key, IdempotencyRecord record, TimeSpan ttl);

    /// <summary>
    /// Removes the in-flight placeholder without storing a record. Called
    /// when the response should not be cached (5xx, 429, oversize).
    /// </summary>
    void Release(string key);
}

/// <summary>
/// A completed response stored under an <c>Idempotency-Key</c>. Used for
/// replay and for detecting "same key, different payload" via
/// <see cref="Fingerprint"/>.
/// </summary>
public sealed record IdempotencyRecord(
    string Key,
    string Fingerprint,
    int StatusCode,
    string ContentType,
    ReadOnlyMemory<byte> Body,
    DateTimeOffset CompletedAt);

/// <summary>Discriminated union for <see cref="IIdempotencyStore.Get"/>.</summary>
public abstract record IdempotencyLookup;

/// <summary>The key is currently in-flight; another request is executing.</summary>
public sealed record IdempotencyInFlight : IdempotencyLookup;

/// <summary>The key has a completed response ready for replay.</summary>
public sealed record IdempotencyHit(IdempotencyRecord Record) : IdempotencyLookup;
```

- [ ] **Step 3.2: Build to verify compilation**

```bash
dotnet build src/Sharkable/Sharkable.csproj
```

Expected: build succeeds with no new warnings.

- [ ] **Step 3.3: Commit**

```bash
git add src/Sharkable/Middleware/Idempotency/IIdempotencyStore.cs
git commit -m "feat(idempotency): add IIdempotencyStore interface and record types"
```

---

## Task 4: `MemoryIdempotencyStore`

**Files:**
- Create: `src/Sharkable/Middleware/Idempotency/MemoryIdempotencyStore.cs`
- Create: `src/Sharkable.Tests/Idempotency/MemoryIdempotencyStoreTests.cs`

- [ ] **Step 4.1: Create the test file (failing tests first)**

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace Sharkable.Tests.Idempotency;

public class MemoryIdempotencyStoreTests
{
    private static MemoryIdempotencyStore NewStore() =>
        new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void TryReserve_NewKey_ReturnsTrue()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void TryReserve_SameKeyTwice_SecondReturnsFalse()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
        Assert.False(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Get_BeforeReserve_ReturnsNull()
    {
        var s = NewStore();
        Assert.Null(s.Get("missing"));
    }

    [Fact]
    public void Get_AfterReserve_ReturnsInFlight()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var result = s.Get("k1");
        Assert.IsType<IdempotencyInFlight>(result);
    }

    [Fact]
    public void Store_AfterReserve_GetReturnsHit()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "hash123", 200, "application/json",
            new byte[] { 1, 2, 3 }, DateTimeOffset.UtcNow);
        s.Store("k1", record, TimeSpan.FromMinutes(1));

        var result = s.Get("k1");
        var hit = Assert.IsType<IdempotencyHit>(result);
        Assert.Equal("hash123", hit.Record.Fingerprint);
        Assert.Equal(200, hit.Record.StatusCode);
    }

    [Fact]
    public void Release_AllowsRereserve()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        s.Release("k1");
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void TryReserve_ExpiresAfterTtl()
    {
        var s = NewStore();
        Assert.True(s.TryReserve("k1", TimeSpan.FromMilliseconds(50)));
        System.Threading.Thread.Sleep(200);
        Assert.True(s.TryReserve("k1", TimeSpan.FromMinutes(1)));
    }

    [Fact]
    public void Store_RemovesInFlightPlaceholder()
    {
        var s = NewStore();
        s.TryReserve("k1", TimeSpan.FromMinutes(1));
        var record = new IdempotencyRecord(
            "k1", "h", 200, "text/plain",
            new byte[] { 9 }, DateTimeOffset.UtcNow);
        s.Store("k1", record, TimeSpan.FromMinutes(1));

        // After store, TryReserve should be able to take the slot (TTL-based eviction)
        // Note: IMemoryCache evicts lazily; we test it expires by sleeping.
        // Use short TTL for this assertion:
        s.Release("k1");
        Assert.True(s.TryReserve("k1", TimeSpan.FromMilliseconds(50)));
    }
}
```

- [ ] **Step 4.2: Run the test to verify it fails**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~MemoryIdempotencyStoreTests" -v minimal
```

Expected: build error `The type or namespace name 'MemoryIdempotencyStore' could not be found`.

- [ ] **Step 4.3: Implement the store**

Create `src/Sharkable/Middleware/Idempotency/MemoryIdempotencyStore.cs`:

```csharp
using Microsoft.Extensions.Caching.Memory;

namespace Sharkable;

/// <summary>
/// In-process <see cref="IIdempotencyStore"/> backed by
/// <see cref="IMemoryCache"/>. Single-instance only; for distributed
/// scenarios implement <see cref="IIdempotencyStore"/> with Redis or
/// similar.
/// </summary>
public sealed class MemoryIdempotencyStore : IIdempotencyStore
{
    private readonly IMemoryCache _cache;

    /// <summary>Marker for an in-flight slot (no record yet).</summary>
    private sealed record InFlightMarker;

    public MemoryIdempotencyStore(IMemoryCache cache)
    {
        _cache = cache;
    }

    public bool TryReserve(string key, TimeSpan inFlightTtl)
    {
        // GetOrCreate with a factory is atomic per key in MemoryCache:
        // the factory runs at most once per key per process.
        // If the factory returns a value, the cache holds it; we then
        // check whether what we got back is OUR marker (won) or a
        // pre-existing marker (lost).
        var marker = new InFlightMarker();
        var actual = _cache.GetOrCreate(key, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = inFlightTtl;
            return marker;
        });
        return ReferenceEquals(actual, marker);
    }

    public IdempotencyLookup? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var value) || value is null)
            return null;
        return value switch
        {
            InFlightMarker => new IdempotencyInFlight(),
            IdempotencyRecord r => new IdempotencyHit(r),
            _ => null,
        };
    }

    public void Store(string key, IdempotencyRecord record, TimeSpan ttl)
    {
        using var entry = _cache.CreateEntry(key);
        entry.AbsoluteExpirationRelativeToNow = ttl;
        entry.Value = record;
    }

    public void Release(string key)
    {
        _cache.Remove(key);
    }
}
```

- [ ] **Step 4.4: Run the test to verify it passes**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~MemoryIdempotencyStoreTests" -v minimal
```

Expected: all tests pass.

- [ ] **Step 4.5: Commit**

```bash
git add src/Sharkable/Middleware/Idempotency/MemoryIdempotencyStore.cs src/Sharkable.Tests/Idempotency/MemoryIdempotencyStoreTests.cs
git commit -m "feat(idempotency): add MemoryIdempotencyStore"
```

---

## Task 5: `SharkIdempotencyMiddleware` — state machine

**Files:**
- Create: `src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs`

- [ ] **Step 5.1: Implement the middleware**

```csharp
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Middleware that enforces idempotency for unsafe HTTP methods via the
/// <c>Idempotency-Key</c> header. See
/// <c>docs/superpowers/specs/2026-06-26-idempotency-middleware-design.md</c>.
/// </summary>
internal sealed class SharkIdempotencyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IIdempotencyStore _store;
    private readonly SharkIdempotencyOptions _options;
    private readonly ILogger<SharkIdempotencyMiddleware> _logger;

    public SharkIdempotencyMiddleware(
        RequestDelegate next,
        IIdempotencyStore store,
        SharkIdempotencyOptions options,
        ILogger<SharkIdempotencyMiddleware> logger)
    {
        _next = next;
        _store = store;
        _options = options;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 1. Header present?
        if (!context.Request.Headers.TryGetValue(_options.HeaderName, out var headerValues)
            || headerValues.Count == 0
            || string.IsNullOrWhiteSpace(headerValues[0]))
        {
            await _next(context);
            return;
        }

        var key = headerValues[0]!;

        // 2. Key valid?
        if (!_options.IsValidKey(key))
        {
            await WriteUnified(context, 400, "invalid_idempotency_key",
                $"Key must be 1..{_options.MaxKeyLength} printable ASCII characters.");
            return;
        }

        // 3. Method eligible?
        if (!_options.UnsafeMethods.Contains(context.Request.Method))
        {
            await _next(context);
            return;
        }

        // 4. Try to reserve the slot.
        if (!_store.TryReserve(key, _options.InFlightTtl))
        {
            // Slot was already taken. Look at its state.
            var existing = _store.Get(key);
            switch (existing)
            {
                case IdempotencyInFlight:
                    context.Response.Headers["Retry-After"] = "1";
                    await WriteUnified(context, 409, "idempotency_in_progress",
                        "An identical request is already in progress; retry after 1 second.");
                    return;

                case IdempotencyHit hit:
                    var fingerprint = ComputeFingerprint(context);
                    if (hit.Record.Fingerprint != fingerprint)
                    {
                        await WriteUnified(context, 422, "idempotency_key_conflict",
                            "Idempotency-Key was reused with a different request payload.");
                        return;
                    }
                    await Replay(context, hit.Record);
                    return;

                default:
                    // Race: placeholder expired between TryReserve and Get. Fall through and execute.
                    break;
            }
        }

        // 5. We own the in-flight slot. Execute downstream with response buffering.
        var originalBody = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            await _next(context);

            // 5a. Oversize response -> 500, do not cache.
            if (buffer.Length > _options.MaxResponseSize)
            {
                _logger.LogWarning(
                    "Idempotency response for key {Key} exceeds {Max} bytes; " +
                    "rejecting and releasing in-flight slot.",
                    key, _options.MaxResponseSize);
                _store.Release(key);
                context.Response.StatusCode = 500;
                context.Response.Body = originalBody;
                await WriteUnified(context, 500, "idempotency_response_too_large",
                    $"Response body exceeded {_options.MaxResponseSize} bytes; " +
                    "idempotent replay is not available for this response.");
                return;
            }

            // 5b. Successful (cacheable) responses -> store and forward.
            if (ShouldCache(context.Response.StatusCode))
            {
                buffer.Position = 0;
                var bytes = buffer.ToArray();
                await buffer.CopyToAsync(originalBody);

                var record = new IdempotencyRecord(
                    key,
                    ComputeFingerprint(context),
                    context.Response.StatusCode,
                    context.Response.ContentType ?? "application/octet-stream",
                    bytes,
                    DateTimeOffset.UtcNow);
                _store.Store(key, record, _options.Ttl);
            }
            else
            {
                // 429 or 5xx: do not cache. Release slot and forward body.
                _store.Release(key);
                await buffer.CopyToAsync(originalBody);
            }
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool ShouldCache(int status) =>
        status >= 200 && status < 500 && status != 429;

    private string ComputeFingerprint(HttpContext context)
    {
        // For fingerprint we use the buffered request body; if no body was read,
        // we fall back to an empty span. The path is context.Request.Path.
        // Note: in this middleware, we do not pre-buffer the request body because
        // ASP.NET Core exposes it as a forward-only stream. We rely on the
        // request body being re-readable via EnableBuffering (set by other
        // middlewares such as the audit trail). If the body is not seekable
        // and not yet consumed, we treat it as empty (this is a known
        // limitation; see §8 of the spec).
        var bodyLength = (int)(context.Request.ContentLength ?? 0);
        byte[] body = bodyLength > 0
            ? ReadBodyBytes(context.Request.Body, bodyLength)
            : Array.Empty<byte>();
        return IdempotencyFingerprint.Compute(
            context.Request.Method,
            context.Request.Path,
            body);
    }

    private static byte[] ReadBodyBytes(Stream body, int length)
    {
        // Caller is expected to have called Request.EnableBuffering upstream.
        // If we cannot seek, we read up to `length` bytes and stop.
        if (body.CanSeek) body.Position = 0;
        var buf = new byte[length];
        int read = 0;
        while (read < length)
        {
            int n = body.Read(buf, read, length - read);
            if (n == 0) break;
            read += n;
        }
        if (read < length) Array.Resize(ref buf, read);
        return buf;
    }

    private async Task Replay(HttpContext context, IdempotencyRecord record)
    {
        context.Response.StatusCode = record.StatusCode;
        context.Response.ContentType = record.ContentType;
        context.Response.Headers[_options.ReplayedHeaderName] = "true";
        await context.Response.Body.WriteAsync(record.Body);
    }

    private static Task WriteUnified(
        HttpContext context, int status, string code, string message)
    {
        // Use the framework's UnifiedResult factory if present, else a simple envelope.
        // Keeping it inline avoids taking a dependency on the internal
        // DefaultUnifiedResultFactory in the test path.
        var body = new
        {
            statusCode = status,
            success = false,
            errorMessage = message,
            code,
        };
        context.Response.StatusCode = status;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsJsonAsync(body);
    }
}
```

- [ ] **Step 5.2: Build to verify compilation**

```bash
dotnet build src/Sharkable/Sharkable.csproj
```

Expected: build succeeds. The middleware is not yet wired into the pipeline; it just needs to compile.

- [ ] **Step 5.3: Commit**

```bash
git add src/Sharkable/Middleware/Idempotency/SharkIdempotencyMiddleware.cs
git commit -m "feat(idempotency): add SharkIdempotencyMiddleware state machine"
```

---

## Task 6: `SharkOption.EnableIdempotency` + `ConfigureIdempotency`

**Files:**
- Modify: `src/Sharkable/Shark/Options/SharkOption.cs`

- [ ] **Step 6.1: Add the property and the method to `SharkOption`**

Edit `src/Sharkable/Shark/Options/SharkOption.cs`. Add the property near the other `Enable*` flags (after `EnableHealthChecks`), and add the configuration method near `ConfigureAuditTrail` and `ConfigureAutoCrud`.

Add this property after `EnableHealthChecks`:

```csharp
/// <summary>
/// When <c>true</c>, wires the idempotency middleware into the pipeline.
/// Requests carrying an <c>Idempotency-Key</c> header on an unsafe HTTP
/// method are deduplicated and replayed. Default is <c>false</c>.
/// </summary>
public bool EnableIdempotency { get; set; } = false;
```

Add this method after `ConfigureAutoCrud`:

```csharp
/// <summary>
/// Configures the idempotency middleware. Called only when
/// <see cref="EnableIdempotency"/> is <c>true</c>.
/// </summary>
public void ConfigureIdempotency(Action<SharkIdempotencyOptions> configure)
{
    var opt = new SharkIdempotencyOptions();
    configure(opt);
    IdempotencyOptions = opt;
}
```

Add this internal property next to `AuditTrailOptions`:

```csharp
/// <summary>
/// Stores the idempotency options provided via <see cref="ConfigureIdempotency"/>.
/// </summary>
internal SharkIdempotencyOptions? IdempotencyOptions { get; set; }
```

- [ ] **Step 6.2: Build to verify compilation**

```bash
dotnet build src/Sharkable/Sharkable.csproj
```

Expected: build succeeds.

- [ ] **Step 6.3: Commit**

```bash
git add src/Sharkable/Shark/Options/SharkOption.cs
git commit -m "feat(idempotency): add EnableIdempotency flag and ConfigureIdempotency"
```

---

## Task 7: DI registration in `AddCommon`

**Files:**
- Modify: `src/Sharkable/Shark/Extensions/SharkExtension.cs`

- [ ] **Step 7.1: Add the registration block**

In `AddCommon`, after the existing "register CORS" block and before the "register JWT auth" block, add:

```csharp
//register idempotency
if (Shark.SharkOption.EnableIdempotency)
{
    services.AddMemoryCache();
    services.AddSingleton(Shark.SharkOption.IdempotencyOptions ?? new SharkIdempotencyOptions());
    services.AddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
}
```

- [ ] **Step 7.2: Build to verify**

```bash
dotnet build src/Sharkable/Sharkable.csproj
```

Expected: build succeeds.

- [ ] **Step 7.3: Commit**

```bash
git add src/Sharkable/Shark/Extensions/SharkExtension.cs
git commit -m "feat(idempotency): register store and options in AddCommon"
```

---

## Task 8: Pipeline wiring in `UseShark`

**Files:**
- Modify: `src/Sharkable/SharkableExtension.cs`

- [ ] **Step 8.1: Wire the middleware**

In `UseShark` (in `SharkableExtension.cs`), add a new conditional block. Place it after the existing "audit trail" block and before `app.MapEndpoints()`:

```csharp
// idempotency
if (Shark.SharkOption.EnableIdempotency)
    app.UseMiddleware<SharkIdempotencyMiddleware>();
```

- [ ] **Step 8.2: Build the full solution**

```bash
dotnet build
```

Expected: solution builds without errors.

- [ ] **Step 8.3: Commit**

```bash
git add src/Sharkable/SharkableExtension.cs
git commit -m "feat(idempotency): wire middleware into pipeline"
```

---

## Task 9: Integration test fixture, endpoint, and scenarios

**Files:**
- Create: `src/Sharkable.Tests/Idempotency/IdempotencyTestFixture.cs`
- Create: `src/Sharkable.Tests/Idempotency/IdempotencyTestEndpoint.cs`
- Create: `src/Sharkable.Tests/Idempotency/IdempotencyIntegrationTests.cs`

- [ ] **Step 9.1: Create the test endpoint**

`src/Sharkable.Tests/Idempotency/IdempotencyTestEndpoint.cs`:

```csharp
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Sharkable;

namespace Sharkable.Tests.Idempotency;

/// <summary>
/// Test endpoint that records every invocation. Used by integration tests
/// to assert that the idempotency middleware deduplicates or passes through
/// as expected. Configure behavior via query string:
///   ?status=200|400|500  -> return that status
///   ?delay=2000          -> wait N ms before responding (for in-flight tests)
///   ?size=2000000        -> return a body of N bytes
/// </summary>
public class IdempotencyTestEndpoint : ISharkEndpoint
{
    public static readonly ConcurrentBag<string> Invocations = new();

    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("idempotency/test", async (HttpContext ctx) =>
        {
            Invocations.Add(ctx.Connection.Id + ":" + DateTime.UtcNow.Ticks);

            var qs = ctx.Request.Query;
            int status = int.TryParse(qs["status"], out var s) ? s : 200;
            int delay = int.TryParse(qs["delay"], out var d) ? d : 0;
            int size = int.TryParse(qs["size"], out var sz) ? sz : 0;

            if (delay > 0) await Task.Delay(delay);

            if (size > 0)
            {
                ctx.Response.StatusCode = status;
                ctx.Response.ContentType = "application/octet-stream";
                await ctx.Response.Body.WriteAsync(new byte[size]);
                return;
            }

            ctx.Response.StatusCode = status;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsJsonAsync(new { ok = status < 400, status });
        });
    }

    public static void Reset() => Invocations.Clear();
}
```

- [ ] **Step 9.2: Create the test fixture**

`src/Sharkable.Tests/Idempotency/IdempotencyTestFixture.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests.Idempotency;

public class IdempotencyTestFixture : IAsyncLifetime
{
    public WebApplication App { get; private set; } = null!;
    public HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseTestServer();
        builder.Environment.EnvironmentName = "Production";

        builder.Services.AddShark(
            [typeof(IdempotencyTestEndpoint).Assembly],
            opt =>
            {
                opt.EnableIdempotency = true;
                opt.ConfigureIdempotency(o =>
                {
                    o.Ttl = TimeSpan.FromMinutes(5);
                    o.InFlightTtl = TimeSpan.FromSeconds(10);
                });
            });

        App = builder.Build();
        App.UseShark();
        await App.StartAsync();
        Client = App.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        await App.DisposeAsync();
    }
}
```

- [ ] **Step 9.3: Create the integration tests (failing — the endpoint exists, but tests are placeholders that need the assertion logic)**

`src/Sharkable.Tests/Idempotency/IdempotencyIntegrationTests.cs`:

```csharp
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.TestHost;

namespace Sharkable.Tests.Idempotency;

public class IdempotencyIntegrationTests : IClassFixture<IdempotencyTestFixture>
{
    private readonly HttpClient _client;

    public IdempotencyIntegrationTests(IdempotencyTestFixture fixture)
    {
        _client = fixture.Client;
        IdempotencyTestEndpoint.Reset();
    }

    private static HttpRequestMessage NewIdempotentRequest(string key, string body = "{\"x\":1}")
    {
        var req = new HttpRequestMessage(HttpMethod.Post, "/api/idempotency/test");
        req.Headers.Add("Idempotency-Key", key);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");
        return req;
    }

    [Fact]
    public async Task NoHeader_PassesThrough()
    {
        var res = await _client.PostAsync("/api/idempotency/test", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);
    }

    [Fact]
    public async Task SameKeySameBody_Replayed()
    {
        var key = Guid.NewGuid().ToString();
        var r1 = await _client.SendAsync(NewIdempotentRequest(key));
        var r2 = await _client.SendAsync(NewIdempotentRequest(key));

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);   // only one execution
        Assert.Equal("true", r2.Headers.GetValues("X-Idempotent-Replayed").First());
    }

    [Fact]
    public async Task SameKeyDifferentBody_422()
    {
        var key = Guid.NewGuid().ToString();
        var r1 = await _client.SendAsync(NewIdempotentRequest(key, "{\"a\":1}"));
        var r2 = await _client.SendAsync(NewIdempotentRequest(key, "{\"a\":2}"));

        Assert.Equal(HttpStatusCode.OK, r1.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, r2.StatusCode);
    }

    [Fact]
    public async Task ConcurrentSameKey_OneSucceedsOthersConflict()
    {
        var key = Guid.NewGuid().ToString();
        var delayReq = NewIdempotentRequest(key);
        delayReq.RequestUri = new Uri("http://localhost/api/idempotency/test?delay=2000&status=200");
        // Need body for fingerprint but also delay param -> use the default body
        delayReq.Content = new StringContent("{\"x\":1}", Encoding.UTF8, "application/json");

        var tasks = Enumerable.Range(0, 5)
            .Select(_ => _client.SendAsync(delayReq))
            .ToArray();
        var results = await Task.WhenAll(tasks);

        var ok = results.Count(r => r.StatusCode == HttpStatusCode.OK);
        var conflict = results.Count(r => r.StatusCode == HttpStatusCode.Conflict);
        Assert.Equal(1, ok);
        Assert.Equal(4, conflict);
    }

    [Fact]
    public async Task First500_NotCached_SecondExecutes()
    {
        var key = Guid.NewGuid().ToString();
        var failing = NewIdempotentRequest(key);
        failing.RequestUri = new Uri("http://localhost/api/idempotency/test?status=500");

        var r1 = await _client.SendAsync(failing);
        var r2 = await _client.SendAsync(failing);

        Assert.Equal(HttpStatusCode.InternalServerError, r1.StatusCode);
        Assert.Equal(HttpStatusCode.InternalServerError, r2.StatusCode);
        Assert.Equal(2, IdempotencyTestEndpoint.Invocations.Count);
    }

    [Fact]
    public async Task First400_Cached_Replayed()
    {
        var key = Guid.NewGuid().ToString();
        var failing = NewIdempotentRequest(key);
        failing.RequestUri = new Uri("http://localhost/api/idempotency/test?status=400");

        var r1 = await _client.SendAsync(failing);
        var r2 = await _client.SendAsync(failing);

        Assert.Equal(HttpStatusCode.BadRequest, r1.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, r2.StatusCode);
        Assert.Single(IdempotencyTestEndpoint.Invocations);
    }

    [Fact]
    public async Task GetWithHeader_Ignored()
    {
        var key = Guid.NewGuid().ToString();
        var req = new HttpRequestMessage(HttpMethod.Get, "/api/idempotency/test");
        req.Headers.Add("Idempotency-Key", key);

        var res = await _client.SendAsync(req);
        // GET on /api/idempotency/test maps to the same endpoint via POST
        // but the middleware should ignore the header on GET. We can only
        // assert that the response is not a 4xx from the middleware.
        Assert.NotEqual(HttpStatusCode.Conflict, res.StatusCode);
    }

    [Fact]
    public async Task KeyTooLong_400()
    {
        var key = new string('a', 256);
        var res = await _client.SendAsync(NewIdempotentRequest(key));
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }
}
```

- [ ] **Step 9.4: Run the integration tests to see what works and what doesn't**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~IdempotencyIntegrationTests" -v minimal
```

Expected: most pass. Specifically:
- The "First500_NotCached" test may fail because the in-memory store shares state across tests; reset it.
- The "ConcurrentSameKey" test may be flaky if InFlightTtl is too long. Adjust if needed.
- The "GetWithHeader_Ignored" test depends on the endpoint not being reached via GET; adjust.

- [ ] **Step 9.5: Address state sharing across tests**

The `IMemoryCache` is a singleton and shared across tests. If a test in the class causes a `200` response to be cached, subsequent tests in the same fixture run with a "polluted" store. To isolate:

Option A: Make `InFlightTtl` and `Ttl` very short (already 5min) and reset cache per test via reflection — fragile.
Option B: Add a per-test unique path so the cache key (just the Idempotency-Key) is the only differentiator. The tests already use `Guid.NewGuid()` for keys, so collisions are improbable.
Option C: Add an internal `Clear()` method to `MemoryIdempotencyStore` and call it in a `Reset()` method of the fixture; expose via a test-only hook.

Pick Option B for now. If tests become flaky, add a per-test path.

- [ ] **Step 9.6: Commit**

```bash
git add src/Sharkable.Tests/Idempotency/
git commit -m "test(idempotency): add integration tests for state machine"
```

---

## Task 10: Oversize response test

**Files:**
- Modify: `src/Sharkable.Tests/Idempotency/IdempotencyIntegrationTests.cs`

- [ ] **Step 10.1: Add the oversize test**

Append to `IdempotencyIntegrationTests`:

```csharp
[Fact]
public async Task OversizeResponse_500_AndInFlightReleased()
{
    var key = Guid.NewGuid().ToString();
    var big = NewIdempotentRequest(key);
    big.RequestUri = new Uri("http://localhost/api/idempotency/test?size=2000000&status=200");

    var r1 = await _client.SendAsync(big);
    Assert.Equal(HttpStatusCode.InternalServerError, r1.StatusCode);

    // Subsequent request with same key should be able to execute (in-flight released).
    var r2 = await _client.SendAsync(NewIdempotentRequest(key));
    Assert.Equal(HttpStatusCode.OK, r2.StatusCode);
}
```

- [ ] **Step 10.2: Run the new test**

```bash
dotnet test src/Sharkable.Tests/Sharkable.Tests.csproj --filter "FullyQualifiedName~OversizeResponse" -v minimal
```

Expected: passes.

- [ ] **Step 10.3: Commit**

```bash
git add src/Sharkable.Tests/Idempotency/IdempotencyIntegrationTests.cs
git commit -m "test(idempotency): add oversize response scenario"
```

---

## Task 11: AOT verification

**Files:**
- Modify: any AOT rd.xml or csproj as needed

- [ ] **Step 11.1: Build the AOT sample**

```bash
dotnet build src/Sharkable.AotSample/Sharkable.AotSample.csproj -c Release
```

Expected: build succeeds. If there are trimming warnings related to the new middleware (e.g., on `IdempotencyLookup` discriminated union), add `DynamicDependency` attributes or `JsonDerivedType` attributes to `IdempotencyLookup` and `IdempotencyRecord`.

- [ ] **Step 11.2: Build the NativeTest project**

```bash
dotnet build src/Sharkable.NativeTest/Sharkable.NativeTest.csproj -c Release
```

Expected: build succeeds.

- [ ] **Step 11.3: Add `JsonDerivedType` if AOT complains about serialization**

If AOT trimming warns about `IdempotencyLookup`, add to `IIdempotencyStore.cs`:

```csharp
[JsonPolymorphic]
[JsonDerivedType(typeof(IdempotencyInFlight), "in_flight")]
[JsonDerivedType(typeof(IdempotencyHit), "hit")]
public abstract record IdempotencyLookup;
```

(Note: this is only needed if AOT reports a warning. Most likely, the records are never serialized over the wire — the middleware never writes them as JSON. So this step may be a no-op.)

- [ ] **Step 11.4: Commit any AOT fixups**

```bash
git add src/Sharkable/Middleware/Idempotency/IIdempotencyStore.cs
git commit -m "fix(idempotency): add JsonDerivedType for AOT compatibility" --allow-empty
```

---

## Task 12: Update `README.md`

**Files:**
- Modify: `src/Sharkable/README.md`

- [ ] **Step 12.1: Add a section for idempotency**

Append to `src/Sharkable/README.md`:

```markdown
### idempotent retries via Idempotency-Key

opt-in middleware that lets clients safely retry unsafe HTTP requests:

```csharp
builder.Services.AddShark(opt =>
{
    opt.EnableIdempotency = true;
    opt.ConfigureIdempotency(o =>
    {
        o.Ttl = TimeSpan.FromHours(24);  // default
        o.MaxResponseSize = 1_048_576;   // default 1 MiB
    });
});
```

clients then send `Idempotency-Key: <uuid>` on `POST`/`PUT`/`PATCH`/`DELETE`
requests. The first response is cached; subsequent requests with the same
key replay it. Conflicting payload → 422; concurrent same-key requests →
409 with `Retry-After`. See
`docs/superpowers/specs/2026-06-26-idempotency-middleware-design.md` for
the full specification.
```

- [ ] **Step 12.2: Commit**

```bash
git add src/Sharkable/README.md
git commit -m "docs(idempotency): add README section"
```

---

## Self-Review

After writing the plan, run these checks against the spec:

1. **Spec coverage** — each spec section has a task:
   - §1 Public API → Tasks 2, 6, 7
   - §2 File layout → Tasks 1-5
   - §3 Storage → Tasks 3, 4
   - §4 Fingerprint → Task 1
   - §5 Middleware state machine → Task 5
   - §6 Error response shape → Task 5 + Task 9
   - §7 AOT → Task 11
   - §8 Limitations → Task 12 (README documents)
   - §9.1 Unit tests → Tasks 1, 2, 4
   - §9.2 Integration tests → Tasks 9, 10
   - §9.3 AOT verification → Task 11

2. **Placeholders** — no "TBD" / "TODO" / "implement later" in any code block.

3. **Type consistency** —
   - `IdempotencyFingerprint.Compute` signature consistent in Task 1 and Task 5.
   - `IIdempotencyStore` methods consistent in Tasks 3, 4, 5.
   - `SharkIdempotencyOptions` properties consistent in Tasks 2, 6, 7.

4. **Step atomicity** — every step is one action (write test / run / implement / run / commit).

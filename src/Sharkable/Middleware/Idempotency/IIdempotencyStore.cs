namespace Sharkable;

/// <summary>
/// Stores idempotency keys, their in-flight placeholders, and the responses
/// they produced. Implementations are responsible for atomicity of
/// <see cref="TryReserveAsync"/>; the middleware relies on it.
/// All methods are async to allow distributed store plugins (Redis, PostgreSQL, etc.).
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
    /// <returns><c>true</c> if reservation succeeded; <c>false</c> if the key was already taken.</returns>
    Task<bool> TryReserveAsync(string key, TimeSpan inFlightTtl);

    /// <summary>
    /// Returns the current state of the key: in-flight, completed, or absent (null).
    /// </summary>
    /// <param name="key">The Idempotency-Key header value.</param>
    /// <returns>An <see cref="IdempotencyInFlight"/>, <see cref="IdempotencyHit"/>, or <c>null</c>.</returns>
    Task<IdempotencyLookup?> GetAsync(string key);

    /// <summary>
    /// Stores the completed response under the key. Replaces any in-flight
    /// placeholder. Applies the configured TTL.
    /// </summary>
    /// <param name="key">The Idempotency-Key header value.</param>
    /// <param name="record">The response record to cache.</param>
    /// <param name="ttl">How long the record should live in the store.</param>
    Task StoreAsync(string key, IdempotencyRecord record, TimeSpan ttl);

    /// <summary>
    /// Removes the in-flight placeholder without storing a record. Called
    /// when the response should not be cached (5xx, 429, oversize).
    /// </summary>
    /// <param name="key">The Idempotency-Key header value.</param>
    Task ReleaseAsync(string key);
}

/// <summary>
/// A completed response stored under an <c>Idempotency-Key</c>. Used for
/// replay and for detecting "same key, different payload" via
/// <see cref="Fingerprint"/>.
/// </summary>
/// <param name="Key">The Idempotency-Key header value.</param>
/// <param name="Fingerprint">SHA-256 hex of the request identity (64 chars).</param>
/// <param name="StatusCode">The HTTP status code of the original response.</param>
/// <param name="ContentType">The original response Content-Type, or a sensible default if absent.</param>
/// <param name="Body">The buffered response body bytes.</param>
/// <param name="CompletedAt">When the original response completed (UTC).</param>
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
/// <param name="Record">The cached response.</param>
public sealed record IdempotencyHit(IdempotencyRecord Record) : IdempotencyLookup;

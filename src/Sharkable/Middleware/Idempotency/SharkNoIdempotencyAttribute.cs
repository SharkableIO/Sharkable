namespace Sharkable;

/// <summary>
/// Marks an <see cref="ISharkEndpoint"/> class (or individual route method) that
/// should not participate in idempotency key handling. The idempotency middleware
/// will pass through without buffering the response for endpoints carrying this
/// metadata, releasing the in-flight idempotency slot immediately.
/// <para>
/// Use this on SSE / streaming endpoints where response buffering would silently
/// break the streaming behavior. See BUG-10.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SharkNoIdempotencyAttribute : Attribute { }

/// <summary>
/// Opts an <see cref="ISharkEndpoint"/> class into idempotency-key handling
/// when global idempotency is disabled. The <see cref="SharkIdempotencyMiddleware"/>
/// checks for this metadata per-endpoint and applies idempotency semantics
/// only for endpoints that carry it.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class SharkIdempotentAttribute : Attribute
{
    /// <summary>
    /// TTL in seconds for completed idempotency records for this endpoint.
    /// Falls back to <see cref="SharkIdempotencyOptions.Ttl"/> when not set.
    /// </summary>
    public int? TtlSeconds { get; set; }

    /// <summary>
    /// Initializes a new instance with no custom TTL (uses global default).
    /// </summary>
    public SharkIdempotentAttribute() { }

    /// <summary>
    /// Initializes a new instance with a custom TTL for completed idempotency records.
    /// </summary>
    /// <param name="ttlSeconds">TTL in seconds for completed idempotency records.</param>
    public SharkIdempotentAttribute(int ttlSeconds)
    {
        TtlSeconds = ttlSeconds;
    }
}

/// <summary>
/// Internal metadata marker added to endpoint builders whose endpoint class
/// carries <see cref="SharkNoIdempotencyAttribute"/>. Checked by
/// <see cref="SharkIdempotencyMiddleware"/> to skip response buffering.
/// </summary>
internal sealed class NoIdempotencyMetadata { }

/// <summary>
/// Internal metadata marker indicating this endpoint has opted into idempotency
/// via <see cref="SharkIdempotentAttribute"/>. Checked by
/// <see cref="SharkIdempotencyMiddleware"/> to enable per-endpoint idempotency.
/// </summary>
internal sealed class SharkIdempotentMetadata
{
    /// <summary>
    /// Optional per-endpoint TTL in seconds.
    /// </summary>
    public int? TtlSeconds { get; }

    /// <summary>
    /// Initializes a new instance with optional TTL.
    /// </summary>
    public SharkIdempotentMetadata(int? ttlSeconds)
    {
        TtlSeconds = ttlSeconds;
    }
}

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
/// Internal metadata marker added to endpoint builders whose endpoint class
/// carries <see cref="SharkNoIdempotencyAttribute"/>. Checked by
/// <see cref="SharkIdempotencyMiddleware"/> to skip response buffering.
/// </summary>
internal sealed class NoIdempotencyMetadata { }

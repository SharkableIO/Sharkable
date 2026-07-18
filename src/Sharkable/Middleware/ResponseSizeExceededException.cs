namespace Sharkable;

/// <summary>
/// Thrown when a response body exceeds the configured size cap.
/// Caught by ETag and Idempotency middleware to skip caching or
/// ETag generation without crashing the response pipeline.
/// </summary>
[Serializable]
internal sealed class ResponseSizeExceededException : Exception
{
    public ResponseSizeExceededException()
        : base("Response body exceeded the configured MaxResponseSize cap.") { }
}

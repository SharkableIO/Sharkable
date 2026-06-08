using System.Net;

namespace Sharkable;

public class UnifiedResult<T> : IUnifiedResult
{
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    public T? Data { get; set; }
    public string? ErrorMessage { get; init; }
    public string? Extra { get; init; }
    public long? TimeStamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public UnifiedResult() { }

    public UnifiedResult(T? data, string? errorMessage = null, HttpStatusCode statusCode = HttpStatusCode.OK, string? extra = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
        StatusCode = statusCode;
        Extra = extra;
    }

    int IUnifiedResult.StatusCode => (int)StatusCode;
    object? IUnifiedResult.Data => Data;
    string? IUnifiedResult.ErrorMessage => ErrorMessage;
}

public static class UnifiedResult
{
    public static object? GetUnifiedResult<T>(T? data)
    {
        return new UnifiedResult<T> { Data = data };
    }
}

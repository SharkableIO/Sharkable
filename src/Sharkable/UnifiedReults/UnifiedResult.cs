using System.Net;
using System.Text.Json.Serialization;

namespace Sharkable;

/// <summary>
/// class generics for unified results
/// </summary>
/// <typeparam name="T"></typeparam>
public record UnifiedResult<T>
{
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Extra { get; init; }
    public long? TimeStamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public UnifiedResult()
    {
        
    }
    public UnifiedResult(T? data,  string? errorMessage = null, string? extra = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
        Extra = extra;
    }

    public UnifiedResult(object data,Type type, string? errorMessage = null, string? extra = null)
    {
        Data = (T?)data;
        ErrorMessage = errorMessage;
        Extra = extra;
    }
}
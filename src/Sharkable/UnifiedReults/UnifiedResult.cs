using System.Net;
using System.Text.Json.Serialization;

namespace Sharkable;

public class MyUnifiedResult<T>
{
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    public T? Data { get; init; }
    public string? ErrorMessage { get; init; }
    public string? Extra { get; init; }
    public long? TimeStamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    public MyUnifiedResult()
    {
        
    }
    public MyUnifiedResult(T? data,  string? errorMessage = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
    }
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(MyUnifiedResult<>))]
[JsonSerializable(typeof(MyUnifiedResult<string>))]
[JsonSerializable(typeof(MyUnifiedResult<int>))]
public partial class MyUnifiedResultSourceContext : JsonSerializerContext
{
    
}
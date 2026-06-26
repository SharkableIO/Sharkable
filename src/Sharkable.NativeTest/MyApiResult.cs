namespace Sharkable.NativeTest;

// Custom unified result — completely different shape from UnifiedResult<T>
// OLD: statusCode, data, errorMessage, extra, timeStamp
// NEW: ok, body, err, code, rid, at
public class MyApiResult : IUnifiedResult
{
    public bool Ok { get; init; }
    public object? Body { get; init; }
    public string? Err { get; init; }
    public int Code { get; init; }
    public string Rid { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string At { get; init; } = DateTimeOffset.UtcNow.ToString("o");

    int IUnifiedResult.StatusCode => Code;
    object? IUnifiedResult.Data => Body;
    string? IUnifiedResult.ErrorMessage => Err;
}

public sealed class MyApiResultFactory : IUnifiedResultFactory
{
    public IUnifiedResult Create(object? data, string? errorMessage, int statusCode)
        => Create(data, errorMessage, statusCode, code: null);

    public IUnifiedResult Create(object? data, string? errorMessage, int statusCode, string? code)
    {
        return new MyApiResult
        {
            Ok = errorMessage == null,
            Body = data,
            Err = errorMessage,
            Code = statusCode,
        };
    }
}

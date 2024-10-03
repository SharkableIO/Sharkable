using System.Net;

namespace Sharkable;

/// <summary>
/// generic class for unified results
/// </summary>
/// <typeparam name="T"></typeparam>
public class UnifiedResult<T>
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
    public UnifiedResult(object? data,
        string? errorMessage = null, 
        string? extra = null)
    {
        Data = (T?)data;
        ErrorMessage = errorMessage;
        Extra = extra;
    }
    public UnifiedResult(T? data, 
        string? errorMessage = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null,
        DateTimeOffset? dateTimeOffset = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
        Extra = extra;
        StatusCode = statusCode;
        TimeStamp = dateTimeOffset == null ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : dateTimeOffset?.ToUnixTimeMilliseconds();
    }
    public UnifiedResult(T? data, 
        string? errorMessage = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null,
        long? timeStamp = null)
    {
        Data = data;
        ErrorMessage = errorMessage;
        Extra = extra;
        StatusCode = statusCode;
        TimeStamp = timeStamp == null ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : timeStamp;
    }
}

/// <summary>
/// Unified result utils
/// </summary>
public static class UnifiedResult
{
    /// <summary>
    /// get an instance of the unified result class object
    /// </summary>
    /// <param name="data">data which need to be assigned</param>
    /// <param name="type">type of the given data</param>
    /// <returns></returns>
    public static object? GetUnifiedResult(object? data, Type type)
    {
        var genericType = typeof(UnifiedResult<>);
        var specificType = genericType.MakeGenericType(type);
        var instance = Activator.CreateInstance(specificType);
        var propertyInfo = specificType.GetProperty("Data");
        propertyInfo?.SetValue(instance, data);
        return instance;
    }

    /// <summary>
    /// get an instance of the unified result class object
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="data">data which need to be assigned</param>
    /// <returns></returns>
    public static object? GetUnifiedResult<T>(T? data)
    {
        return GetUnifiedResult(data, typeof(T));
    }
}
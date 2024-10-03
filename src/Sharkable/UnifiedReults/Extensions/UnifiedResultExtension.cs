using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

public static class UnifiedResultExtension
{
    /// <summary>
    /// produce an unified result
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="data"></param>
    /// <param name="errors"></param>
    /// <param name="statusCode"></param>
    /// <param name="extra"></param>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public static UnifiedResult<TResult>? AsUnifiedResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? null : new UnifiedResult<TResult>(data, errors, statusCode, extra, timeStamp);
    }
    /// <summary>
    /// produce as unified error
    /// </summary>
    /// <typeparam name="TResult"></typeparam>
    /// <param name="data"></param>
    /// <param name="errors"></param>
    /// <param name="statusCode"></param>
    /// <param name="extra"></param>
    /// <param name="timeStamp"></param>
    /// <returns></returns>
    public static UnifiedResult<string>? AsUnifiedError(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return errors == null ? null : new UnifiedResult<string>(null, errors, statusCode, extra, timeStamp);
    }
    
    public static IResult AsOkResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? default! : Results.Ok(data.AsUnifiedResult(errors, statusCode, extra, timeStamp));
    }
    public static IResult AsBadRequest(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return errors == null ? default! : Results.BadRequest(errors.AsUnifiedError(statusCode, extra, timeStamp));
    }
    public static IResult AsUnauthorized(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.Unauthorized, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return errors == null ? default! : Results.BadRequest(errors.AsUnifiedError(statusCode, extra, timeStamp));
    }
}
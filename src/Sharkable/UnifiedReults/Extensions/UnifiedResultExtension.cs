using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

public static class UnifiedResultExtension
{
    public static UnifiedResult<TResult>? AsUnifiedResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? null : new UnifiedResult<TResult>(data, errors, statusCode, extra, timeStamp);
    }
    
    public static IResult AsOkResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? default! : Results.Ok(data.AsUnifiedResult(errors, statusCode, extra, timeStamp));
    }
    public static IResult AsBadRequest<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? default! : Results.BadRequest(data.AsUnifiedResult(errors, statusCode, extra, timeStamp));
    }
    public static IResult AsUnauthorized<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.Unauthorized, 
        string? extra = null, 
        DateTimeOffset? timeStamp = null)
    {
        return data == null ? default! : Results.BadRequest(data.AsUnifiedResult(errors, statusCode, extra, timeStamp));
    }
}
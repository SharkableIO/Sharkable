using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

public static class UnifiedResultExtension
{
    public static UnifiedResult<TResult>? AsUnifiedResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return data == null ? null : new UnifiedResult<TResult>(data, errors, statusCode, extra);
    }

    public static UnifiedResult<string>? AsUnifiedError(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return errors == null ? null : new UnifiedResult<string>(null, errors, statusCode, extra);
    }

    public static IResult AsOkResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return data == null ? Results.Ok() : Results.Ok(data.AsUnifiedResult(errors, statusCode, extra));
    }

    public static IResult AsBadRequest(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, 
        string? extra = null)
    {
        return errors == null ? Results.BadRequest() : Results.BadRequest(errors.AsUnifiedError(statusCode, extra));
    }

    public static IResult AsUnauthorized(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.Unauthorized, 
        string? extra = null)
    {
        return errors == null ? Results.Unauthorized() : Results.Unauthorized();
    }
}

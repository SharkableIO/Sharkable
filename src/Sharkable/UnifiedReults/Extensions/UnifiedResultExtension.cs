using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Extension methods for wrapping values in <see cref="UnifiedResult{T}"/> and returning <see cref="IResult"/>.
/// </summary>
public static class UnifiedResultExtension
{
    /// <summary>Wraps data in a <see cref="UnifiedResult{TResult}"/> with optional error and status code.</summary>
    public static UnifiedResult<TResult>? AsUnifiedResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return data == null ? null : new UnifiedResult<TResult>(data, errors, statusCode, extra);
    }

    /// <summary>Wraps an error string in a <see cref="UnifiedResult{T}"/> with the given status code.</summary>
    public static UnifiedResult<string>? AsUnifiedError(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return errors == null ? null : new UnifiedResult<string>(null, errors, statusCode, extra);
    }

    /// <summary>Returns an <see cref="IResult"/> that serializes the data wrapped in <see cref="UnifiedResult{TResult}"/>.</summary>
    public static IResult AsOkResult<TResult>(this TResult? data, 
        string? errors = null, 
        HttpStatusCode statusCode = HttpStatusCode.OK, 
        string? extra = null)
    {
        return data == null ? Results.Ok() : Results.Ok(data.AsUnifiedResult(errors, statusCode, extra));
    }

    /// <summary>Returns a 400 <see cref="IResult"/> with the given error message in <see cref="UnifiedResult{T}"/> format.</summary>
    public static IResult AsBadRequest(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.BadRequest, 
        string? extra = null)
    {
        return errors == null ? Results.BadRequest() : Results.BadRequest(errors.AsUnifiedError(statusCode, extra));
    }

    /// <summary>Returns a 401 <see cref="IResult"/>.</summary>
    public static IResult AsUnauthorized(this string? errors, 
        HttpStatusCode statusCode = HttpStatusCode.Unauthorized, 
        string? extra = null)
    {
        return errors == null ? Results.Unauthorized() : Results.Unauthorized();
    }
}

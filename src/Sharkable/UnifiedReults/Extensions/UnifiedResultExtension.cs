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
        return Results.Unauthorized();
    }

    /// <summary>Returns a 404 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsNotFound(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.NotFound,
        string? extra = null)
    {
        return errors == null ? Results.NotFound() : Results.NotFound(errors.AsUnifiedError(statusCode, extra));
    }

    /// <summary>Returns a 403 <see cref="IResult"/>.</summary>
    public static IResult AsForbidden(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.Forbidden,
        string? extra = null)
    {
        return Results.Forbid();
    }

    /// <summary>Returns a 409 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsConflict(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.Conflict,
        string? extra = null)
    {
        return errors == null ? Results.Conflict() : Results.Conflict(errors.AsUnifiedError(statusCode, extra));
    }

    /// <summary>Returns a 204 <see cref="IResult"/>.</summary>
    public static IResult AsNoContent()
    {
        return Results.NoContent();
    }

    /// <summary>Returns a 201 <see cref="IResult"/> with the given data wrapped in UnifiedResult.</summary>
    public static IResult AsCreated<TResult>(this TResult? data,
        string? uri = null,
        string? errors = null,
        string? extra = null)
    {
        var result = data.AsUnifiedResult(errors, HttpStatusCode.Created, extra);
        if (result == null) return Results.StatusCode(201);
        return uri != null ? Results.Created(uri, result) : Results.Ok(result);
    }

    /// <summary>Returns a 202 <see cref="IResult"/> with the given data and optional location URI.</summary>
    public static IResult AsAccepted<TResult>(this TResult? data,
        string? uri = null,
        string? errors = null,
        string? extra = null)
    {
        var result = data.AsUnifiedResult(errors, HttpStatusCode.Accepted, extra);
        if (result == null) return Results.StatusCode(202);
        return uri != null ? Results.Accepted(uri, result) : Results.Json(result, statusCode: 202);
    }

    /// <summary>Returns a 405 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsMethodNotAllowed(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.MethodNotAllowed,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 406 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsNotAcceptable(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.NotAcceptable,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 410 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsGone(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.Gone,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 415 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsUnsupportedMediaType(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.UnsupportedMediaType,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 422 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsUnprocessableEntity(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.UnprocessableEntity,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 429 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsTooManyRequests(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.TooManyRequests,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 500 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsInternalServerError(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.InternalServerError,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 501 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsNotImplemented(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.NotImplemented,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 502 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsBadGateway(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.BadGateway,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 503 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsServiceUnavailable(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.ServiceUnavailable,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns a 504 <see cref="IResult"/> with the given error message.</summary>
    public static IResult AsGatewayTimeout(this string? errors,
        HttpStatusCode statusCode = HttpStatusCode.GatewayTimeout,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns an <see cref="IResult"/> with the given custom status code and error message.</summary>
    public static IResult AsStatus(this string? errors,
        HttpStatusCode statusCode,
        string? extra = null)
    {
        return errors == null ? Results.StatusCode((int)statusCode) : Results.Json(errors.AsUnifiedError(statusCode, extra), statusCode: (int)statusCode);
    }

    /// <summary>Returns an <see cref="IResult"/> with the given custom status code and data wrapped in <see cref="UnifiedResult{TResult}"/>.</summary>
    public static IResult AsStatus<TResult>(this TResult? data,
        HttpStatusCode statusCode,
        string? errors = null,
        string? extra = null)
    {
        var result = data.AsUnifiedResult(errors, statusCode, extra);
        return result == null ? Results.StatusCode((int)statusCode) : Results.Json(result, statusCode: (int)statusCode);
    }
}

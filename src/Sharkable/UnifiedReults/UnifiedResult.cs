using System.Net;
using Microsoft.AspNetCore.Http;

namespace Sharkable;

/// <summary>
/// Default unified response type. Returned by the exception handler, auto-wrap filter,
/// and extension methods like <c>AsOkResult()</c>. Pluggable via <see cref="IUnifiedResultFactory"/>.
/// </summary>
/// <typeparam name="T">Type of the response payload.</typeparam>
public class UnifiedResult<T> : IUnifiedResult
{
    /// <summary>HTTP status code for the response.</summary>
    public HttpStatusCode StatusCode { get; init; } = HttpStatusCode.OK;
    /// <summary>Response payload data.</summary>
    public T? Data { get; set; }
    /// <summary>Error message. Null when the request succeeds.</summary>
    public string? ErrorMessage { get; init; }
    /// <summary>Optional extra metadata attached to the response.</summary>
    public string? Extra { get; init; }
    /// <summary>UTC timestamp in milliseconds since epoch.</summary>
    public long? TimeStamp { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    /// <summary>Creates an empty unified result with default OK status.</summary>
    public UnifiedResult() { }

    /// <summary>Creates a unified result with the given data and optional error/status metadata.</summary>
    /// <param name="data">Response payload.</param>
    /// <param name="errorMessage">Error message (null when successful).</param>
    /// <param name="statusCode">HTTP status code.</param>
    /// <param name="extra">Optional extra metadata.</param>
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

/// <summary>
/// Static helpers for creating <see cref="UnifiedResult{T}"/> instances and returning <see cref="IResult"/>.
/// </summary>
public static class UnifiedResult
{
    /// <summary>Creates a <see cref="UnifiedResult{T}"/> with the given data and a 200 status code.</summary>
    public static object? GetUnifiedResult<T>(T? data)
    {
        return new UnifiedResult<T> { Data = data };
    }

    /// <summary>Returns a 200 <see cref="IResult"/> with the given data wrapped in <see cref="UnifiedResult{T}"/>.</summary>
    public static IResult Ok<T>(T? data, string? errors = null, string? extra = null)
    {
        return data == null ? Results.Ok() : Results.Ok(new UnifiedResult<T>(data, errors, HttpStatusCode.OK, extra));
    }

    /// <summary>Returns a 400 <see cref="IResult"/> with the given error message.</summary>
    public static IResult BadRequest(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.BadRequest() : Results.BadRequest(new UnifiedResult<string>(null, errors, HttpStatusCode.BadRequest, extra));
    }

    /// <summary>Returns a 401 <see cref="IResult"/>.</summary>
    public static IResult Unauthorized()
    {
        return Results.Unauthorized();
    }

    /// <summary>Returns a 403 <see cref="IResult"/>.</summary>
    public static IResult Forbidden()
    {
        return Results.Forbid();
    }

    /// <summary>Returns a 404 <see cref="IResult"/> with the given error message.</summary>
    public static IResult NotFound(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.NotFound() : Results.NotFound(new UnifiedResult<string>(null, errors, HttpStatusCode.NotFound, extra));
    }

    /// <summary>Returns a 409 <see cref="IResult"/> with the given error message.</summary>
    public static IResult Conflict(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.Conflict() : Results.Conflict(new UnifiedResult<string>(null, errors, HttpStatusCode.Conflict, extra));
    }

    /// <summary>Returns a 204 <see cref="IResult"/>.</summary>
    public static IResult NoContent()
    {
        return Results.NoContent();
    }

    /// <summary>Returns a 201 <see cref="IResult"/> with the given data wrapped in <see cref="UnifiedResult{T}"/>.</summary>
    public static IResult Created<T>(T? data, string? uri = null, string? errors = null, string? extra = null)
    {
        var result = new UnifiedResult<T>(data, errors, HttpStatusCode.Created, extra);
        return uri != null ? Results.Created(uri, result) : Results.Ok(result);
    }

    /// <summary>Returns a 202 <see cref="IResult"/> with the given data and optional location URI.</summary>
    public static IResult Accepted<T>(T? data, string? uri = null, string? errors = null, string? extra = null)
    {
        var result = new UnifiedResult<T>(data, errors, HttpStatusCode.Accepted, extra);
        if (data == null) return Results.StatusCode(202);
        return uri != null ? Results.Accepted(uri, result) : Results.Json(result, statusCode: 202);
    }

    /// <summary>Returns a 405 <see cref="IResult"/> with the given error message.</summary>
    public static IResult MethodNotAllowed(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(405) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.MethodNotAllowed, extra), statusCode: 405);
    }

    /// <summary>Returns a 406 <see cref="IResult"/> with the given error message.</summary>
    public static IResult NotAcceptable(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(406) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.NotAcceptable, extra), statusCode: 406);
    }

    /// <summary>Returns a 410 <see cref="IResult"/> with the given error message.</summary>
    public static IResult Gone(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(410) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.Gone, extra), statusCode: 410);
    }

    /// <summary>Returns a 415 <see cref="IResult"/> with the given error message.</summary>
    public static IResult UnsupportedMediaType(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(415) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.UnsupportedMediaType, extra), statusCode: 415);
    }

    /// <summary>Returns a 422 <see cref="IResult"/> with the given error message.</summary>
    public static IResult UnprocessableEntity(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(422) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.UnprocessableEntity, extra), statusCode: 422);
    }

    /// <summary>Returns a 429 <see cref="IResult"/> with the given error message.</summary>
    public static IResult TooManyRequests(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(429) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.TooManyRequests, extra), statusCode: 429);
    }

    /// <summary>Returns a 500 <see cref="IResult"/> with the given error message.</summary>
    public static IResult InternalServerError(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(500) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.InternalServerError, extra), statusCode: 500);
    }

    /// <summary>Returns a 501 <see cref="IResult"/> with the given error message.</summary>
    public static IResult NotImplemented(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(501) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.NotImplemented, extra), statusCode: 501);
    }

    /// <summary>Returns a 502 <see cref="IResult"/> with the given error message.</summary>
    public static IResult BadGateway(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(502) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.BadGateway, extra), statusCode: 502);
    }

    /// <summary>Returns a 503 <see cref="IResult"/> with the given error message.</summary>
    public static IResult ServiceUnavailable(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(503) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.ServiceUnavailable, extra), statusCode: 503);
    }

    /// <summary>Returns a 504 <see cref="IResult"/> with the given error message.</summary>
    public static IResult GatewayTimeout(string? errors = null, string? extra = null)
    {
        return errors == null ? Results.StatusCode(504) : Results.Json(new UnifiedResult<string>(null, errors, HttpStatusCode.GatewayTimeout, extra), statusCode: 504);
    }

    /// <summary>Returns an <see cref="IResult"/> with the given custom status code, data, and optional error message.</summary>
    public static IResult Status<T>(T? data, HttpStatusCode statusCode, string? errors = null, string? extra = null)
    {
        if (data == null && errors == null)
            return Results.StatusCode((int)statusCode);
        return Results.Json(new UnifiedResult<T>(data, errors, statusCode, extra), statusCode: (int)statusCode);
    }
}

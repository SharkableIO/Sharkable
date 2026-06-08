using System.Net;

namespace Sharkable;

/// <summary>
/// Configuration for the global exception handler middleware.
/// Controls status code mappings and error message verbosity.
/// </summary>
public sealed class ExceptionHandlerOptions
{
    private readonly Dictionary<Type, HttpStatusCode> _exceptionMappings;

    public ExceptionHandlerOptions()
    {
        _exceptionMappings = new Dictionary<Type, HttpStatusCode>
        {
            { typeof(KeyNotFoundException), HttpStatusCode.NotFound },
            { typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized },
            { typeof(ArgumentException), HttpStatusCode.BadRequest },
        };
    }

    /// <summary>
    /// When true, include full exception details (stack trace) in the error response.
    /// Automatically set to <see cref="IHostEnvironment.IsDevelopment()"/> by default.
    /// </summary>
    public bool IsDevelopment { get; set; }

    /// <summary>
    /// Map an exception type to an HTTP status code.
    /// </summary>
    public void Map<TException>(HttpStatusCode statusCode) where TException : Exception
    {
        _exceptionMappings[typeof(TException)] = statusCode;
    }

    /// <summary>
    /// Resolve the HTTP status code for a given exception by walking the type hierarchy.
    /// Falls back to 500 InternalServerError if no mapping is found.
    /// </summary>
    public HttpStatusCode GetStatusCode(Exception exception)
    {
        var type = exception.GetType();
        while (type != null && type != typeof(Exception))
        {
            if (_exceptionMappings.TryGetValue(type, out var statusCode))
                return statusCode;
            type = type.BaseType;
        }
        return HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Get the error message for the response.
    /// In development mode includes the full ToString() (including stack trace).
    /// </summary>
    public string GetErrorMessage(Exception exception)
    {
        return IsDevelopment ? exception.ToString() : exception.Message;
    }
}

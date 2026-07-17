using System.Net;

namespace Sharkable;

/// <summary>
/// Configuration for the global exception handler middleware.
/// Controls status code mappings and error message verbosity.
/// </summary>
public sealed class ExceptionHandlerOptions
{
    private readonly Dictionary<Type, HttpStatusCode> _exceptionMappings;

    /// <summary>
    /// Initialises a new instance with default exception-to-status mappings.
    /// </summary>
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
    /// Defaults to <c>false</c>; explicitly opt-in for local debugging only.
    /// <para>
    /// SHARK-SEC-M003: <c>exception.ToString()</c> includes the assembly-qualified
    /// type name, internal member names, and full stack trace — sending it to
    /// arbitrary clients leaks implementation details. Even in development,
    /// callers should prefer <c>exception.Message</c>; when this flag is set,
    /// the framework strips the assembly-qualified type prefix so the response
    /// stays usable but does not advertise the assembly / version.
    /// </para>
    /// </summary>
    public bool IsDevelopment { get; set; }

    /// <summary>
    /// When <c>true</c> (opt-in), production error responses include the actual
    /// <see cref="Exception.Message"/>. When <c>false</c> (default), production
    /// responses return a generic "An error occurred." message.
    /// <see cref="IsDevelopment"/> continues to return the stripped full exception
    /// text regardless of this setting. Default is <c>false</c>.
    /// </summary>
    public bool IncludeExceptionMessage { get; set; } = false;

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
    /// In development mode (<see cref="IsDevelopment"/> = <c>true</c>) the full
    /// exception is included but with the assembly-qualified type name stripped
    /// so the response cannot leak the exact framework version / assembly.
    /// In production, returns a generic message unless <see cref="IncludeExceptionMessage"/>
    /// is <c>true</c>.
    /// </summary>
    public string GetErrorMessage(Exception exception)
    {
        if (IsDevelopment)
        {
            var full = exception.ToString();
            var fullTypeName = exception.GetType().FullName;
            var shortTypeName = exception.GetType().Name;
            if (!string.IsNullOrEmpty(fullTypeName) && full.StartsWith(fullTypeName, StringComparison.Ordinal))
                return shortTypeName + full[fullTypeName.Length..];
            return full;
        }

        return IncludeExceptionMessage
            ? exception.Message
            : "An error occurred.";
    }
}

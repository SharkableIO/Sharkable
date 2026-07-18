namespace Sharkable;

/// <summary>
/// Controls the shape of validation error responses produced by
/// <see cref="ValidationFilter"/>.
/// </summary>
public enum ValidationErrorMode
{
    /// <summary>
    /// Validation error messages are joined with <c>"; "</c> and returned
    /// in the <c>ErrorMessage</c> field of the unified result envelope.
    /// This is the default and matches the existing behavior.
    /// </summary>
    Messages = 0,

    /// <summary>
    /// Validation errors are emitted as RFC 7807 ProblemDetails with an
    /// <c>errors</c> object mapping field names to arrays of messages.
    /// Consistent with <c>UseProblemDetails</c> and
    /// <c>ValidationProblem()</c>.
    /// </summary>
    ProblemDetails = 1,
}

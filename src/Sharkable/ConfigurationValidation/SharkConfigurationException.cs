namespace Sharkable;

/// <summary>
/// Exception thrown when Sharkable configuration validation fails at startup.
/// Provides a list of specific misconfiguration messages to help users quickly fix setup issues.
/// </summary>
public sealed class SharkConfigurationException : Exception
{
    /// <summary>
    /// Creates a new <see cref="SharkConfigurationException"/> with the given error messages.
    /// </summary>
    /// <param name="errors">The list of configuration validation errors.</param>
    public SharkConfigurationException(IReadOnlyList<string> errors)
        : base($"Sharkable configuration validation failed:\n{string.Join("\n", errors.Select(e => $"  - {e}"))}")
    {
        Errors = errors;
    }

    /// <summary>
    /// The individual configuration validation errors.
    /// </summary>
    public IReadOnlyList<string> Errors { get; }
}

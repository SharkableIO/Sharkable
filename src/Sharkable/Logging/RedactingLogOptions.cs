namespace Sharkable;

/// <summary>
/// Options for configuring structured log field redaction via <see cref="RedactingLoggerProvider"/>.
/// </summary>
public sealed class RedactingLogOptions
{
    /// <summary>
    /// Field names whose values should be redacted in structured log output.
    /// Comparison is case-insensitive.
    /// </summary>
    public string[] RedactFields { get; set; } = ["password", "secret", "token", "apiKey", "authorization", "creditCard", "ssn"];

    /// <summary>
    /// Replacement text for redacted values. Default is <c>"***"</c>.
    /// </summary>
    public string RedactWith { get; set; } = "***";
}

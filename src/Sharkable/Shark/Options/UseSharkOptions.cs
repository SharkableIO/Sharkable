namespace Sharkable;

/// <summary>
/// Runtime configuration for the Sharkable application pipeline. Passed via callback in <c>app.UseShark(opt => ...)</c>.
/// </summary>
public sealed class UseSharkOptions : ISharkOption
{
    /// <summary>
    /// When <c>true</c> (default), <c>UseShark()</c> wires the global exception handler middleware.
    /// </summary>
    public bool EnableExceptionHandler { get; set; } = true;
    /// <summary>
    /// When <c>true</c>, endpoint return values that are not <see cref="IResult"/> are
    /// automatically wrapped in <see cref="UnifiedResult{T}"/>.
    /// Default is <c>false</c> (opt-in).
    /// </summary>
    public bool EnableAutoWrap { get; set; } = false;
}

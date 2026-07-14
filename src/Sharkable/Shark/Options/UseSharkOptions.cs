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

    /// <summary>
    /// Adds a custom middleware <see cref="Action{WebApplication}"/> that runs
    /// before authentication/authorization in the pipeline.
    /// Useful for request logging, header validation, or custom pre-auth logic.
    /// </summary>
    public void AddBeforeAuth(Action<WebApplication> configure)
    {
        BeforeAuthActions.Add(configure);
    }
    internal List<Action<WebApplication>> BeforeAuthActions { get; set; } = [];

    /// <summary>
    /// Adds a custom middleware <see cref="Action{WebApplication}"/> that runs
    /// after authentication/authorization but before endpoint mapping.
    /// Useful for tenant resolution, culture setting, or request enrichment.
    /// </summary>
    public void AddAfterAuth(Action<WebApplication> configure)
    {
        AfterAuthActions.Add(configure);
    }
    internal List<Action<WebApplication>> AfterAuthActions { get; set; } = [];

    /// <summary>
    /// Adds a custom middleware <see cref="Action{WebApplication}"/> that runs
    /// after all Sharkable endpoints have been mapped.
    /// Useful for fallback routes, custom error pages, or post-endpoint middleware.
    /// </summary>
    public void AddAfterEndpoints(Action<WebApplication> configure)
    {
        AfterEndpointsActions.Add(configure);
    }
    internal List<Action<WebApplication>> AfterEndpointsActions { get; set; } = [];
}

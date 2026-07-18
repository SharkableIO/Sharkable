using Microsoft.AspNetCore.OpenApi;

namespace Sharkable;

/// <summary>
/// A Sharkable plugin. Implement this to ship a NuGet package
/// or a hot-plug folder that auto-integrates with the framework.
/// Discovered by assembly scanning or folder scanning at startup.
/// </summary>
public interface ISharkPlugin
{
    /// <summary>
    /// Human-readable unique name for diagnostics, opt-out, and deduplication.
    /// Example: "Sharkable.Cache.Redis"
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Called during <c>AddShark()</c>. Register services, options, stores here.
    /// Use <c>TryAdd*</c> patterns so the host application wins on conflicts.
    /// </summary>
    void ConfigureServices(IServiceCollection services, SharkOption option);

    /// <summary>
    /// Called during <c>UseShark()</c> after framework middleware is wired.
    /// Wire plugin middleware, map additional endpoints, or configure
    /// request-time pipeline behavior here.
    /// </summary>
    void ConfigurePipeline(WebApplication app, SharkOption option);

    /// <summary>
    /// Called during OpenAPI document generation. Add schemas, operation
    /// transformers, or document transformers here.
    /// Called only when <c>UseOpenApi</c> is enabled.
    /// </summary>
    void ConfigureOpenApi(OpenApiOptions openApiOptions, SharkOption option);
}

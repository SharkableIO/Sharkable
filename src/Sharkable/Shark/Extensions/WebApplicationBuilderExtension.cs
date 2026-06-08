
namespace Sharkable;

/// <summary>
/// Extension methods for configuring Sharkable at the builder stage.
/// </summary>
public static class WebApplicationBuilderExtension
{
    /// <summary>
    /// Configures the <see cref="WebApplicationBuilder"/> with Sharkable hosting and assembly context.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <param name="assemblies">Assemblies to register for endpoint and service discovery.</param>
    /// <param name="configure">Optional additional builder configuration.</param>
    public static WebApplicationBuilder Sharkable(this WebApplicationBuilder builder, Assembly[]? assemblies, Action<WebApplicationBuilder>? configure = null)
    {
        InternalShark.WebHostEnvironment = builder.Environment;
        InternalShark.ConfigureShark(builder.WebHost, assemblies, builder.Host);
        return builder;
    }
}


namespace Sharkable;

public static class WebApplicationBuilderExtension
{
    public static WebApplicationBuilder Sharkable(this WebApplicationBuilder builder, Assembly[]? assemblies, Action<WebApplicationBuilder>? configure = null)
    {
        InternalShark.WebHostEnvironment = builder.Environment;
        InternalShark.ConfigureShark(builder.WebHost, assemblies, builder.Host);
        return builder;
    }
}

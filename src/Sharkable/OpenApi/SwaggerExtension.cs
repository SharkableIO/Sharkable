
using Scalar.AspNetCore;

namespace Sharkable;

internal static class OpenApiExtension
{
    internal static IServiceCollection AddSharkOpenApi(this IServiceCollection services)
    {
        if (Shark.SharkOption.UseOpenApi)
        {
            var configure = SharkOption.OpenApiConfigure;
            if (configure != null)
                services.AddOpenApi(configure);
            else
                services.AddOpenApi();
        }
        return services;
    }

    internal static WebApplication UseSharkOpenApi(this WebApplication app)
    {
        if (Shark.SharkOption.UseOpenApi)
        {
            app.MapOpenApi();
            app.MapScalarApiReference();
        }
        return app;
    }
}


using Microsoft.AspNetCore.Routing.Constraints;

namespace Sharkable;

internal static class SwaggerExtension
{
    internal static IServiceCollection SharkSwagger(this IServiceCollection services)
    {
        //skip Swagger setup when AutoCrud (SqlSugar) is configured — autocrud manages its own OpenAPI
        if(SharkOption.SqlSugarOptionsConfigure != null)
            return services;
        // Add services to the container.
        services.Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });

        if (Shark.SharkOption.UseSwaggerDoc)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(SharkOption.SwaggerGenConfigure);
        }
        return services;
    }

    internal static void UseSharkSwagger(this WebApplication app)
    {
        if (Shark.SharkOption.UseSwaggerDoc)
        {
            app.UseSwagger(UseSharkOptions.UseSwaggerConfigure);
            app.UseSwaggerUI();
        }
    }
}

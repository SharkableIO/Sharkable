
namespace Sharkable;

internal static class SwaggerExtension
{
    internal static IServiceCollection SharkSwagger(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        if (Shark.SharkOption.UseSwaggerDoc)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(SharkOption.SwaggerGenConfigure);
        }
        return services;
    }

    internal static void UseSharkSwagger(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
    {
        if (Shark.SharkOption.UseSwaggerDoc)
        {
            app.UseSwagger(UseSharkOptions.UseSwaggerConfigure);
            app.UseSwaggerUI();
        }
    }
}

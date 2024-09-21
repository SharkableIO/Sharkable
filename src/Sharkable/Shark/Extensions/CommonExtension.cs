using Microsoft.AspNetCore.Routing.Constraints;

namespace Sharkable;

public static class CommonExtension
{
    internal static void AddCommom(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        var option = new SharkOption();
        if(setupOptions != null)
        {
            setupOptions(option);
            services.Configure(setupOptions);
        }
        else
        {
            services.Configure<SharkOption>((opt)=> { opt = option; });
        }
        // Add services to the container.
        services.Configure<RouteOptions>(options =>
        {
            options.SetParameterPolicy<RegexInlineRouteConstraint>("regex");
        });
        //setup shark options
        Shark.SharkOption = option;
        //wire endpoints
        services.WireSharkEndpoint();
        //wire service lifelime
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);

        if (option.UseSwaggerDoc)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(SharkOption.SwaggerGenConfigure);
        }
    }
    internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
    {
        var opt = new UseSharkOptions();

        setupOptions?.Invoke(opt);

        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        InternalShark.ServiceProvider = InternalShark.ServiceScopeFactory.CreateScope().ServiceProvider;

        if(Shark.SharkOption.UseSwaggerDoc)
        {
            app.UseSwagger(UseSharkOptions.UseSwaggerConfigure);
            app.UseSwaggerUI();
        }
    }
}

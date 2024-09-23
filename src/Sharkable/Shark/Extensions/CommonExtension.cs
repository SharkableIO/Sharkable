using Microsoft.AspNetCore.Routing.Constraints;

namespace Sharkable;

public static class CommonExtension
{
    internal static void AddCommom(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        var option = new SharkOption();
        //invoke options
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
        //setup swagger gen
        services.SharkSwagger(setupOptions);
    }
    internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
    {
        var opt = new UseSharkOptions();
        //invoke options
        setupOptions?.Invoke(opt);
        //configure internal shark 
        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        InternalShark.ServiceProvider = InternalShark.ServiceScopeFactory.CreateScope().ServiceProvider;
        //setup swagger
        app.UseSharkSwagger(setupOptions);
    }
}

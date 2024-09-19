using Microsoft.AspNetCore.Routing.Constraints;

namespace Sharkable;

public static class CommonExtension
{
    internal static void AddCommom(this IServiceCollection services, Action<SharkOption>? configure = null)
    {
        var option = new SharkOption();
        if(configure != null)
        {
            configure(option);
            services.Configure(configure);
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

        if (option.UseOpenApi)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
    }
    internal static void UseCommon(this WebApplication app)
    {
        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        InternalShark.ServiceProvider = InternalShark.ServiceScopeFactory.CreateScope().ServiceProvider;

        if(Shark.SharkOption.ShowSwaggerDoc)
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }
    }
}

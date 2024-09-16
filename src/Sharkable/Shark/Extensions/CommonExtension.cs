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
        services.WireSharkEndpoint();
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
    }
    internal static void UseCommon(this WebApplication app)
    {
        InternalShark.ServiceProvider = app.Services;
        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();     
    }
}

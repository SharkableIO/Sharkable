namespace Sharkable;

public static class SharkExtension
{
    /// <summary>
    /// common service collection extensions for sharkable
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupOptions"></param>
    internal static void AddCommon(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        var option = new SharkOption();
        //invoke and setup options
        services.Configure<SharkOption>((opt) => 
        { 
            setupOptions?.Invoke(opt); 
            Shark.SharkOption = option; 
        });
        //wire endpoints
        services.WireSharkEndpoint();
        //wire service lifelime
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        //setup swagger gen
        services.SharkSwagger();
        //setup auto crud services
        services.AddAutoCrud();
    }
    /// <summary>
    /// common webapplication extensions for sharkable
    /// </summary>
    /// <param name="app"></param>
    /// <param name="setupOptions"></param>
    internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
    {
        var opt = new UseSharkOptions();
        //invoke options
        setupOptions?.Invoke(opt);
        Shark.UseSharkOptions = opt;
        //configure internal shark 
        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        InternalShark.ServiceProvider = InternalShark.ServiceScopeFactory.CreateScope().ServiceProvider;
        //setup swagger
        app.UseSharkSwagger();
    }
}

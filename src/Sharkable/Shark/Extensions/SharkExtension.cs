namespace Sharkable;

public static class SharkExtension
{
    internal static void AddCommon(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        var option = new SharkOption();
        //invoke options
        setupOptions?.Invoke(option);
        services.Configure<SharkOption>((opt) => { setupOptions?.Invoke(opt); });
        //setup shark options
        Shark.SharkOption = option;
        //wire endpoints
        services.WireSharkEndpoint();
        //wire service lifelime
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        //setup swagger gen
        services.SharkSwagger();
        //setup auto crud services
        services.AddAutoCrud();
    }
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

namespace  Microsoft.Extensions.DependencyInjection;

public static class SharkableExtension
{
    [RequiresDynamicCode("use Assembly[] method instead")]
    public static void AddShark(this IServiceCollection services, Action<SharkOption>? setupOption = null)
    {
        //set aot mode to false if use this method
        InternalShark.AotMode = false;
        //get asseblies
        Shark.SetAssebly(Utils.GetAssemblies());
        //set common extensions
        services.AddCommom(setupOption);
    }

    public static void AddShark(this IServiceCollection services, Assembly[]? assembly, Action<SharkOption>? setupOption = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        //set aot mode to true if use this method
        InternalShark.AotMode = true;
        //get assemblies
        Shark.SetAssebly(Utils.GetAssemblies(assembly));
        //set common extensions
        services.AddCommom(setupOption);
    }

    public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
    {
        app.UseCommon(setupOption);
        app.MapEndpoints();
    }

    public static void UseShark(this WebApplication app, Assembly[] assemblies)
    {
        app.UseCommon();
        app.MapEndpoints(assemblies);
    }
}

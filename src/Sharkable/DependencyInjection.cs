using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Sharkable;

namespace  Microsoft.Extensions.DependencyInjection;

public static class SharkableExtension
{
    [RequiresDynamicCode("use Assembly[] method instead")]
    public static void AddShark(this IServiceCollection services)
    {
        Shark.SetAssebly(Utils.GetAssemblies());
        services.AddCommom();
    }

    public static void AddShark(this IServiceCollection services, Assembly[] assembly)
    {
        Shark.SetAssebly(Utils.GetAssemblies(assembly));
        services.AddCommom();
    }

    public static void UseShark(this WebApplication app)
    {
        app.MapEndpoints();
    }

    public static void UseShark(this WebApplication app, Assembly[] assemblies)
    {
        app.MapEndpoints(assemblies);
    }

    internal static void AddCommom(this IServiceCollection services)
    {
        services.WireSharkEndpoint();
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        Shark.GetShark();
    }
    public static void ShowInterface<T>(this Assembly[] assemblies)
    {
        var types = Utils.GetRequiredInterface<T>(assemblies);

        types.MyForEach(x =>
        {
            Console.WriteLine(x.FullName);
        });
    }
}

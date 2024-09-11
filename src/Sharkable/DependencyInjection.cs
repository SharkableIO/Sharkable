using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Sharkable;

namespace  Microsoft.Extensions.DependencyInjection;

public static class SharkableExtension
{
    [RequiresDynamicCode("Add Assembly[] instead")]
    public static void AddShark(this IServiceCollection services)
    {
        var a = Utils.GetAssemblies();

        Shark.Assemblies = a;
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        Shark.GetShark();
    }

    public static void AddShark(this IServiceCollection services, Assembly[] assembly)
    {
        var a = Utils.GetAssemblies(assembly);
        
        Shark.Assemblies = a;
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        Shark.GetShark();
    }

    [RequiresDynamicCode("Add Assembly[] instead")]
    public static void UserShark(this WebApplication app)
    {
        app.MapEndpoints();
    }

    public static void UserShark(this WebApplication app, Assembly[] assemblies)
    {
        app.MapEndpoints(assemblies);
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

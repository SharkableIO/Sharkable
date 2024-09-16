using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Sharkable;

namespace  Microsoft.Extensions.DependencyInjection;

public static class SharkableExtension
{
    [RequiresDynamicCode("use Assembly[] method instead")]
    public static void AddShark(this IServiceCollection services, Action<SharkOption>? setupOption = null)
    {
        Shark.SetAssebly(Utils.GetAssemblies());
        services.AddCommom(setupOption);
    }

    public static void AddShark(this IServiceCollection services, Assembly[]? assembly, Action<SharkOption>? setupOption = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        Shark.SetAssebly(Utils.GetAssemblies(assembly));
        services.AddCommom(setupOption);
    }

    public static void UseShark(this WebApplication app)
    {
        app.UseCommon();
        app.MapEndpoints();
    }

    public static void UseShark(this WebApplication app, Assembly[] assemblies)
    {
        app.UseCommon();
        app.MapEndpoints(assemblies);
    }
}

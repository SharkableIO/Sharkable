
namespace Sharkable;

/// <summary>Utility methods for assembly scanning, string formatting, and reflection helpers.</summary>
public partial class Utils
{
    internal static Assembly[]? GetAssemblies(params Assembly[] assembly)
    {
        if (assembly.Length > 0)
            return assembly;

        var lst = new List<Assembly>();

        var entry = Assembly.GetEntryAssembly();
        if (entry != null)
            lst.Add(entry);

        var refAssemblies = entry?.GetReferencedAssemblies();

        refAssemblies.MyForEach(refAssemblie =>
        {
            lst.Add(Assembly.Load(refAssemblie));
        });
        return [.. lst];
    }

    internal static Type[]? GetRequiredInterface<I>(this Assembly[]? assemblies)
    {
        if (assemblies == null)
            return null;

        var type = typeof(I);
        var typeList = new List<Type>();
        foreach (var a in assemblies)
        {
            var matchingTypes = a.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && t.GetInterfaces().Any(x => x == type));
            typeList.AddRange(matchingTypes);
        }

        return typeList.Count == 0 ? null : [.. typeList];
    }

    internal static void SetupModules(Assembly[]? assemblies, ref IServiceCollection services)
    {
        if (assemblies == null)
            return;
        foreach(var implType in assemblies.SelectMany(a => a.GetTypes().Where(t => !t.IsAbstract && typeof(ISingleton).IsAssignableFrom(t))))
        {
            var obj = Activator.CreateInstance(implType);
            services.AddSingleton(implType);
            Utils.WriteDebug($"type is {implType.Name}");
        }
    }
}
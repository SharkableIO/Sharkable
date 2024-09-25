
namespace Sharkable;

public partial class Utils
{
    internal static Assembly[]? GetAssemblies(params Assembly[] assembly)
    {
        if (assembly.Length > 0)
            return assembly;

        var lst = new List<Assembly?>();

        var entry = Assembly.GetEntryAssembly();
        lst.AddNonNull(entry);

        var refAssemblies = entry?.GetReferencedAssemblies();

        refAssemblies.MyForEach(refAssemblie =>
        {
            lst.AddNonNull(Assembly.Load(refAssemblie));
        });
        return [.. lst];
    }

    internal static Type[]? GetRequiredInterface<I>(this Assembly[]? assemblies)
    {
        if (assemblies == null)
            return null;

        var lst = new List<I>();
        var type = typeof(I);
        var typeList = new List<Type>();
        foreach(var a in assemblies)
        {
            var t = a.GetType();
            var myT = t.GetInterfaces().Where(x => x == type);
            typeList.Union(myT);
        }
        
        return [.. typeList];
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
using System.Reflection;

namespace Sharkable;

public class AssemblyContext
{
    public static Assembly[]? Assemblies => Instance?.InternalAssemblies;

    public Assembly[]? InternalAssemblies { get; internal set; }

    internal static AssemblyContext? Instance;

    private static readonly object locker = new();

    internal static void SetAssembly(Assembly[]? assemblies)
    {
        if(Instance != null)
            Instance.InternalAssemblies = assemblies;
    }
    public static AssemblyContext GetAssemblyContext(Assembly[]? assemblies)
    {
        lock(locker)
        {
            Instance ??= new AssemblyContext(assemblies);
            return Instance;
        }
    }

    public static AssemblyContext GetAssemblyContext()
    {
        lock(locker)
        {
            Instance ??= new AssemblyContext(Assemblies);
            return Instance;
        }
    }

    public AssemblyContext()
    {
        
    }
    public AssemblyContext(Assembly[]? assemblies)
    {
        InternalAssemblies = assemblies;
    }
}

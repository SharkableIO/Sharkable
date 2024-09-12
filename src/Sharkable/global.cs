using System.Reflection;

namespace Sharkable;

public partial class Shark
{
    private static Shark instance = null!;

    static readonly object condition = new();
    public static Assembly[]? Assemblies { get; private set; }

    public static Shark GetShark()
    {
        lock(condition)
        {
            instance ??= new Shark();
            return instance;
        }
    }

    public static void SetAssebly(Assembly[]? assemblies)
    {
        Assemblies = assemblies;
    }

    public static string? ApiPrefix { get; set; } = "/api";
}
using System.Reflection;

namespace Sharkable;

public partial class Shark
{
    private static Shark instance = null!;

    static readonly object condition = new();
    public static Assembly[]? Assemblies { get; internal set; }

    public static Shark GetShark()
    {
        lock(condition)
        {
            instance ??= new Shark();
            return instance;
        }
    }

    public static string? ApiPrefix { get; set; } = "/api";
}
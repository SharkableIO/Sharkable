using System.Reflection;

namespace Sharkable.Sample;

public static class SharkSample
{
    public static void AddSharkServices(this IServiceCollection services)
    {
        //services.AddSingleton<IMonitor, Monitor>();
    }
    public static Assembly[] GetStaticAssemblies()
    {
        var lst = new List<Assembly>
        {
            typeof(Program).Assembly
        };
        var arr = lst.ToArray();
        arr.MyForEach(x =>
        {
            x.DefinedTypes.MyForEach(a => {
                Console.WriteLine(a.Name);
            });
        });
        return arr;
    }
}
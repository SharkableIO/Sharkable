 
namespace Sharkable;

internal static class AutoCrudExtension
{
    internal static IServiceCollection AddAutoCrud(this IServiceCollection services)
    {
        //only proceed when AutoCrud is configured
        var sqlSugarOptions = Shark.SharkOption.SqlSugarOptionsConfigure;
        if (sqlSugarOptions == null)
            return services;

        //get auto crud sqlsugar extensions
        //todo: will use regex extension to get all Sharkable.AutoCrud.* if more aot supported orms are comming out;
        var assembly = Shark.Assemblies?.FirstOrDefault(x=>x.GetName().Name!.Equals("Sharkable.AutoCrud.SqlSugar"));

        // Fallback: the assembly may not be loaded yet (lazy NuGet loading).
        // Trigger explicit load so the extension method is discoverable.
        if (assembly == null)
        {
            try
            {
                assembly = Assembly.Load("Sharkable.AutoCrud.SqlSugar");
            }
            catch
            {
                Utils.WriteDebug("no auto crud generation service added (assembly not found).");
                return services;
            }
        }

        if(assembly != null)
        {
            var crudTypes = assembly.GetType("Sharkable.AutoCrud.SqlSugar.AutoCrudExtension");
            var method = crudTypes?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "AddSqlSugar");

            if (method?.Invoke(null, [services, sqlSugarOptions]) is IServiceCollection s)
            {
                Utils.WriteDebug("auto crud generation added.");
                return s;
            }
        }
        Utils.WriteDebug("no auto crud generation service added.");
        return services;
    }
}

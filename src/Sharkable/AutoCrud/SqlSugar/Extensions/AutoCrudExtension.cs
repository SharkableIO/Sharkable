
namespace Sharkable;

internal static class AutoCrudExtension
{
    internal static IServiceCollection AddAutoCrud(this IServiceCollection services)
    {
        //get auto crud sqlsugar extensions
        //todo: will use regex extension to get all Sharkable.AutoCrud.* if more aot supported orms are comming out;
        var assembly = Shark.Assemblies?.FirstOrDefault(x=>x.GetName().Name!.Equals("Sharkable.AutoCrud.SqlSugar"));
        if(assembly != null)
        {
            var crudTypes = assembly.GetType("Sharkable.AutoCrud.SqlSugar.AutoCrudExtension");
            var method = crudTypes?.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "AddSqlSugar");
            
            if(method != null)
            {
                if (method.Invoke(null, [services, SharkOption.SqlSugarOptionsConfigure]) is IServiceCollection s) 
                {
                    Utils.WriteDebug("auto crud generation added.");
                    return s;
                }
            }
        }
        Utils.WriteDebug("no auto crud generation service added.");
        return services;
    }
}

using System;

namespace Sharkable.AssemlyContext.Extensions;

public static class AssemblyExtension
{
    public static IList<Type>? GetAssemblyTypeList<T>(this Assembly[]? assemblies)
    {
        return assemblies.GetAssemblyTypeList(typeof(T));
    }
    public static IList<Type>? GetAssemblyTypeList(this Assembly[]? assemblies, Type? type = null)
    {
        //todo 
        return null;
    }
}

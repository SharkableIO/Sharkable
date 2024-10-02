using System.Linq.Expressions;

namespace Sharkable;

public class Reflector
{
    internal static IEnumerable<MethodInfo>? GetMethods<T>(T type, 
        BindingFlags flags = BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly, Func<MethodInfo, int, bool>? predicate = null)
    {
        return predicate is null ? 
            type?.GetType().GetMethods(bindingAttr: flags) : 
            type?.GetType().GetMethods(bindingAttr: flags).Where(predicate);
    }
    /// <summary>
    /// get an instance of the unified result class object
    /// </summary>
    /// <param name="data">data which need to be assigned</param>
    /// <param name="type">type of the given data</param>
    /// <returns></returns>
    internal static object? GetUnifiedResult(object? data, Type type)
    {
        var genericType = typeof(UnifiedResult<>);
        var specificType = genericType.MakeGenericType(type);
        var instance = Activator.CreateInstance(specificType);
        var propertyInfo = specificType.GetProperty("Data");
        propertyInfo?.SetValue(instance, data);
        return instance;
    }

    internal static Delegate? GetMethodDelegate(MethodInfo methodInfo, object? instance)
    {
        if (instance == null)
        {
            return null;
        }
        
        var parameters = methodInfo.GetParameters()
            .Select(p => p.ParameterType)
            .ToArray();
        
        if (methodInfo.ReturnType == typeof(Task) || (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
        {
            var delegateType = Expression.GetDelegateType(parameters.Concat<Type>(new[] { methodInfo.ReturnType }).ToArray());
            return methodInfo.CreateDelegate(delegateType, instance);
        } 
        if (methodInfo.ReturnType == typeof(void))
        {
            var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { typeof(void) }).ToArray());
            return  methodInfo.CreateDelegate(delegateType, instance);
            
        }

        if (!methodInfo.ReturnType.IsGenericType) return null;
        {
            var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { methodInfo.ReturnType }).ToArray());
            return methodInfo.CreateDelegate(delegateType, instance);
        }
    }
}
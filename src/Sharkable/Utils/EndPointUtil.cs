using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Sharkable.Extensions;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;

namespace Sharkable;

internal static class EndPointUtil
{
    [RequiresDynamicCode("Add Assembly[] instead")]
    public static void MapEndpoints(this WebApplication? app)
    {
        var assemblies = Utils.GetAssemblies(); 

        AddEndpoints(assemblies,  app);
    }

    public static void MapEndpoints(this WebApplication? app, Assembly[] assemblies)
    {
        AddEndpoints(assemblies,  app);
    }

    private static void AddEndpoints(Assembly[]? assemblies, WebApplication? app)
    {
        ArgumentNullException.ThrowIfNull(app);
        ArgumentNullException.ThrowIfNull(assemblies);

        var lst = GetEndpoints(ref assemblies);
        lst.MyForEach(a =>
        {
            var group = app.MapGroup(a.Item1!);
            a.Item2.MyForEach(e =>
            {
                switch(e.Item2)
                {
                    case SharkHttpMethod.GET:
                        group.MapGet(e.Item1!, e.Item3);
                        break;
                    case SharkHttpMethod.POST:
                        group.MapPost(e.Item1!, e.Item3);
                        break;
                    case SharkHttpMethod.PUT:
                        group.MapPut(e.Item1!, e.Item3);
                        break;
                    case SharkHttpMethod.DELETE:
                        group.MapDelete(e.Item1!, e.Item3);
                        break;
                    case SharkHttpMethod.PATCH:
                        group.MapPatch(e.Item1!, e.Item3);
                        break;
                    default:
                        break;
                }
            });
        });
    }

    public static List<Tuple<string?, List<Tuple<string?, SharkHttpMethod, Delegate>>>>? GetEndpoints(ref Assembly[]? assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        var lst = new List<Tuple<string?, List<Tuple<string?, SharkHttpMethod, Delegate>>>>();
        assemblies.MyForEach(a =>
        {
            a.GetTypes().MyForEach(t =>
            {
                var endpointAttrubute = t.GetCustomAttributes<SharkEndpointAttribute>()
                    .FirstOrDefault();
                var methods = new List<Tuple<string?, SharkHttpMethod, Delegate>>();

                if (endpointAttrubute == null)
                    return;

                endpointAttrubute.Group ??= t.Name.FormatAsGroupName();

                var taggedMethods = t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                   .Where(x => x.CustomAttributes
                       .Any(x => x.AttributeType == typeof(SharkMethodAttribute)))
                   .ToList();

                if (taggedMethods.Count == 0)
                    return;

                var instance = Activator.CreateInstance(t);
                taggedMethods.MyForEach(methodInfo =>
                {
                    //methods.Add(new Tuple<string?, SharkHttpMethod, Delegate>(methodAttribute.AddressName, methodAttribute.Method, methodDelegate));
                    var methodAttribute = methodInfo.GetCustomAttribute<SharkMethodAttribute>();

                    if (methodAttribute == null)
                        return;

                    //setup route address
                    if (string.IsNullOrWhiteSpace(methodAttribute.AddressName))
                        methodAttribute.AddressName ??= methodInfo.Name;

                    var parameters = methodInfo.GetParameters()
                                   .Select(p => p.ParameterType)
                                   .ToArray();

                    if (methodInfo.ReturnType == typeof(Task) || (methodInfo.ReturnType.IsGenericType && methodInfo.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)))
                    {
                        var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { methodInfo.ReturnType }).ToArray());
                        var del = methodInfo.CreateDelegate(delegateType, instance);
                        methods.Add(new Tuple<string?, SharkHttpMethod, Delegate>(methodAttribute.AddressName, methodAttribute.Method, del));
                    }
                    else if (methodInfo.ReturnType == typeof(void))
                    {
                        var delegateType = Expression.GetDelegateType(parameters.Concat(new[] { typeof(void) }).ToArray());
                        var del = methodInfo.CreateDelegate(delegateType, instance);
                        methods.Add(new Tuple<string?, SharkHttpMethod, Delegate>(methodAttribute.AddressName, methodAttribute.Method, del));
                    }
                });

                lst.Add(new Tuple<string?, List<Tuple<string?, SharkHttpMethod, Delegate>>>(endpointAttrubute?.Group, methods));
            });
        });

        return lst;
    }
}

using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Options;

namespace Sharkable;

internal static class SharkEndPointExtension
{
    public static void MapEndpoints(this WebApplication? app)
    {
        app.MapSharkEndpoints();
        
        //map shark endpoint attributes only when i non aot mode
        if (!Shark.SharkOption.AotMode)
        {
            MapAttributeEndpoints(Shark.Assemblies, app);
        }
    }

    internal static WebApplication MapSharkEndpoints(this WebApplication? app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var endpointServices = app.Services.GetServices<ISharkEndpoint>();
        var options = app.Services.GetService<IOptions<SharkOption>>();

        ArgumentNullException.ThrowIfNull(options);

        endpointServices.MyForEach(e =>
        {
            SharkEndpoint sharkEndpoint;

            if (e is SharkEndpoint endpoint)
            {
                sharkEndpoint = endpoint;
                sharkEndpoint.BuildAction = endpoint.AddRoutes;
            }
            else
            {
                sharkEndpoint = CreateSharkEndpoint(e);
            }

            if (string.IsNullOrWhiteSpace(sharkEndpoint.apiPrefix))
                sharkEndpoint.apiPrefix = options.Value.ApiPrefix;

            string groupName = null!;

            if (sharkEndpoint.groupName != null)
            {
                groupName = sharkEndpoint.groupName.GetCaseFormat(options.Value.Format)!;
                sharkEndpoint.baseApiPath = $"{sharkEndpoint.apiPrefix}/{groupName}";
            }
            else
            {
                groupName = string.Empty;
            }

            if (string.IsNullOrWhiteSpace(sharkEndpoint.apiPrefix))
            {
                sharkEndpoint.BuildAction?.Invoke(app);
            }
            else
            {
                sharkEndpoint.baseApiPath = string.IsNullOrWhiteSpace(groupName) ? 
                    sharkEndpoint.apiPrefix : $"{sharkEndpoint.apiPrefix}/{groupName}";

                var group = app.MapGroup(sharkEndpoint.baseApiPath).WithDisplayName(groupName);
                sharkEndpoint.BuildAction?.Invoke(group);
            }

        });
        return app;
    }

    internal static void WireSharkEndpoint(this IServiceCollection services)
    {
        var endpoints = GetSharkEndpint(Shark.Assemblies);

        endpoints.MyForEach(e =>
        {
            Utils.WriteDebug($"wiring {e.FullName}");
            services.AddSingleton(typeof(ISharkEndpoint), e);
        });
    }

    private static List<Type>? GetSharkEndpint(Assembly[]? assemblies)
    {
        ArgumentNullException.ThrowIfNull(assemblies);

        var assemblyList = assemblies.ToList();

        if (assemblyList.Count == 0)
            return null;

        var endpoints = assemblyList.Select(x => x.GetTypes()
            .Where(i => i
                .GetInterfaces()
                .Any(type => type == typeof(ISharkEndpoint))).ToList()).ToList();
        if (endpoints.Count == 0)
            return null;

        var lst = new List<Type>();

        endpoints.MyForEach(lst.AddRange);

        return lst;
    }

    private static SharkEndpoint CreateSharkEndpoint<T>(T shark, string? apiPrefix = "api") where T : ISharkEndpoint
    {
        var sharkEndpointType = typeof(SharkEndpoint);
        var sharkAttribute = shark.GetType().GetCustomAttribute<SharkEndpointAttribute>();
        var instance = (SharkEndpoint)Activator.CreateInstance(sharkEndpointType, nonPublic: true)!;

        if (sharkAttribute != null)
        {
            instance.groupName = sharkAttribute.Group;
            instance.version = sharkAttribute.Version;
            if (string.IsNullOrWhiteSpace(sharkAttribute.ApiPrefix))
                instance.addPrefix = false;
        }
        else
        {
            // Directly set fields using known properties or methods
            instance.groupName = shark.GetType().Name.FormatAsGroupName();
            instance.addPrefix = !string.IsNullOrWhiteSpace(apiPrefix);
        }

        instance.apiPrefix = apiPrefix;
        // Assign the delegate
        instance.BuildAction = shark.AddRoutes;

        return instance;
    }
    private static JsonSerializerOptions? ResolveSerializerOptions(HttpContext ctx)
    {
        // Attempt to resolve options from DI then fallback to default options
        return ctx.RequestServices.GetService<IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions>>()?.Value?.SerializerOptions;
    }
    [RequiresDynamicCode("https://github.com/SharkableIO/Sharkable/issues/53")]
    private static void MapAttributeEndpoints(
        Assembly[]? assemblies, WebApplication? app)
    {
        ArgumentNullException.ThrowIfNull(assemblies);
        ArgumentNullException.ThrowIfNull(app);

        assemblies.MyForEach(a =>
        {
            a.GetTypes().MyForEach(t =>
            {
                var endpointAttribute = t.GetCustomAttributes<SharkEndpointAttribute>()
                    .FirstOrDefault();
                
                if (endpointAttribute == null)
                    return;

                endpointAttribute.Group ??= t.Name;
                endpointAttribute.Group = endpointAttribute.Group
                    .FormatAsGroupName()
                    .GetCaseFormat(Shark.SharkOption.Format)
                    .GetVersionFormat();
                
                var taggedMethods = t
                    .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                    .Where(x => !x.GetCustomAttributes<NonActionAttribute>().Any())
                    .ToList();

                if (taggedMethods.Count == 0)
                    return;
                var factory = app.Services.GetService<IDependencyReflectorFactory>();
                var instance = factory?.CreateInstance(t) ??
                               throw new Exception($"error when creating an instance of {t.Name}");
                    
                var group = app.MapGroup(endpointAttribute.Group!);

                taggedMethods.MyForEach(methodInfo =>
                {
                    //methods.Add(new Tuple<string?, SharkHttpMethod, Delegate>(methodAttribute.AddressName, methodAttribute.Method, methodDelegate));
                    var attribute = methodInfo.GetCustomAttribute<SharkMethodAttribute>();
                    var methodAttribute = attribute ?? new SharkMethodAttribute();

                    //setup route address
                    if (string.IsNullOrWhiteSpace(methodAttribute.Pattern))
                        methodAttribute.Pattern ??= methodInfo.Name;

                    methodAttribute.Pattern = methodAttribute.Pattern
                        .GetCaseFormat(Shark.SharkOption.Format)
                        .GetVersionFormat()!;

                    var methodDelegate = instance.GetDelegate(methodInfo);
                    if (methodDelegate == null)
                        return;

                    group.MapMethods(methodAttribute.Pattern!, [methodAttribute.Method.ToString()], methodDelegate);
                });
            });
        });
    }
}

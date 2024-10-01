using Microsoft.Extensions.Options;


namespace Sharkable;

internal static class SharkEndPointExtension
{
    public static void MapEndpoints(this WebApplication? app)
    {
        app.MapSharkEndpoints();
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

            if (sharkEndpoint.grouName != null)
            {
                groupName = sharkEndpoint.grouName.GetCaseFormat(options.Value.Format)!;
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
                if (string.IsNullOrWhiteSpace(groupName))
                {
                    sharkEndpoint.baseApiPath = sharkEndpoint.apiPrefix;
                }
                else
                {
                    sharkEndpoint.baseApiPath = $"{sharkEndpoint.apiPrefix}/{groupName}";
                }
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

        var alist = assemblies.ToList();

        if (alist.Count == 0)
            return null;

        var endpoints = alist.Select(x => x.GetTypes()
                .Where(i => i.GetInterfaces()
                        .Where(x => x == typeof(ISharkEndpoint))
                        .Any()).ToList()).ToList();
        if (endpoints.Count == 0)
            return null;

        var lst = new List<Type>();

        endpoints.MyForEach(lst.AddRange);

        return lst;
    }

    public static SharkEndpoint CreateSharkEndpoint<T>(T shark, string? apiPrefix = "api") where T : ISharkEndpoint
    {
        var sharkEndpointType = typeof(SharkEndpoint);
        var sharkAttribute = shark.GetType().GetCustomAttribute<SharkEndpointAttribute>();
        var instance = (SharkEndpoint)Activator.CreateInstance(sharkEndpointType, nonPublic: true)!;
        
        if (sharkAttribute != null)
        {
            instance.grouName = sharkAttribute.Group;
            instance.version = sharkAttribute.Version;
            if (string.IsNullOrWhiteSpace(sharkAttribute.ApiPrefix))
                instance.addPrefix = false;
        }
        else
        {
            // Directly set fields using known properties or methods
            instance.grouName = shark.GetType().Name.FormatAsGroupName();
            instance.addPrefix = !string.IsNullOrWhiteSpace(apiPrefix);
        }
        instance.apiPrefix = apiPrefix;
        // Assign the delegate
        instance.BuildAction = shark.AddRoutes;

        return instance;
    }
}

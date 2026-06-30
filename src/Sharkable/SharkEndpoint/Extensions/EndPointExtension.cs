#pragma warning disable CS0618 // Internal use of legacy attribute-based endpoint system

using System.Linq;
using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
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

        // Phase 1: Collect all SharkEndpoint instances with metadata
        var collected = new List<(SharkEndpoint endpoint, Type classType)>();
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

            var groupAttr = e.GetType().GetCustomAttribute<EndpointGroupAttribute>();
            if (groupAttr != null)
            {
                sharkEndpoint.groupName = groupAttr.Name;
            }

            var versionAttr = e.GetType().GetCustomAttribute<SharkVersionAttribute>();
            if (versionAttr != null)
            {
                sharkEndpoint.version = versionAttr.Version;
            }

            if (string.IsNullOrWhiteSpace(sharkEndpoint.apiPrefix))
                sharkEndpoint.apiPrefix = options.Value.ApiPrefix;

            collected.Add((sharkEndpoint, e.GetType()));
        });

        // Phase 2: Group by (version, groupName) tuple
        var grouped = new Dictionary<string, List<(SharkEndpoint, Type)>>();
        collected.MyForEach(item =>
        {
            var version = item.endpoint.version?.GetCaseFormat(options.Value.Format);
            var groupName = item.endpoint.groupName?.GetCaseFormat(options.Value.Format) ?? string.Empty;
            var key = string.IsNullOrWhiteSpace(version) ? groupName : $"{version}_{groupName}";
            if (!grouped.ContainsKey(key))
                grouped[key] = [];
            grouped[key].Add(item);
        });

        // Phase 3: One MapGroup per unique group name
        foreach (var (groupName, endpoints) in grouped)
        {
            var first = endpoints.First().Item1;

            if (string.IsNullOrWhiteSpace(first.apiPrefix))
            {
                endpoints.MyForEach(ep => ep.Item1.BuildAction?.Invoke(app));
                continue;
            }

            var version = first.version?.GetCaseFormat(options.Value.Format);
            var basePath = first.apiPrefix;
            if (!string.IsNullOrWhiteSpace(version))
                basePath = $"{basePath}/{version}";
            var groupNameForUrl = first.groupName?.GetCaseFormat(options.Value.Format) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(groupNameForUrl))
                basePath = $"{basePath}/{groupNameForUrl}";

            var group = app.MapGroup(basePath).WithDisplayName(groupName);

            // Shared filters (once per group)
            var autoWrap = Shark.SharkOption.EnableAutoWrap || (Shark.UseSharkOptions?.EnableAutoWrap ?? false);
            if (autoWrap)
                group.AddEndpointFilter<UnifiedResultWrapFilter>();

            if (Shark.SharkOption.EnableValidation)
                group.AddEndpointFilter<ValidationFilter>();

            if (Shark.SharkOption.ApiKeys?.Length > 0 && Shark.SharkOption.AuthorizationInterceptorFactory == null)
                group.AddEndpointFilter<ApiKeyFilter>();

            if (Shark.SharkOption.AuthorizationInterceptorFactory != null)
                group.AddEndpointFilter<AuthorizationInterceptorFilter>();

            // Resolve tags + class-level metadata from all endpoint types in this group
            var tags = ResolveGroupTags(endpoints, groupName);
            var summary = ResolveGroupSummary(endpoints);
            var description = ResolveGroupDescription(endpoints);
            var responseTypes = ResolveGroupResponseTypes(endpoints);
            var isDeprecated = ResolveGroupIsDeprecated(endpoints);

            // Auto-Tags + OperationId + class-level metadata via Add() convention
            var capturedGroupName = !string.IsNullOrWhiteSpace(version) ? $"{version}_{groupName}" : groupName;
            var capturedBasePath = basePath;
            var capturedTags = tags;
            var capturedSummary = summary;
            var capturedDescription = description;
            var capturedResponseTypes = responseTypes;
            var capturedIsDeprecated = isDeprecated;
            ((IEndpointConventionBuilder)group).Add(builder =>
            {
                if (!builder.Metadata.Any(m => m is ITagsMetadata) && capturedTags.Count != 0)
                    builder.Metadata.Add(new TagsAttribute([.. capturedTags]));

                if (capturedSummary != null && !builder.Metadata.Any(m => m is EndpointSummaryAttribute))
                    builder.Metadata.Add(new EndpointSummaryAttribute(capturedSummary));

                if (capturedDescription != null && !builder.Metadata.Any(m => m is EndpointDescriptionAttribute))
                    builder.Metadata.Add(new EndpointDescriptionAttribute(capturedDescription));

                foreach (var rt in capturedResponseTypes)
                {
                    if (!builder.Metadata.Any(m => m is IProducesResponseTypeMetadata pm && pm.StatusCode == rt.StatusCode))
                        builder.Metadata.Add(rt);
                }

                if (capturedIsDeprecated && !builder.Metadata.Any(m => m is ObsoleteAttribute))
                    builder.Metadata.Add(new ObsoleteAttribute("This endpoint is deprecated."));

                if (!builder.Metadata.Any(m => m is EndpointNameMetadata))
                {
                    var routePattern = (builder as RouteEndpointBuilder)?.RoutePattern?.RawText ?? string.Empty;
                    var httpMethod = builder.Metadata
                        .OfType<HttpMethodMetadata>()
                        .FirstOrDefault()?.HttpMethods?.FirstOrDefault() ?? "Unknown";

                    var relativePath = routePattern;
                    if (!string.IsNullOrEmpty(capturedBasePath) && relativePath.StartsWith(capturedBasePath + "/"))
                        relativePath = relativePath[(capturedBasePath.Length + 1)..];

                    var opId = $"{capturedGroupName}_{httpMethod}_{relativePath}"
                        .Replace('/', '_')
                        .Replace('-', '_')
                        .Replace(' ', '_');
                    builder.Metadata.Add(new EndpointNameMetadata(opId));
                }
            });

            // AutoCrud: generate routes before user's AddRoutes so overrides take precedence
            var crudGenerator = app.Services.GetService<IAutoCrudGenerator>();
            if (crudGenerator != null)
            {
                foreach (var (_, classType) in endpoints)
                {
                    var entityInterface = classType.GetInterfaces()
                        .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAutoCrudEntity<>));
                    if (entityInterface == null) continue;

                    var entityType = entityInterface.GetGenericArguments()[0];
                    var operations = CrudOperations.All;
                    try
                    {
                        if (Activator.CreateInstance(classType) is IAutoCrudEntityMarker marker)
                            operations = marker.GetOperations();
                    }
                    catch { }

                    crudGenerator.GenerateRoutes(group, entityType, classType, operations);
                }
            }

            // Call AddRoutes for each endpoint in this group
            endpoints.MyForEach(ep => ep.Item1.BuildAction?.Invoke(group));
        }

        // health check endpoint
        if (Shark.SharkOption.EnableHealthChecks)
        {
            HealthCheckEndpoint.Map(app);
        }

        return app;
    }

    private static List<string> ResolveGroupTags(List<(SharkEndpoint, Type)> endpoints, string defaultTag)
    {
        var tags = endpoints
            .SelectMany(ep => ep.Item2.GetCustomAttributes<SharkTagAttribute>())
            .Select(attr => attr.Tag)
            .Distinct()
            .ToList();

        if (tags.Count == 0)
            tags.Add(defaultTag);

        return tags;
    }

    private static string? ResolveGroupSummary(List<(SharkEndpoint, Type)> endpoints)
    {
        return endpoints
            .Select(ep => ep.Item2.GetCustomAttribute<SharkDescriptionAttribute>()?.Summary)
            .FirstOrDefault(s => s != null);
    }

    private static string? ResolveGroupDescription(List<(SharkEndpoint, Type)> endpoints)
    {
        return endpoints
            .Select(ep => ep.Item2.GetCustomAttribute<SharkDescriptionAttribute>()?.Description)
            .FirstOrDefault(d => d != null);
    }

    private static List<IProducesResponseTypeMetadata> ResolveGroupResponseTypes(List<(SharkEndpoint, Type)> endpoints)
    {
        return endpoints
            .SelectMany(ep => ep.Item2.GetCustomAttributes<SharkResponseTypeAttribute>())
            .Select(attr => (IProducesResponseTypeMetadata)new SharkResponseMetadata
            {
                StatusCode = attr.StatusCode,
                Type = attr.ResponseType,
                ContentTypes = ["application/json"],
            })
            .ToList();
    }

    private static bool ResolveGroupIsDeprecated(List<(SharkEndpoint, Type)> endpoints)
    {
        return endpoints.Any(ep =>
            ep.Item2.GetCustomAttribute<SharkDeprecatedAttribute>() != null);
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
        var sharkAttribute = shark.GetType().GetCustomAttribute<SharkEndpointAttribute>();
        var instance = new SharkEndpoint();

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

        var endpointGroupAttr = shark.GetType().GetCustomAttribute<EndpointGroupAttribute>();
        if (endpointGroupAttr != null)
        {
            instance.groupName = endpointGroupAttr.Name;
            instance.addPrefix = !string.IsNullOrWhiteSpace(apiPrefix);
        }

        var versionAttr = shark.GetType().GetCustomAttribute<SharkVersionAttribute>();
        if (versionAttr != null)
        {
            instance.version = versionAttr.Version;
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
                               throw new InvalidOperationException($"error when creating an instance of {t.Name}");
                    
                var group = app.MapGroup(endpointAttribute.Group!);

                // Resolve tags for this endpoint class
                var tagAttrs = t.GetCustomAttributes<SharkTagAttribute>();
                var tags = tagAttrs.Any()
                    ? tagAttrs.Select(a => a.Tag).ToArray()
                    : [endpointAttribute.Group!];
                ((RouteGroupBuilder)group).WithTags(tags);

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

                    group.MapMethods(methodAttribute.Pattern!, [methodAttribute.Method.ToString()], methodDelegate)
                         .WithMetadata(new EndpointNameMetadata($"{t.Name}_{methodInfo.Name}"));
                });
            });
        });
    }
}

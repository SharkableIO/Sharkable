
namespace Sharkable;

/// <summary>
/// scan for service extensions
/// </summary>
internal static class AttributeBasedServiceCollectionExtensions
{
    /// <summary>
    /// scan for service
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="assemblys"></param>
    internal static void AddServicesWithAttributeOfTypeFromAssembly(this IServiceCollection serviceCollection, Assembly[]? assemblys)
    {
        serviceCollection.AddServicesWithAttributeOfType(Shark.Assemblies);
        serviceCollection.AddServicesWithInterfaceMarker(Shark.Assemblies);
    }
    /// <summary>
    /// scan for service
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="assemblys"></param>
    internal static void AddServicesWithAttributeOfType(this IServiceCollection serviceCollection, params Assembly[]? assemblys)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        ArgumentNullException.ThrowIfNull(assemblys);

        AddServicesWithAttributeOfType<ScopedServiceAttribute>(serviceCollection, assemblys.ToList());
        AddServicesWithAttributeOfType<TransientServiceAttribute>(serviceCollection, assemblys.ToList());
        AddServicesWithAttributeOfType<SingletonServiceAttribute>(serviceCollection, assemblys.ToList());
    }
    /// <summary>
    ///scan for service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceCollection"></param>
    /// <param name="assemblys"></param>
    internal static void AddServicesWithAttributeOfType<T>(this IServiceCollection serviceCollection, params Assembly[]? assemblys)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        ArgumentNullException.ThrowIfNull(assemblys);

        AddServicesWithAttributeOfType<T>(serviceCollection, assemblys.ToList());
    }
    /// <summary>
    /// scan first，if not registered then register
    /// </summary>
    /// <param name="serviceCollection"></param>
    /// <param name="serviceType"></param>
    /// <param name="implementationType"></param>
    /// <param name="lifetime"></param>
    internal static void TryAdd(this IServiceCollection serviceCollection, Type serviceType, Type implementationType, ServiceLifetime lifetime)
    {
        bool isAlreadyRegistered = serviceCollection.Any(s => s.ServiceType == serviceType && s.ImplementationType == implementationType);
        if (!isAlreadyRegistered)
        {
            serviceCollection.Add(new ServiceDescriptor(serviceType, implementationType, lifetime));
        }
    }
    /// <summary>
    /// scan for service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceCollection"></param>
    /// <param name="assembliesToBeScanned"></param>
    internal static void AddServicesWithAttributeOfType<T>(this IServiceCollection serviceCollection, IEnumerable<Assembly> assembliesToBeScanned)
    {
        ArgumentNullException.ThrowIfNull(serviceCollection);

        ArgumentNullException.ThrowIfNull(assembliesToBeScanned);

        if (!assembliesToBeScanned.Any())
        {
            throw new ArgumentException($"The {assembliesToBeScanned} is empty.", nameof(assembliesToBeScanned));
        }

        ServiceLifetime lifetime = ServiceLifetime.Scoped;

        lifetime = typeof(T).Name switch
        {
            nameof(TransientServiceAttribute) => ServiceLifetime.Transient,
            nameof(ScopedServiceAttribute) => ServiceLifetime.Scoped,
            nameof(SingletonServiceAttribute) => ServiceLifetime.Singleton,
            _ => throw new ArgumentException($"The type {typeof(T).Name} is not a valid type in this context."),
        };
        
        List<Type> servicesToBeRegistered = assembliesToBeScanned
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => type.IsDefined(typeof(T), false))
            .ToList();

        var attrType = typeof(T);
        foreach (Type serviceType in servicesToBeRegistered)
        {
            List<Type> implementations = [];

            // Check if this is an eager singleton
            var attr = serviceType.GetCustomAttribute(attrType) as SingletonServiceAttribute;
            if (attr is { Eager: true } && typeof(T) == typeof(SingletonServiceAttribute))
            {
                InternalShark.EagerSingletonTypes.Add(serviceType);
            }

            if (serviceType.IsGenericType && serviceType.IsGenericTypeDefinition)
            {
                implementations = assembliesToBeScanned.SelectMany(a => a.GetTypes())
                .Where(type => type.IsGenericType && type.IsClass && type.GetInterfaces()
                .Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == serviceType.GetGenericTypeDefinition()))
                .ToList();
            }
            else
            {
                implementations = assembliesToBeScanned.SelectMany(a => a.GetTypes())
                .Where(type => serviceType.IsAssignableFrom(type) && type.IsClass).ToList();
            }

            if (implementations.Count != 0)
            {
                foreach (Type implementation in implementations)
                {
                    Type[] ts = implementation.GetInterfaces();
                    if (ts?.Length > 0)
                    {
                        foreach (Type type in implementation.GetInterfaces())
                        {
                            Utils.WriteDebug("injecting service:" + type.Name + "," + implementation.Name);
                            serviceCollection.TryAdd(type, implementation, lifetime);
                        }
                    }

                    Type? baseType = implementation.BaseType;
                    if (baseType!=null && !baseType.Equals(typeof(Object)))
                    {
                        Utils.WriteDebug("injecting service:" + baseType.Name + "," + implementation.Name);
                        serviceCollection.TryAdd(baseType, implementation, lifetime);
                    }
                    else
                    {
                        Utils.WriteDebug("injecting service:" + implementation.Name + "," + implementation.Name);
                        serviceCollection.TryAdd(implementation, implementation, lifetime);
                    }
                }
            }
            else
            {
                if (serviceType.IsClass)
                {
                    serviceCollection.TryAdd(serviceType, serviceType, lifetime);
                }
            }
        }
    }

    /// <summary>
    /// Scan and register classes that implement <see cref="ISingleton"/>,
    /// <see cref="IScoped"/>, or <see cref="ITransient"/> marker interfaces.
    /// If a class implements multiple marker interfaces, the first match
    /// in priority order (ISingleton &gt; IScoped &gt; ITransient) wins.
    /// </summary>
    internal static void AddServicesWithInterfaceMarker(this IServiceCollection services, Assembly[]? assemblies)
    {
        if (assemblies is not { Length: > 0 })
            return;

        var candidates = assemblies.SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract && !t.IsGenericTypeDefinition)
            .ToList();

        foreach (var type in candidates)
        {
            var serviceInterfaces = type.GetInterfaces();
            ServiceLifetime? lifetime = null;

            if (serviceInterfaces.Any(i => i == typeof(ISingleton)))
                lifetime = ServiceLifetime.Singleton;
            else if (serviceInterfaces.Any(i => i == typeof(IScoped)))
                lifetime = ServiceLifetime.Scoped;
            else if (serviceInterfaces.Any(i => i == typeof(ITransient)))
                lifetime = ServiceLifetime.Transient;

            if (lifetime == null)
                continue;

            services.RegisterImplementation(type, lifetime.Value);
        }
    }

    /// <summary>
    /// Register a type for its business interfaces, base class, or itself,
    /// excluding the marker interfaces (<see cref="ISingleton"/> etc.).
    /// </summary>
    private static void RegisterImplementation(this IServiceCollection services, Type implementation, ServiceLifetime lifetime)
    {
        var businessInterfaces = implementation.GetInterfaces()
            .Where(i => i != typeof(ISingleton) && i != typeof(IScoped) && i != typeof(ITransient))
            .ToArray();

        if (businessInterfaces.Length > 0)
        {
            foreach (var iface in businessInterfaces)
            {
                Utils.WriteDebug("injecting service (marker):" + iface.Name + "," + implementation.Name);
                services.TryAdd(iface, implementation, lifetime);
            }
        }

        Type? baseType = implementation.BaseType;
        if (baseType != null && baseType != typeof(object))
        {
            Utils.WriteDebug("injecting service (marker):" + baseType.Name + "," + implementation.Name);
            services.TryAdd(baseType, implementation, lifetime);
        }
        else
        {
            Utils.WriteDebug("injecting service (marker):" + implementation.Name + "," + implementation.Name);
            services.TryAdd(implementation, implementation, lifetime);
        }
    }
}


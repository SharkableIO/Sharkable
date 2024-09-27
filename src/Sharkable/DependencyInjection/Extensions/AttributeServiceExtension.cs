
namespace Sharkable;

/// <summary>
/// scan for service extensions
/// </summary>
internal static class AttributeBasedServiceCollectionExtensions
{
    /// <summary>
    /// scan for service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="serviceCollection"></param>
    /// <param name="assemblys"></param>
    internal static void AddServicesWithAttributeOfTypeFromAssembly(this IServiceCollection serviceCollection, Assembly[]? assemblys)
    {
        serviceCollection.AddServicesWithAttributeOfType(Shark.Assemblies);
    }
    /// <summary>
    /// scan for service
    /// </summary>
    /// <typeparam name="T"></typeparam>
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
    /// <typeparam name="T"></typeparam>
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

        foreach (Type serviceType in servicesToBeRegistered)
        {
            List<Type> implementations = [];

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
}


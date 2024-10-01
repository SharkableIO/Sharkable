
namespace Sharkable;
public class Injector
{
    private readonly IServiceProvider _serviceProvider;
    public Injector(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    public object? GetReflectedObjectV2(object objectType)
    {
        var assemblyQualifiedName = objectType.GetType().AssemblyQualifiedName;
        if (assemblyQualifiedName == null)
            return null;
        var type = Type.GetType(assemblyQualifiedName);
        if (type == null) return null;
        return Activator.CreateInstance(type);
    }
    public void GetConstructor(object objectType)
    {
        var type = objectType.GetType();
        var constructors = type.GetConstructors();
        var constructor = constructors.FirstOrDefault(
                constructor => GetFirstParameter(constructor) == typeof(Factory));
    }

    private static Type? GetFirstParameter(ConstructorInfo constructor)
    {
        return constructor?.GetParameters()?.FirstOrDefault()?.ParameterType;
    }

    public object GetReflectedObject(object objectType)
    {
        var requiredFactoryObject = new Factory(_serviceProvider);

        var assemblyQualifiedName = objectType.GetType().AssemblyQualifiedName;

        var parameters = GetConstructorParameters(objectType);

        var injectedParamerters = (new object[] { requiredFactoryObject })
            .Concat(GetDIParamters(parameters)).ToArray();

        return Activator.CreateInstance(Type.GetType(assemblyQualifiedName), injectedParamerters);
    }

    private IEnumerable<object> GetDIParamters(ParameterInfo[] parameters)
    {
        return parameters.Skip(1)
            .Select(parameter => _serviceProvider.GetService(parameter.ParameterType));
    }

    public ParameterInfo[] GetConstructorParameters(object objectType)
    {
        var type = objectType.GetType();
        var constructors = type.GetConstructors();
        return constructors.FirstOrDefault(
                constructor => GetFirstParameter(constructor) == typeof(Factory))
            .GetParameters();
    }
}

public class Factory
{
    private readonly IServiceProvider serviceProvider;

    public Factory(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }
}

using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// A factory that can create objects with DI
/// </summary>
public class DependencyReflectorFactory : IDependencyReflectorFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DependencyReflectorFactory> _logger;

    /// <summary>
    /// 
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="logger"></param>
    public DependencyReflectorFactory(IServiceProvider serviceProvider, ILogger<DependencyReflectorFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    /// <summary>
    /// get constructor for dependency injection
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public object?[] GetConstructorParameters(Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor == null)
        {
            _logger.LogDebug("No constructor found for {type}", type);
            throw new InvalidOperationException("No constructor found for type" + type);
        }
        var parameters = constructor.GetParameters()
            .Select(p=>_serviceProvider.GetService(p.ParameterType))
            .ToArray();
        return parameters;
    }

    public object CreateInstance(Type type)
    {
        var p = GetConstructorParameters(type);
        
        return Activator.CreateInstance(type, p) ?? throw new InvalidOperationException($"No constructor found for {type}");
    }
    public T GetReflectedType<T>(Type typeToReflect, object[] constructorRequiredParamerters)
        where T : class
    {
        var propertyTypeAssemblyQualifiedName = typeToReflect?.AssemblyQualifiedName;
        var constructors = typeToReflect?.GetConstructors();
        if (constructors?.Length == 0)
        {
            LogConstructorError(typeToReflect, constructorRequiredParamerters);
            return null!;
        }
        var parameters = GetConstructor(constructors, constructorRequiredParamerters)?.GetParameters();
        if (parameters == null)
        {
            LogConstructorError(typeToReflect, constructorRequiredParamerters);
            return null!;
        }
        object?[]? injectedParamerters = null;
        if (constructorRequiredParamerters == null)
        {
            injectedParamerters = parameters?.Select(parameter => _serviceProvider.GetService(parameter.ParameterType)).ToArray();
        }
        else
        {
            injectedParamerters = constructorRequiredParamerters
            .Concat(parameters.Skip(constructorRequiredParamerters.Length).Select(parameter => _serviceProvider.GetService(parameter.ParameterType)))
            .ToArray();
        }
        if (propertyTypeAssemblyQualifiedName == null)
            return null!;

        return (T)Activator.CreateInstance(Type.GetType(propertyTypeAssemblyQualifiedName)!, injectedParamerters)!;
    }

    /// <summary>
    /// Logs a constructor error
    /// </summary>
    /// <param name="typeToReflect"></param>
    /// <param name="constructorRequiredParamerters"></param>
    private void LogConstructorError(Type? typeToReflect, object[]? constructorRequiredParamerters)
    {
        string constructorNames = string.Join(", ", constructorRequiredParamerters?.Select(item => item?.GetType()?.Name));
        string message = $"Unable to create instance of {typeToReflect?.Name}. " +
            $"Could not find a constructor with {constructorNames} as first argument(s)";
        _logger.LogError(message);
    }

    /// <summary>
    /// Takes the required paramters from a constructor
    /// </summary>
    /// <param name="constructor"></param>
    /// <param name="constructorRequiredParamertersLength"></param>
    /// <returns></returns>
    private ParameterInfo[]? TakeConstructorRequiredParamters(ConstructorInfo constructor, int constructorRequiredParamertersLength)
    {
        var parameters = constructor.GetParameters();
        if (parameters.Length < constructorRequiredParamertersLength)
        {
            return parameters;
        }
        return parameters?.Take(constructorRequiredParamertersLength).ToArray();
    }

    /// <summary>
    /// Validates the required parameters from a constructor
    /// </summary>
    /// <param name="constructor"></param>
    /// <param name="constructorRequiredParameters"></param>
    /// <returns></returns>
    private bool ValidateConstructorRequiredParameters(ConstructorInfo constructor, object[] constructorRequiredParameters)
    {
        if (constructorRequiredParameters == null)
        {
            return true;
        }
        var parameters = TakeConstructorRequiredParamters(constructor, constructorRequiredParameters.Length);
        for (int i = 0; i < parameters?.Length; i++)
        {
            var requiredParameter = constructorRequiredParameters[i].GetType();
            if (parameters[i].ParameterType != requiredParameter)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Gets a constructor
    /// </summary>
    /// <param name="constructors"></param>
    /// <param name="constructorRequiredParameters"></param>
    /// <returns></returns>
    private ConstructorInfo? GetConstructor(ConstructorInfo[]? constructors, object[] constructorRequiredParameters)
    {
        return constructors?.FirstOrDefault(constructor =>
          ValidateConstructorRequiredParameters(constructor, constructorRequiredParameters));
    }
}

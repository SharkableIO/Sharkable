﻿using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// A factory that can create objects with DI
/// </summary>
public class DependencyReflectorFactory : IDependencyReflectorFactory
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger<DependencyReflectorFactory> logger;

    public DependencyReflectorFactory(IServiceProvider serviceProvider, ILogger<DependencyReflectorFactory> logger)
    {
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public object?[]? GetConstructorParameters(Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor == null)
        {
            throw new InvalidOperationException("no suitable constructor found");
        }

        var parameters = constructor.GetParameters().Select(p=>serviceProvider.GetService(p.ParameterType)).ToArray();
        return parameters;
    }

    public object? CreateInstance(Type type)
    {
        var p = GetConstructorParameters(type);

        return Activator.CreateInstance(type, p);
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
            injectedParamerters = parameters?.Select(parameter => serviceProvider.GetService(parameter.ParameterType)).ToArray();
        }
        else
        {
            injectedParamerters = constructorRequiredParamerters
            .Concat(parameters.Skip(constructorRequiredParamerters.Length).Select(parameter => serviceProvider.GetService(parameter.ParameterType)))
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
        logger.LogError(message);
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

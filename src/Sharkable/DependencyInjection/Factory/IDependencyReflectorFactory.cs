namespace Sharkable;

/// <summary>
/// Factory for creating objects via reflection with DI support.
/// This is an internal API of the legacy attribute-based endpoint system.
/// </summary>
[Obsolete("Internal API of the legacy [SharkEndpoint] system. No replacement needed — migrate to ISharkEndpoint.")]
public interface IDependencyReflectorFactory
{
    /// <summary>
    /// Gets the reflected type with DI
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="typeToReflect">The type to create</param>
    /// <param name="constructorRequiredParamerters">The required parameters on the constructor</param>
    /// <returns></returns>
    /// <summary>Creates a reflected instance of <typeparamref name="T"/> from the given type.</summary>
    T GetReflectedType<T>(Type typeToReflect, object[]? constructorRequiredParamerters) where T : class;
    /// <summary>Creates an instance of the given type using DI-resolved constructor parameters.</summary>
    object? CreateInstance(Type type);
    /// <summary>Returns constructor parameters resolved from the DI container for the given type.</summary>
    object?[]? GetConstructorParameters(Type type);
}

namespace Sharkable;

/// <summary>
/// Extension methods for creating delegates from method info via <see cref="Reflector"/>.
/// Internal API of the legacy attribute-based endpoint system.
/// </summary>
[Obsolete("Internal API of the legacy [SharkEndpoint] system. No replacement needed — migrate to ISharkEndpoint.")]
public static class ReflectorExtension
{
    internal static Delegate? GetDelegate(this object? instance, MethodInfo method)
    {
        return Reflector.GetMethodDelegate(method, instance);
    }
}

namespace Sharkable;

/// <summary>
/// Extension methods for creating delegates from method info via <see cref="Reflector"/>.
/// </summary>
public static class ReflectorExtension
{
    internal static Delegate? GetDelegate(this object? instance, MethodInfo method)
    {
        return Reflector.GetMethodDelegate(method, instance);
    }
}

namespace Sharkable;

public static class ReflectorExtension
{
    internal static Delegate? GetDelegate(this object? instance, MethodInfo method)
    {
        return Reflector.GetMethodDelegate(method, instance);
    }
}
namespace Sharkable;

/// <summary>
/// inject as singleton service
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SingletonServiceAttribute : Attribute
{
}

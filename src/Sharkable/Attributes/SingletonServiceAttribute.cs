namespace Sharkable;

/// <summary>
/// Shark singleton service
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SingletonServiceAttribute : Attribute
{
}

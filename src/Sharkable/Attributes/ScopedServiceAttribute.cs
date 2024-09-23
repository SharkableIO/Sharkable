namespace Sharkable;
/// <summary>
/// Shark scoped service
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
public sealed class ScopedServiceAttribute : Attribute
{
}
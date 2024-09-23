namespace  Sharkable;

/// <summary>
/// Shark transient service
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
public sealed class TransientServiceAttribute : Attribute
{
}

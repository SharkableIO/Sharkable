namespace Sharkable;

/// <summary>
/// Marks all endpoints in an <see cref="ISharkEndpoint"/> class as deprecated in the OpenAPI document.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SharkDeprecatedAttribute : Attribute
{
}

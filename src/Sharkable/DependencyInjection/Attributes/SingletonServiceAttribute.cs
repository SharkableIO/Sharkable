namespace Sharkable;

/// <summary>
/// inject as singleton service
/// </summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Class, AllowMultiple = false)]
public sealed class SingletonServiceAttribute : Attribute
{
    /// <summary>
    /// When <c>true</c>, the singleton is eagerly resolved at startup
    /// (forced instantiation) before the server begins accepting requests.
    /// Default is <c>false</c>.
    /// </summary>
    public bool Eager { get; set; }
}

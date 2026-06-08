namespace Sharkable;

/// <summary>
/// Overrides the OpenAPI tag for an endpoint class. Repeatable for multiple tags.
/// When absent, the tag is derived from the group name.
/// </summary>
/// <param name="tag">The OpenAPI tag value.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class SharkTagAttribute(string tag) : Attribute
{
    /// <summary>The OpenAPI tag value.</summary>
    public string Tag { get; } = tag;
}

namespace Sharkable;

/// <summary>
/// Explicitly assigns an endpoint class to a URL prefix group.
/// Multiple classes with the same group name share the same route prefix and OpenAPI tag.
/// When absent, the group name is derived from the class name.
/// </summary>
/// <param name="name">The group name (becomes the URL prefix segment and default OpenAPI tag).</param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class EndpointGroupAttribute(string name) : Attribute
{
    /// <summary>The group name.</summary>
    public string Name { get; } = name;
}

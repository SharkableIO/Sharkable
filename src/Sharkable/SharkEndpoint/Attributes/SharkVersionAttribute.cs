namespace Sharkable;

/// <summary>
/// Specifies an API version for an <see cref="ISharkEndpoint"/> class.
/// The version is prepended to the URL path: <c>api/v1/{group}/{route}</c>.
/// </summary>
/// <param name="version">Version string (e.g., "v1", "V2"). Case is transformed by <see cref="EndpointFormat"/>.</param>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class SharkVersionAttribute(string version) : Attribute
{
    /// <summary>The version string to include in the URL path.</summary>
    public string Version { get; } = version;
}

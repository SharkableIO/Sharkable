namespace Sharkable;

/// <summary>
/// Case formatting options for endpoint URL paths.
/// Applied to group names and route patterns derived from class/method names.
/// </summary>
public enum EndpointFormat
{
    /// <summary>Convert to all lowercase.</summary>
    ToLower,
    /// <summary>Keep the original casing unchanged.</summary>
    UnChanged,
    /// <summary>Convert to camelCase (default).</summary>
    CamelCase,
    /// <summary>Convert to snake_case.</summary>
    SnakeCase,
}

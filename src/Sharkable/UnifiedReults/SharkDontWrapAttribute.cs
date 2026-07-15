namespace Sharkable;

/// <summary>
/// Prevents auto-wrapping of return values for an entire <see cref="ISharkEndpoint"/> class.
/// When applied, ALL routes under that endpoint class skip auto-wrap.
/// Use <see cref="DisableAutoWrapExtensions.DisableAutoWrap"/> to exclude individual routes.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class SharkDontWrapAttribute : Attribute { }

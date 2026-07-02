namespace Sharkable;

/// <summary>
/// Marks a property as sensitive so the OpenAPI schema transformer strips
/// it from <c>/openapi/v1.json</c>. Use for fields like
/// <c>Password</c>, <c>RefreshToken</c>, <c>ApiSecret</c>, <c>PasswordHash</c>
/// that must not appear in the public schema even when they are part of a
/// response DTO. SHARK-SEC-L009.
/// </summary>
/// <example>
/// <code>
/// public sealed class LoginResponse
/// {
///     [SharkOpenApiIgnore]
///     public string AccessToken { get; init; } = "";
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
public sealed class SharkOpenApiIgnoreAttribute : Attribute
{
}
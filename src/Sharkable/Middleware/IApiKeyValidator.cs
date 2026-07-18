using System.Security.Claims;

namespace Sharkable;

/// <summary>
/// Result of API key validation. Carries claims/roles for the authenticated principal.
/// </summary>
public sealed class ApiKeyValidationResult
{
    /// <summary>
    /// Whether the API key is valid.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Claims to attach to the authenticated principal.
    /// </summary>
    public IReadOnlyList<Claim>? Claims { get; }

    /// <summary>
    /// Optional per-key rate-limit multiplier.
    /// Values &gt; 1 increase the effective limit for this key.
    /// Values &lt; 1 decrease it. Default is 1.0 (no multiplier).
    /// </summary>
    public double RateLimitMultiplier { get; }

    /// <summary>
    /// Creates a successful validation result with optional claims and rate-limit multiplier.
    /// </summary>
    public static ApiKeyValidationResult Success(
        IReadOnlyList<Claim>? claims = null,
        double rateLimitMultiplier = 1.0)
    {
        return new ApiKeyValidationResult(true, claims, rateLimitMultiplier);
    }

    /// <summary>
    /// Creates a failed validation result.
    /// </summary>
    public static ApiKeyValidationResult Failed()
    {
        return new ApiKeyValidationResult(false, null, 1.0);
    }

    private ApiKeyValidationResult(bool isValid, IReadOnlyList<Claim>? claims, double rateLimitMultiplier)
    {
        IsValid = isValid;
        Claims = claims;
        RateLimitMultiplier = rateLimitMultiplier;
    }
}

/// <summary>
/// Validates API keys presented in requests. Implement this to support
/// multi-tenant API-key scenarios with per-key claims/roles.
/// Falls back to the configured <c>SharkOption.ApiKeys</c> array when not registered.
/// </summary>
public interface IApiKeyValidator
{
    /// <summary>
    /// Validates the given API key and returns a validation result with
    /// optional claims and rate-limit multiplier.
    /// </summary>
    /// <param name="apiKey">The API key value from the request header.</param>
    /// <returns>Validation result indicating success/failure and optional metadata.</returns>
    ApiKeyValidationResult Validate(string apiKey);
}

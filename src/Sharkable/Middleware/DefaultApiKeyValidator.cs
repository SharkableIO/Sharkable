namespace Sharkable;

/// <summary>
/// Default implementation of <see cref="IApiKeyValidator"/> that validates
/// against the static <c>opt.ApiKeys</c> array using constant-time SHA-256 comparison.
/// Replaced when a custom <see cref="IApiKeyValidator"/> is registered.
/// </summary>
internal sealed class DefaultApiKeyValidator : IApiKeyValidator
{
    private readonly ApiKeyValidator _inner;

    public DefaultApiKeyValidator(ApiKeyValidator inner)
    {
        _inner = inner;
    }

    public ApiKeyValidationResult Validate(string apiKey)
    {
        if (_inner.Validate(apiKey))
            return ApiKeyValidationResult.Success();

        return ApiKeyValidationResult.Failed();
    }
}

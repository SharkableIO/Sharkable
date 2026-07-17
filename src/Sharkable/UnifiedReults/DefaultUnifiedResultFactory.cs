using System.Net;

namespace Sharkable;

internal sealed class DefaultUnifiedResultFactory : IUnifiedResultFactory
{
    internal static readonly DefaultUnifiedResultFactory Instance = new();

    public IUnifiedResult Create(object? data, string? errorMessage, int statusCode)
    {
        return new UnifiedResult<object?>
        {
            StatusCode = (HttpStatusCode)statusCode,
            Data = data,
            ErrorMessage = errorMessage
        };
    }
}

internal static class UnifiedResultFactoryHelper
{
    internal static IUnifiedResultFactory ResolveFactory()
    {
        return Shark.SharkOption.UnifiedResultFactory ?? DefaultUnifiedResultFactory.Instance;
    }
}

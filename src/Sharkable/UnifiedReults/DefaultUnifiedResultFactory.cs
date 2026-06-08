using System.Net;

namespace Sharkable;

internal sealed class DefaultUnifiedResultFactory : IUnifiedResultFactory
{
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

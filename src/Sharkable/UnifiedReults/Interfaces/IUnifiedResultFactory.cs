namespace Sharkable;

public interface IUnifiedResultFactory
{
    IUnifiedResult Create(object? data, string? errorMessage, int statusCode);
}

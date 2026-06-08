namespace Sharkable;

public interface IUnifiedResult
{
    int StatusCode { get; }
    object? Data { get; }
    string? ErrorMessage { get; }
}

namespace Sharkable.NativeTest;

public class ExceptionTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/throw/notfound", () =>
        {
            throw new KeyNotFoundException("user not found");
        });

        app.MapGet("/throw/unauthorized", () =>
        {
            throw new UnauthorizedAccessException("token expired");
        });

        app.MapGet("/throw/bad", () =>
        {
            throw new ArgumentException("invalid input");
        });

        app.MapGet("/throw/server", () =>
        {
            throw new InvalidOperationException("something went wrong");
        });

        app.MapGet("/wrap", () => "hello from auto-wrap");
    }
}

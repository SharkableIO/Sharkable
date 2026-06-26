namespace Sharkable.AotSample;

public class IdempotencyTestEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("idempotency/test", (string body) => Results.Ok(new { received = body }));
    }
}
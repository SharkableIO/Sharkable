using System;

namespace Sharkable.AotSample;

public class LoveSellerServiceV2 : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/lover", () =>
        {
        
            return Results.Ok("lover");
        }).AllowAnonymous();
    }
}

public class LoveSellerServiceV1 : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/lover", () =>
        {
        
            return Results.Ok("lover");
        }).AllowAnonymous();
    }
}

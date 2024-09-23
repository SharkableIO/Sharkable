using System;
using Microsoft.AspNetCore.Mvc;

namespace Sharkable.AotSample;

public class LoveSellerServiceV2 : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.SharkMapGet("/ILoveYouV2{like}/{id}", ([FromServices]ILogger<LoveSellerServiceV2> logger, int id, int like) =>
        {
            logger.LogInformation(id.ToString());
            logger.LogInformation(like.ToString());
            return Results.Ok("lover");
        }).AllowAnonymous();
    }
}

public class LoveSellerV1Service : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/lover", () =>
        {
        
            return Results.Ok("lover");
        }).AllowAnonymous();
    }
}

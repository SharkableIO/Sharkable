using System;
using Microsoft.AspNetCore.Mvc;

namespace Sharkable.AotSample;

// public class LoveSellerServiceV2 : ISharkEndpoint
// {
//     public void AddRoutes(IEndpointRouteBuilder app)
//     {
//         app.MapGet("/ILoveYouV2/{id}", ([FromServices]ILogger<LoveSellerServiceV2> logger, int id) =>
//         {
//             logger.LogInformation(id.ToString());
            
//             return Results.Ok("lover");
//         }).AllowAnonymous();
//     }
// }

public class LoveSellerV1Service : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/lover", () =>
        {
        
            return TypedResults.Ok("lover");
        })
        .WithName("GetLover")
        .WithOpenApi(op=>new(op)
        {
            Summary = "love is love",
            Description = "i know this is hard to say"
        });
    }
}

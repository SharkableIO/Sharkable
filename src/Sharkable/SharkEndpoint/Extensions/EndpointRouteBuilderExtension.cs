using System;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Sharkable;

public static class EndpointRouteBuilderExtension
{
    public static IEndpointConventionBuilder SharkMapGet(this IEndpointRouteBuilder app, [StringSyntax("Route")]string sharkPattern, Delegate sharkHandler)
    {
        var p = sharkPattern.GetCaseFormat(Shark.SharkOption.Format) ?? 
            throw new Exception("error when formatting given string path");
        
        return app.MapGet(p, sharkHandler);
    }
}

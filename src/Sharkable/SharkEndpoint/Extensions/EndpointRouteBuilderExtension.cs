using System;
using Microsoft.AspNetCore.Routing.Patterns;

namespace Sharkable;

/// <summary>
/// Extension methods for <see cref="IEndpointRouteBuilder"/> with Sharkable formatting support.
/// </summary>
public static class EndpointRouteBuilderExtension
{
    /// <summary>
    /// Maps a GET request with automatic path formatting (case, version) based on <see cref="SharkOption.Format"/>.
    /// </summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <param name="sharkPattern">The route pattern (formatted automatically).</param>
    /// <param name="sharkHandler">The request delegate.</param>
    public static IEndpointConventionBuilder SharkMapGet(this IEndpointRouteBuilder app, [StringSyntax("Route")]string sharkPattern, Delegate sharkHandler)
    {
        var p = sharkPattern.GetCaseFormat(Shark.SharkOption.Format) ?? 
            throw new Exception("error when formatting given string path");
        
        return app.MapGet(p, sharkHandler);
    }
}

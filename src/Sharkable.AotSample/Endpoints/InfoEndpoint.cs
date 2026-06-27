namespace Sharkable.AotSample;

/// <summary>
/// Service info and health endpoints.
/// </summary>
[SharkDescription("Service Information", "Version info, feature flags, and health")]
public sealed class InfoEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("/version", () =>
        {
            return Results.Ok(new VersionInfo(
                Version: "0.3.1",
                Framework: ".NET 10",
                AotMode: true,
                EnabledFeatures:
                [
                    "ISharkEndpoint convention routing",
                    "OpenAPI metadata attributes",
                    "API versioning",
                    "FluentValidation",
                    "Rate limiting",
                    "Output cache",
                    "Idempotency",
                    "Audit trail",
                    "Redacting formatter",
                    "CORS",
                    "JWT + API Key auth",
                    "Health checks",
                    "Global exception handler",
                    "Unified responses",
                ]
            ));
        })
        .WithName("GetVersionInfo")
        .SharkCacheOutput();

        app.MapGet("/features", () =>
        {
            return Results.Ok(new FeatureFlagsResponse(
                AotMode: true,
                ApiPrefix: "api",
                EndpointFormat: "camelCase",
                IdempotencyEnabled: true,
                ValidationEnabled: true,
                HealthChecksEnabled: true
            ));
        })
        .WithName("GetFeatures");
    }
}

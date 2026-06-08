namespace Sharkable.NativeTest;

[SharkVersion("v1")]
[SharkTag("versioned")]
public class VersionedV1Endpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("info", () => new VersionInfo("v1", "Initial release"));
    }
}

[SharkVersion("v2")]
[SharkTag("versioned")]
public class VersionedV2Endpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("info", () => new VersionInfo("v2", "With new features"));
    }
}

public sealed record VersionInfo(string Version, string Description);

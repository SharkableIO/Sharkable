namespace Sharkable.NativeTest;

[EndpointGroup("admin")]
[SharkTag("admin")]
public class AdminEndpoint : ISharkEndpoint
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("users", () =>
        {
            var users = new UserInfo[]
            {
                new(1, "Alice"),
                new(2, "Bob")
            };
            return Results.Ok(users);
        });

        app.MapGet("report", () =>
        {
            var logger = Shark.GetService<ILogger<AdminEndpoint>>();
            logger?.LogInformation("report requested");
            return Results.Ok(new ReportStats(2, 1));
        });
    }
}

public sealed record UserInfo(int Id, string Name);

public sealed record ReportStats(int TotalUsers, int ActiveUsers);

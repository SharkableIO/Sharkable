namespace Sharkable;

internal static class CronAdminEndpoint
{
    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/_sharkable/jobs", async (ICronScheduler scheduler) =>
        {
            var list = await scheduler.ListAsync();
            return Results.Ok(list);
        }).ExcludeFromDescription();
    }
}

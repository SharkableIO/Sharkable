using Microsoft.AspNetCore.Http;

namespace Sharkable;

internal static class CronAdminEndpoint
{
    private const int MaxLastErrorChars = 100;

    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/_sharkable/jobs", async (ICronScheduler scheduler, HttpContext context, ApiKeyValidator validator) =>
        {
            if (Shark.SharkOption.CronAdminRequireApiKey && !IsApiKeyAuthorized(context, validator))
                return Results.NotFound();

            var list = await scheduler.ListAsync();
            return Results.Ok(list.Select(RedactState));
        }).ExcludeFromDescription();
    }

    private static object RedactState(CronJobState s) => new
    {
        s.Name,
        s.Description,
        s.Cron,
        s.IsRunning,
        s.NextRun,
        s.LastRun,
        s.LastDurationMs,
        LastError = TruncateLastError(s.LastError),
        s.RunCount,
        s.Paused,
    };

    private static string? TruncateLastError(string? value)
    {
        if (value == null) return null;
        if (value.Length <= MaxLastErrorChars) return value;

        var cut = MaxLastErrorChars;
        if (char.IsHighSurrogate(value[cut - 1])) cut--;
        return value.Substring(0, cut) + "...";
    }

    private static bool IsApiKeyAuthorized(HttpContext context, ApiKeyValidator validator)
    {
        if (!validator.HasConfiguredKeys)
            return false;

        if (!context.Request.Headers.TryGetValue(Shark.SharkOption.ApiKeyHeaderName, out var provided))
            return false;

        return validator.Validate(provided.ToString());
    }
}

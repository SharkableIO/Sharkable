using System.Security.Cryptography;
using System.Text;

namespace Sharkable;

internal static class CronAdminEndpoint
{
    /// <summary>
    /// Maximum number of characters of <see cref="CronJobState.LastError"/>
    /// surfaced through the admin endpoint. Longer messages are truncated and
    /// suffixed with <c>"..."</c> to prevent business-logic leakage via error
    /// text (SHARK-SEC-016).
    /// </summary>
    private const int MaxLastErrorChars = 100;

    internal static void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/_sharkable/jobs", async (ICronScheduler scheduler, HttpContext context) =>
        {
            // SHARK-SEC-016: gate on API key by default. Fail-closed — if no
            // keys are configured, the endpoint responds 404 so its existence
            // is not leaked to unauthenticated probes.
            if (Shark.SharkOption.CronAdminRequireApiKey && !IsApiKeyAuthorized(context))
                return Results.NotFound();

            var list = await scheduler.ListAsync();
            return Results.Ok(list.Select(RedactState));
        }).ExcludeFromDescription();
    }

    /// <summary>
    /// Projects a <see cref="CronJobState"/> into an anonymous record with the
    /// <c>LastError</c> field truncated to <see cref="MaxLastErrorChars"/>
    /// characters. Other fields pass through unchanged.
    /// </summary>
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

    /// <summary>
    /// Truncates <paramref name="value"/> to <see cref="MaxLastErrorChars"/>
    /// characters and appends an ellipsis when truncation occurred. When the
    /// character at the cut boundary is a UTF-16 high surrogate, the boundary
    /// is shifted back by one so the resulting string never contains an
    /// unpaired surrogate (which would render as <c>U+FFFD</c> in JSON and
    /// downstream consumers) — SHARK-SEC-016 follow-up.
    /// </summary>
    private static string? TruncateLastError(string? value)
    {
        if (value == null) return null;
        if (value.Length <= MaxLastErrorChars) return value;

        var cut = MaxLastErrorChars;
        if (char.IsHighSurrogate(value[cut - 1])) cut--;
        return value.Substring(0, cut) + "...";
    }

    /// <summary>
    /// Returns <c>true</c> when the request carries a configured API key.
    /// Mirrors the constant-time SHA-256 comparison used by
    /// <see cref="ApiKeyFilter"/> (SHARK-SEC-008). Returns <c>false</c> — and
    /// the caller maps that to <c>404</c> — when no keys are configured at all
    /// so the endpoint's existence is not advertised.
    /// </summary>
    private static bool IsApiKeyAuthorized(HttpContext context)
    {
        var keys = Shark.SharkOption.ApiKeys;
        if (keys == null || keys.Length == 0)
            return false;

        if (!context.Request.Headers.TryGetValue(Shark.SharkOption.ApiKeyHeaderName, out var provided))
            return false;

        var candidateHash = SHA256.HashData(Encoding.UTF8.GetBytes(provided.ToString()));
        var matched = false;
        for (var i = 0; i < keys.Length; i++)
        {
            var stored = SHA256.HashData(Encoding.UTF8.GetBytes(keys[i]));
            if (CryptographicOperations.FixedTimeEquals(candidateHash, stored))
                matched = true;
        }
        return matched;
    }
}

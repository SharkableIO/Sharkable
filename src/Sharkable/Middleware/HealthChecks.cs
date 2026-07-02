using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Health check that verifies the configured JWT authority is reachable.
/// Registered automatically when <c>ConfigureJwt()</c> is set.
/// </summary>
internal sealed class JwtHealthCheck : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var authority = Shark.SharkOption.JwtAuthority;
        if (string.IsNullOrEmpty(authority))
            return HealthCheckResult.Unhealthy("JWT authority is not configured");

        // SHARK-SEC-M004: /healthz is publicly readable and the previous
        // implementation echoed the authority URL (and ex.Message on
        // failure) into the JSON description, leaking the OIDC issuer
        // topology and exception internals to any anonymous caller.
        // Return generic descriptions; the full URL and exception details
        // are surfaced via structured logging below.
        var logger = InternalShark.ServiceProvider?.GetService<ILoggerFactory>()
            ?.CreateLogger<JwtHealthCheck>();

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync(
                $"{authority.TrimEnd('/')}/.well-known/openid-configuration",
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                logger?.LogDebug("JWT authority reachable: {Authority}", authority);
                return HealthCheckResult.Healthy("JWT authority reachable");
            }

            logger?.LogWarning(
                "JWT authority returned {StatusCode}: {Authority}",
                response.StatusCode, authority);
            return HealthCheckResult.Degraded("JWT authority probe failed");
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "JWT authority unreachable: {Authority}", authority);
            return HealthCheckResult.Unhealthy("JWT authority unreachable");
        }
    }
}

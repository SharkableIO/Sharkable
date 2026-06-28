using Microsoft.Extensions.Diagnostics.HealthChecks;

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

        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            var response = await http.GetAsync(
                $"{authority.TrimEnd('/')}/.well-known/openid-configuration",
                cancellationToken);

            if (response.IsSuccessStatusCode)
                return HealthCheckResult.Healthy($"JWT authority reachable: {authority}");

            return HealthCheckResult.Degraded(
                $"JWT authority returned {response.StatusCode}: {authority}");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                $"JWT authority unreachable ({authority}): {ex.Message}");
        }
    }
}

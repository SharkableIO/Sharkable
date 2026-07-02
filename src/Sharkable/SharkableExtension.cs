using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Threading.Tasks;

namespace  Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Entry points for registering Sharkable services and wiring the application pipeline.
/// </summary>
public static class SharkableExtension
{
    /// <summary>
    /// Registers Sharkable services with automatic assembly discovery.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="setupOption">Optional callback to configure <see cref="SharkOption"/>.</param>
    [RequiresDynamicCode("use Assembly[] method instead")]
    public static void AddShark(this IServiceCollection services, Action<SharkOption>? setupOption = null)
    {
        //set aot mode to false if use this method
        InternalShark.AotMode = false;
        //get assemblies
        Shark.SetAssebly(Utils.GetAssemblies());
        //set common extensions
        services.AddCommon(setupOption);
    }

    /// <summary>
    /// Registers Sharkable services with explicit assemblies. Recommended for AOT scenarios.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="assembly">Assemblies to scan — required in AOT mode.</param>
    /// <param name="setupOption">Optional callback to configure <see cref="SharkOption"/>.</param>
    public static void AddShark(this IServiceCollection services, Assembly[]? assembly, Action<SharkOption>? setupOption = null)
    {
        ArgumentNullException.ThrowIfNull(assembly);
        //set aot mode to true if use this method
        InternalShark.AotMode = true;
        //get assemblies
        Shark.SetAssebly(Utils.GetAssemblies(assembly));
        //set common extensions
        services.AddCommon(setupOption);
    }

    /// <summary>
    /// Wires Sharkable middleware into the application pipeline — exception handler, swagger, and endpoint mapping.
    /// Must be called after <c>builder.Build()</c> and before <c>app.Run()</c>.
    /// </summary>
    /// <param name="app">The <see cref="WebApplication"/> to configure.</param>
    /// <param name="setupOption">Optional callback to configure <see cref="UseSharkOptions"/>.</param>
    public static void UseShark(this WebApplication app, Action<UseSharkOptions>? setupOption = null)
    {
        app.UseCommon(setupOption);

        // distributed tracing — must be first in pipeline to capture full request
        if (Shark.SharkOption.TracingOptions != null)
            app.UseMiddleware<TracingMiddleware>();

        // response compression — early for max coverage
        if (Shark.SharkOption.EnableResponseCompression)
            app.UseResponseCompression();

        // graceful shutdown — must be early in pipeline to reject new requests
        var gsOptions = Shark.SharkOption.GracefulShutdownOptions;
        var auditOptions = Shark.SharkOption.AuditTrailOptions;
        if (gsOptions != null || auditOptions?.EnsureFlushOnShutdown == true)
        {
            var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

            lifetime.ApplicationStopping.Register(() =>
            {
                if (gsOptions != null)
                {
                    Volatile.Write(ref InternalShark.IsShuttingDown, true);
                }

                Task.Run(async () =>
                {
                    try
                    {
                        if (gsOptions != null)
                        {
                            using var drainCts = new CancellationTokenSource(gsOptions.DrainTimeout);
                            var pollingMs = Math.Max(
                                (int)gsOptions.DrainPollingInterval.TotalMilliseconds, 10);

                            while (!drainCts.IsCancellationRequested)
                            {
                                if (Volatile.Read(ref InternalShark.ActiveRequests) == 0)
                                    break;
                                await Task.Delay(pollingMs, drainCts.Token);
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (Exception)
                    {
                    }

                    InternalShark.AuditLogBuffer?.FlushRemaining();
                }).GetAwaiter().GetResult();
            });

            if (gsOptions != null)
                app.UseMiddleware<GracefulShutdownMiddleware>();
        }

        // multi-tenant
        if (Shark.SharkOption.TenantOptions?.ResolveTenant != null)
            app.UseMiddleware<TenantResolutionMiddleware>();

        // rate limiter
        if (Shark.SharkOption.RateLimiterConfigure != null)
            app.UseRateLimiter();
        if (Shark.SharkOption.RateLimitingOptions != null)
            app.UseMiddleware<SharkRateLimiterMiddleware>();
        // output cache
        if (Shark.SharkOption.OutputCacheConfigure != null)
            app.UseOutputCache();
        // CORS
        if (Shark.SharkOption.CorsConfigure != null)
            app.UseCors();
        // JWT auth
        if (Shark.SharkOption.JwtAuthority != null)
        {
            app.UseAuthentication();
            if (Shark.SharkOption.EnableAuthorization)
                app.UseAuthorization();
        }

        if (Shark.UseSharkOptions?.EnableExceptionHandler ?? true)
        {
            Shark.SharkOption.ExceptionHandlerOptions.IsDevelopment = app.Environment.IsDevelopment();
            app.UseSharkExceptionHandler();
        }

        // audit trail
        if (Shark.SharkOption.AuditTrailOptions != null)
            app.UseMiddleware<AuditTrailMiddleware>();

        // idempotency
        if (Shark.SharkOption.EnableIdempotency)
            app.UseMiddleware<SharkIdempotencyMiddleware>();

        // ETag / 304 conditional responses
        if (Shark.SharkOption.EnableETag)
            app.UseMiddleware<ETagMiddleware>();

        // profiler — wraps endpoints to record latency/memory
        if (Shark.SharkOption.ProfilerOptions != null)
        {
            app.UseMiddleware<ProfilerMiddleware>();
            app.MapProfilerEndpoint();
        }

        // cron jobs admin endpoint
        if (Shark.SharkOption.ConfigureCronJobs != null)
            CronAdminEndpoint.Map(app);

        app.MapEndpoints();
    }
}

using Microsoft.Extensions.Hosting;

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

        // multi-tenant
        if (Shark.SharkOption.TenantOptions?.ResolveTenant != null)
            app.UseMiddleware<TenantResolutionMiddleware>();

        // rate limiter
        if (Shark.SharkOption.RateLimiterConfigure != null)
            app.UseRateLimiter();
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

        app.MapEndpoints();
    }
}

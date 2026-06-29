using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sharkable;

public static class SharkExtension
{
    /// <summary>
    /// common service collection extensions for sharkable
    /// </summary>
    /// <param name="services"></param>
    /// <param name="setupOptions"></param>
    internal static void AddCommon(this IServiceCollection services, Action<SharkOption>? setupOptions = null)
    {
        services.AddJsonContext();
        services.AddDiFactory();
        //reset and invoke options
        Shark.SharkOption = new SharkOption();
        setupOptions?.Invoke(Shark.SharkOption);
        services.Configure<SharkOption>((opt) => 
        { 
            setupOptions?.Invoke(opt);
        });
        //wire endpoints
        services.WireSharkEndpoint();
        //wire service lifetime
        services.AddServicesWithAttributeOfTypeFromAssembly(Shark.Assemblies);
        //setup OpenAPI document generation
        services.AddSharkOpenApi();
        //register fluent validation
        services.AddValidators();
        //setup auto crud services
        services.AddAutoCrud();
        //register rate limiter
        if (Shark.SharkOption.RateLimiterConfigure != null)
            services.AddRateLimiter(Shark.SharkOption.RateLimiterConfigure);
        //register output cache
        if (Shark.SharkOption.OutputCacheConfigure != null)
            services.AddOutputCache(Shark.SharkOption.OutputCacheConfigure);
        //register health checks
        if (Shark.SharkOption.EnableHealthChecks)
        {
            var hc = services.AddHealthChecks();
            Shark.SharkOption.HealthChecksConfigure?.Invoke(hc);

            // auto-check JWT authority if configured
            if (Shark.SharkOption.JwtAuthority != null)
                hc.AddCheck<JwtHealthCheck>("jwt");
        }
        //register response compression
        if (Shark.SharkOption.EnableResponseCompression)
            services.AddResponseCompression();
        //register CORS
        if (Shark.SharkOption.CorsConfigure != null)
            services.AddCors(Shark.SharkOption.CorsConfigure);
        //register idempotency
        if (Shark.SharkOption.EnableIdempotency)
        {
            services.AddMemoryCache();
            services.AddSingleton(Shark.SharkOption.IdempotencyOptions ?? new SharkIdempotencyOptions());
            if (Shark.SharkOption.IdempotencyStoreFactory != null)
                services.AddSingleton(Shark.SharkOption.IdempotencyStoreFactory);
            else
                services.TryAddSingleton<IIdempotencyStore, MemoryIdempotencyStore>();
        }
        //register distributed rate limiter
        if (Shark.SharkOption.RateLimitingOptions != null)
        {
            services.AddSingleton(Shark.SharkOption.RateLimitingOptions);
            if (Shark.SharkOption.RateLimitStoreFactory != null)
                services.AddSingleton(Shark.SharkOption.RateLimitStoreFactory);
            else
                services.TryAddSingleton<IDistributedRateLimitStore, MemoryRateLimitStore>();
        }
        else
        {
            // Always register the store interface so plugins can provide their own.
            // TryAddSingleton ensures a previously-registered plugin implementation wins.
            if (Shark.SharkOption.RateLimitStoreFactory != null)
                services.AddSingleton(Shark.SharkOption.RateLimitStoreFactory);
            else
                services.TryAddSingleton<IDistributedRateLimitStore, MemoryRateLimitStore>();
        }
        //register JWT auth
        if (Shark.SharkOption.JwtAuthority != null)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(opt =>
                {
                    opt.Authority = Shark.SharkOption.JwtAuthority;
                    opt.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = Shark.SharkOption.JwtAudiences?.Length > 0,
                        ValidateIssuerSigningKey = true,
                        ValidateLifetime = true,
                    };
                    if (Shark.SharkOption.JwtAudiences?.Length > 0)
                        opt.TokenValidationParameters.ValidAudiences = Shark.SharkOption.JwtAudiences;

                    // Let user configure first (OnTokenValidated, custom claims, etc.)
                    Shark.SharkOption.JwtConfigure?.Invoke(opt);

                    // Save user's handlers so we can chain with Sharkable defaults
                    var userOnChallenge = opt.Events.OnChallenge;
                    var userOnForbidden = opt.Events.OnForbidden;

                    opt.Events.OnChallenge = async ctx =>
                    {
                        if (userOnChallenge != null)
                            await userOnChallenge(ctx);
                        if (!ctx.Response.HasStarted)
                        {
                            ctx.HandleResponse();
                            ctx.Response.StatusCode = 401;
                            await ProblemDetailsResult.WriteAsync(ctx.HttpContext, 401, "Authentication failed");
                        }
                    };

                    opt.Events.OnForbidden = async ctx =>
                    {
                        if (userOnForbidden != null)
                            await userOnForbidden(ctx);
                        if (!ctx.Response.HasStarted)
                        {
                            ctx.Response.StatusCode = 403;
                            await ProblemDetailsResult.WriteAsync(ctx.HttpContext, 403, "Forbidden");
                        }
                    };
                });
        }
        //register redacting log formatter
        if (Shark.SharkOption.RedactingLogOptions != null)
        {
            services.AddSingleton(Shark.SharkOption.RedactingLogOptions);
            services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(RedactingLogger<>)));
        }
        //register multi-tenant services
        if (Shark.SharkOption.TenantOptions != null)
        {
            services.AddScoped<ITenant, Tenant>();

            if (Shark.SharkOption.TenantOptions.DataSourceOptions != null)
            {
                services.AddSingleton(Shark.SharkOption.TenantOptions.DataSourceOptions);
                services.AddScoped<ITenantDataSource, DefaultTenantDataSource>();
            }
        }
        //register distributed tracing
        if (Shark.SharkOption.TracingOptions != null)
        {
            services.AddSingleton(Shark.SharkOption.TracingOptions);
            if (Shark.SharkOption.TracingOptions.Exporter != null)
                services.AddSingleton(Shark.SharkOption.TracingOptions.Exporter);
        }
        //register profiler
        if (Shark.SharkOption.ProfilerOptions != null)
        {
            services.AddSingleton(Shark.SharkOption.ProfilerOptions);
        }
        //register ETag
        if (Shark.SharkOption.EnableETag)
        {
            services.AddSingleton(Shark.SharkOption.ETagOptions ?? new ETagOptions());
        }
        //register error localizer
        if (Shark.SharkOption.ErrorLocalizerFactory != null)
        {
            services.AddSingleton(Shark.SharkOption.ErrorLocalizerFactory);
        }
        else
        {
            services.TryAddSingleton<IErrorLocalizer, DefaultErrorLocalizer>();
        }
        //register authorization interceptor
        if (Shark.SharkOption.AuthorizationInterceptorFactory != null)
        {
            services.AddSingleton(Shark.SharkOption.AuthorizationInterceptorFactory);
        }
        //register saga store
        if (Shark.SharkOption.SagaStoreFactory != null)
            services.AddSingleton<ISagaStore>(sp => Shark.SharkOption.SagaStoreFactory(sp));
        else
            services.TryAddSingleton<ISagaStore, MemorySagaStore>();
        services.TryAddSingleton<SagaExecutor>();

        //validate configuration
        var configErrors = ConfigurationValidator.Validate();
        if (configErrors.Count > 0)
        {
            throw new SharkConfigurationException(configErrors);
        }
    }
    /// <summary>
    /// common webapplication extensions for sharkable
    /// </summary>
    /// <param name="app"></param>
    /// <param name="setupOptions"></param>
    internal static void UseCommon(this WebApplication app, Action<UseSharkOptions>? setupOptions = null)
    {
        var opt = new UseSharkOptions();
        //invoke options
        setupOptions?.Invoke(opt);
        Shark.UseSharkOptions = opt;
        //configure internal shark 
        InternalShark.Configuration = app.Configuration;
        InternalShark.HostEnvironment = app.Environment;
        InternalShark.ServiceScopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        InternalShark.ServiceProvider = app.Services;
        InternalShark.StartedAt = DateTimeOffset.UtcNow;
        InternalShark.AppVersion = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Version?.ToString(3);
        //setup OpenAPI endpoint + Scalar UI
        app.UseSharkOpenApi();
       // app.MapSharkEndpointsWithAttributes();
    }
}

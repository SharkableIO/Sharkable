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
            services.AddHealthChecks();
        //register CORS
        if (Shark.SharkOption.CorsConfigure != null)
            services.AddCors(Shark.SharkOption.CorsConfigure);
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
                    opt.Events = new JwtBearerEvents
                    {
                        OnChallenge = ctx =>
                        {
                            ctx.HandleResponse();
                            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
                            var result = factory.Create(null, "Authentication failed", 401);
                            ctx.Response.StatusCode = 401;
                            ctx.Response.ContentType = "application/json";
                            return ctx.Response.WriteAsJsonAsync(result, result.GetType());
                        },
                        OnForbidden = ctx =>
                        {
                            var factory = Shark.SharkOption.UnifiedResultFactory ?? new DefaultUnifiedResultFactory();
                            var result = factory.Create(null, "Forbidden", 403);
                            ctx.Response.StatusCode = 403;
                            ctx.Response.ContentType = "application/json";
                            return ctx.Response.WriteAsJsonAsync(result, result.GetType());
                        },
                    };
                    Shark.SharkOption.JwtConfigure?.Invoke(opt);
                });
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
        InternalShark.ServiceProvider = app.Services;//InternalShark.ServiceScopeFactory.CreateScope().ServiceProvider;
        //setup OpenAPI endpoint + Scalar UI
        app.UseSharkOpenApi();
       // app.MapSharkEndpointsWithAttributes();
    }
}

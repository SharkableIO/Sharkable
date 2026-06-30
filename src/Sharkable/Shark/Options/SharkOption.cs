using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace Sharkable;

/// <summary>
/// Configuration options for Sharkable. Passed via callback in <c>builder.Services.AddShark(opt => ...)</c>.
/// </summary>
public sealed class SharkOption : ISharkOption
{
    public const string Default = "Sharkable";
    /// <summary>
    /// API prefix for all endpoint groups. Default is <c>"api"</c>.
    /// </summary>
    public string ApiPrefix { get; set; } = "api";
    /// <summary>
    /// Whether to enable OpenAPI document generation and Scalar UI. Default is true.
    /// </summary>
    public bool UseOpenApi { get; set; } = true;
    /// <summary>
    /// Endpoint path format, default is camel case.
    /// </summary>
    public EndpointFormat Format { get; set; } = EndpointFormat.CamelCase;
    /// <summary>
    /// Indicates whether the current environment is in AOT mode.
    /// </summary>
    public bool AotMode => InternalShark.AotMode;
    /// <summary>
    /// Options for the global exception handler middleware.
    /// </summary>
    public ExceptionHandlerOptions ExceptionHandlerOptions { get; set; } = new();
    /// <summary>
    /// When <c>true</c>, endpoint return values that are not <see cref="IResult"/> are
    /// automatically wrapped in <see cref="UnifiedResult{T}"/>.
    /// Can also be set at runtime via <see cref="UseSharkOptions.EnableAutoWrap"/>.
    /// Default is <c>false</c> (opt-in).
    /// </summary>
    public bool EnableAutoWrap { get; set; } = false;
    /// <summary>
    /// Custom factory for the OpenAPI schema used by <see cref="EnableAutoWrap"/>.
    /// Takes the original response schema and returns the wrapped schema.
    /// When <c>null</c>, the default <see cref="UnifiedResult{T}"/> shape is used.
    /// Set this when using a custom <see cref="IUnifiedResultFactory"/> so that
    /// the generated OpenAPI document matches your actual response structure.
    /// </summary>
    public Func<OpenApiSchema, IOpenApiSchema>? WrapSchemaFactory { get; set; }
    /// <summary>
    /// When true, scan and register FluentValidation validators, and auto-validate
    /// endpoint parameters that have a registered <see cref="IValidator{T}"/>.
    /// Default is false (opt-in).
    /// </summary>
    public bool EnableValidation { get; set; } = false;
    /// <summary>
    /// Factory for creating unified result responses.
    /// Set this to use a custom result format.
    /// Defaults to <see cref="DefaultUnifiedResultFactory"/> producing <see cref="UnifiedResult{T}"/>.
    /// </summary>
    public IUnifiedResultFactory? UnifiedResultFactory { get; set; }
    /// <summary>
    /// Configures rate limiting policies. Calls <c>services.AddRateLimiter()</c> when set.
    /// Apply to endpoints via <c>.SharkRequireRateLimiting("policyName")</c>.
    /// For distributed rate limiting backed by Redis/PostgreSQL, use
    /// <see cref="ConfigureRateLimiting"/> instead.
    /// </summary>
    public Action<RateLimiterOptions>? RateLimiterConfigure { get; set; }
    /// <summary>
    /// Overrides the default <see cref="IIdempotencyStore"/> registration.
    /// When set, the factory is invoked with <see cref="IServiceProvider"/> to
    /// create the store instance. Use this to plug in a Redis or database-backed
    /// store. If <c>null</c>, <see cref="MemoryIdempotencyStore"/> is used as
    /// the default unless a custom implementation was already registered.
    /// </summary>
    public Func<IServiceProvider, IIdempotencyStore>? IdempotencyStoreFactory { get; set; }
    /// <summary>
    /// Overrides the default <see cref="IDistributedRateLimitStore"/> registration.
    /// When set, the factory is invoked with <see cref="IServiceProvider"/> to
    /// create the store instance. Use this to plug in a Redis or database-backed
    /// store. If <c>null</c>, <see cref="MemoryRateLimitStore"/> is used as
    /// the default unless a custom implementation was already registered.
    /// </summary>
    public Func<IServiceProvider, IDistributedRateLimitStore>? RateLimitStoreFactory { get; set; }
    /// <summary>
    /// Configures output caching policies. Calls <c>services.AddOutputCache()</c> when set.
    /// Apply to endpoints via <c>.SharkCacheOutput("policyName")</c>.
    /// </summary>
    public Action<OutputCacheOptions>? OutputCacheConfigure { get; set; }
    /// <summary>
    /// When <c>true</c>, registers health check services and maps <c>/healthz</c> endpoint.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableHealthChecks { get; set; } = false;
    /// <summary>
    /// Optional callback to add custom health checks. Called after
    /// <c>services.AddHealthChecks()</c>. The builder parameter is
    /// <see cref="IHealthChecksBuilder"/>.
    /// </summary>
    public Action<IHealthChecksBuilder>? HealthChecksConfigure { get; set; }
    /// <summary>
    /// Endpoint path for the health check endpoint. Default is <c>"/healthz"</c>.
    /// </summary>
    public string HealthCheckPath { get; set; } = "/healthz";
    /// <summary>
    /// When <c>true</c>, wires the idempotency middleware into the pipeline.
    /// Requests carrying an <c>Idempotency-Key</c> header on an unsafe HTTP
    /// method are deduplicated and replayed. Default is <c>false</c>.
    /// </summary>
    public bool EnableIdempotency { get; set; } = false;
    /// <summary>
    /// Configures CORS policies. Calls <c>services.AddCors()</c> when set, and wires <c>app.UseCors()</c>.
    /// </summary>
    public Action<CorsOptions>? CorsConfigure { get; set; }
    /// <summary>
    /// Valid API keys for the API Key authentication middleware.
    /// When set, requests must include a valid <c>X-Api-Key</c> header.
    /// </summary>
    public string[]? ApiKeys { get; set; }
    /// <summary>
    /// Request header name for API key authentication. Default is <c>"X-Api-Key"</c>.
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-Api-Key";
    /// <summary>
    /// Configures JWT Bearer authentication with opinionated defaults.
    /// Calls <c>services.AddAuthentication().AddJwtBearer()</c> with the given authority and audiences.
    /// </summary>
    /// <param name="authority">The trusted token authority (issuer) URL.</param>
    /// <param name="audiences">Accepted audience values.</param>
    /// <param name="configure">Optional additional <see cref="JwtBearerOptions"/> configuration.</param>
    public void ConfigureJwt(string authority, string[] audiences, Action<JwtBearerOptions>? configure = null)
    {
        JwtAuthority = authority;
        JwtAudiences = audiences;
        JwtConfigure = configure;
    }
    internal string? JwtAuthority { get; set; }
    internal string[]? JwtAudiences { get; set; }
    internal Action<JwtBearerOptions>? JwtConfigure { get; set; }
    /// <summary>
    /// When <c>true</c> (default), calls <c>services.AddAuthorization()</c> to register
    /// authorization services. Set to <c>false</c> to disable entirely.
    /// </summary>
    public bool EnableAuthorization { get; set; } = true;
    /// <summary>
    /// Optional callback to configure <see cref="Microsoft.AspNetCore.Authorization.AuthorizationOptions"/>.
    /// For example: add custom policies, default authorization policy, fallback policy, etc.
    /// Ignored when <see cref="EnableAuthorization"/> is <c>false</c>.
    /// </summary>
    public Action<AuthorizationOptions>? ConfigureAuthorization { get; set; }
    /// <summary>
    /// Configures the OpenAPI document generation options.
    /// </summary>
    public void ConfigureOpenApi(Action<OpenApiOptions>? options)
    {
        OpenApiConfigure = options;
    }
    /// <summary>
    /// Configures the Scalar API reference UI.
    /// </summary>
    public void ConfigureScalar(Action<ScalarOptions> configure)
    {
        ScalarConfigure = configure;
    }
    internal Action<ScalarOptions>? ScalarConfigure { get; set; }
    /// <summary>
    /// Configures structured request/response audit trail logging.
    /// When set, the <see cref="AuditTrailMiddleware"/> is wired into the pipeline.
    /// </summary>
    public void ConfigureAuditTrail(Action<AuditTrailOptions> configure)
    {
        var opt = new AuditTrailOptions();
        configure(opt);
        AuditTrailOptions = opt;
    }
    internal AuditTrailOptions? AuditTrailOptions { get; set; }
    /// <summary>
    /// Stores the idempotency options provided via <see cref="ConfigureIdempotency"/>.
    /// </summary>
    internal SharkIdempotencyOptions? IdempotencyOptions { get; set; }
    /// <summary>
    /// Stores the distributed rate limiter options provided via
    /// <see cref="ConfigureRateLimiting"/>.
    /// </summary>
    internal SharkRateLimiterOptions? RateLimitingOptions { get; set; }
    /// <summary>
    /// Configures AutoCrud (SqlSugar) options.
    /// </summary>
    public void ConfigureAutoCrud(Action<SqlSugarOptions>? options)
    {
        SqlSugarOptionsConfigure = options;
    }
    /// <summary>
    /// Configures the distributed rate limiter middleware. When set, a
    /// fixed-window rate limiter backed by <see cref="IDistributedRateLimitStore"/>
    /// is wired into the pipeline. Uses <see cref="MemoryRateLimitStore"/> by
    /// default; swap to Redis via <see cref="RateLimitStoreFactory"/>.
    /// </summary>
    /// <param name="configure">Callback to mutate the options instance.</param>
    public void ConfigureRateLimiting(Action<SharkRateLimiterOptions> configure)
    {
        var opt = new SharkRateLimiterOptions();
        configure(opt);
        RateLimitingOptions = opt;
    }
    /// <summary>
    /// Configures the idempotency middleware. Called only when
    /// <see cref="EnableIdempotency"/> is <c>true</c>.
    /// </summary>
    /// <param name="configure">Callback to mutate the options instance.</param>
    public void ConfigureIdempotency(Action<SharkIdempotencyOptions> configure)
    {
        var opt = new SharkIdempotencyOptions();
        configure(opt);
        IdempotencyOptions = opt;
    }
    /// <summary>
    /// Stores the OpenAPI configuration action provided via <see cref="ConfigureOpenApi"/>.
    /// </summary>
    internal Action<OpenApiOptions>? OpenApiConfigure { get; private set; }
    /// <summary>
    /// Stores the SqlSugar configuration action provided via <see cref="ConfigureAutoCrud"/>.
    /// </summary>
    internal Action<SqlSugarOptions>? SqlSugarOptionsConfigure{ get; private set; }
    /// <summary>
    /// Configures structured log field redaction.
    /// When set, <see cref="ILogger{T}"/> is replaced with a redacting wrapper.
    /// </summary>
    public void ConfigureRedactingLog(Action<RedactingLogOptions> configure)
    {
        var opt = new RedactingLogOptions();
        configure(opt);
        RedactingLogOptions = opt;
    }
    internal RedactingLogOptions? RedactingLogOptions { get; set; }
    /// <summary>
    /// Configures multi-tenant support.
    /// When set, the <see cref="ITenant"/> scoped service and resolution middleware are registered.
    /// </summary>
    public void ConfigureMultiTenant(Action<TenantOptions> configure)
    {
        var opt = new TenantOptions();
        configure(opt);
        TenantOptions = opt;
    }
    internal TenantOptions? TenantOptions { get; set; }
    /// <summary>
    /// Configures graceful shutdown behavior. When set, the application will:
    /// <list type="bullet">
    ///   <item>Mark <c>/healthz</c> as unhealthy on SIGTERM (load balancer traffic drain)</item>
    ///   <item>Reject new requests with 503 during shutdown</item>
    ///   <item>Wait for in-flight requests to complete up to <see cref="GracefulShutdownOptions.DrainTimeout"/></item>
    /// </list>
    /// </summary>
    public void ConfigureGracefulShutdown(Action<GracefulShutdownOptions> configure)
    {
        var opt = new GracefulShutdownOptions();
        configure(opt);
        GracefulShutdownOptions = opt;
    }
    internal GracefulShutdownOptions? GracefulShutdownOptions { get; set; }
    /// <summary>
    /// Configures distributed tracing. When set, a W3C <c>traceparent</c>
    /// middleware is wired at the front of the pipeline, creating an
    /// <see cref="System.Diagnostics.Activity"/> per request.
    /// Compatible with OpenTelemetry exporters (Jaeger, Zipkin, OTLP)
    /// out of the box.
    /// </summary>
    public void ConfigureTracing(Action<TracingOptions> configure)
    {
        var opt = new TracingOptions();
        configure(opt);
        TracingOptions = opt;
    }
    internal TracingOptions? TracingOptions { get; set; }
    /// <summary>
    /// Configures the built-in profiler panel. Exposes a
    /// <c>/_sharkable/profiler</c> (configurable) endpoint showing request
    /// counts, average latency, and top-N slowest recent requests.
    /// </summary>
    public void ConfigureProfiler(Action<ProfilerOptions> configure)
    {
        var opt = new ProfilerOptions();
        configure(opt);
        ProfilerOptions = opt;
    }
    internal ProfilerOptions? ProfilerOptions { get; set; }
    /// <summary>
    /// Enables automatic ETag generation and 304 Not Modified responses for
    /// GET/HEAD requests. Excludes health check, OpenAPI, and profiler paths
    /// by default.
    /// Default is <c>false</c>.
    /// </summary>
    public bool EnableETag { get; set; } = false;
    /// <summary>
    /// Options for ETag middleware (excluded paths, etc.).
    /// </summary>
    public ETagOptions? ETagOptions { get; set; }
    /// <summary>
    /// Pluggable error message localizer. When set, error messages from
    /// middleware (429, 503, etc.) are translated based on the
    /// <c>Accept-Language</c> request header. Set a factory to provide
    /// your implementation, or leave <c>null</c> for the default
    /// no-op (keys returned unchanged).
    /// </summary>
    public Func<IServiceProvider, IErrorLocalizer>? ErrorLocalizerFactory { get; set; }
    /// <summary>
    /// Default culture used when the <c>Accept-Language</c> header is missing or empty.
    /// Default is <c>"en"</c>.
    /// </summary>
    public string DefaultCulture { get; set; } = "en";
    /// <summary>
    /// Pluggable authorization interceptor. Runs before every endpoint.
    /// Return <c>null</c> to allow; return a non-null <see cref="IResult"/>
    /// to reject. Use for claim-based RBAC, tenant-scoped access, or custom
    /// API-key validation logic.
    /// </summary>
    public Func<IServiceProvider, IAuthorizationInterceptor>? AuthorizationInterceptorFactory { get; set; }
    /// <summary>
    /// Custom factory for ProblemDetails <c>type</c> URI.
    /// Receives the HTTP status code, returns the type URI.
    /// Default: <c>https://httpstatuses.com/{code}</c>.
    /// </summary>
    public Func<int, string>? ProblemDetailsTypeFactory { get; set; }
    /// <summary>
    /// Custom factory for ProblemDetails <c>title</c> string.
    /// Receives the HTTP status code, returns the title.
    /// Default: standard English titles (e.g. 400 → "Bad Request").
    /// </summary>
    public Func<int, string>? ProblemDetailsTitleFactory { get; set; }
    /// <summary>
    /// When <c>true</c>, all error responses (401, 403, 429, 500, 503, etc.)
    /// are written in RFC 7807 ProblemDetails format instead of the Sharkable
    /// unified result envelope. Default is <c>false</c>.
    /// </summary>
    public bool UseProblemDetails { get; set; } = false;
    /// <summary>
    /// When <c>true</c>, enables response compression via
    /// <c>services.AddResponseCompression()</c> and
    /// <c>app.UseResponseCompression()</c>. Skips already-compressed
    /// MIME types (images, videos). Default is <c>false</c>.
    /// </summary>
    public bool EnableResponseCompression { get; set; } = false;
    /// <summary>
    /// Overrides the default <see cref="ISagaStore"/> registration.
    /// When set, the factory is invoked with <see cref="IServiceProvider"/>
    /// to create the store instance. If <c>null</c>, <see cref="MemorySagaStore"/>
    /// is used as the default unless a custom implementation was already registered.
    /// </summary>
    public Func<IServiceProvider, ISagaStore>? SagaStoreFactory { get; set; }
    /// <summary>
    /// Overrides the default <see cref="ICronJobStore"/> registration.
    /// Same factory pattern as <see cref="SagaStoreFactory"/>.
    /// </summary>
    public Func<IServiceProvider, ICronJobStore>? CronJobStoreFactory { get; set; }
    /// <summary>
    /// Callback for registering cron jobs. Invoked during
    /// <c>AddShark()</c> so that jobs can be registered before the
    /// hosted service starts.
    /// </summary>
    public Action<ICronScheduler>? ConfigureCronJobs { get; set; }
    /// <summary>
    /// Pre-filled JWT token in the Scalar UI authentication dialog.
    /// When set, replaces the default placeholder in Scalar's Bearer auth.
    /// </summary>
    public string? ScalarJwtToken { get; set; }
    /// <summary>
    /// Pre-filled API key value in the Scalar UI authentication dialog.
    /// When set, replaces the default placeholder in Scalar's API Key auth.
    /// </summary>
    public string? ScalarApiKeyValue { get; set; }
    /// <summary>
    /// Regex pattern for stripping common suffixes from endpoint group names.
    /// Default strips <c>Endpoint</c>, <c>Service</c>, <c>Controller</c>, etc.
    /// </summary>
    public string GroupNameSuffixPattern { get; set; } = "(endpoint|service|services|controller|controllers|apicontroller)(?=V?\\d*$)";
    /// <summary>
    /// Regex pattern for converting version prefixes in endpoint URLs.
    /// Default converts <c>V1</c> → <c>@1</c>.
    /// </summary>
    public string VersionFormatPattern { get; set; } = @"V(\d+)";
    /// <summary>
    /// Replacement string for <see cref="VersionFormatPattern"/>.
    /// Default is <c>@$1</c>.
    /// </summary>
    public string VersionFormatReplacement { get; set; } = @"@$1";
}

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
    /// </summary>
    public Action<RateLimiterOptions>? RateLimiterConfigure { get; set; }
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
    /// Configures AutoCrud (SqlSugar) options.
    /// </summary>
    public void ConfigureAutoCrud(Action<SqlSugarOptions>? options)
    {
        SqlSugarOptionsConfigure = options;
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
}

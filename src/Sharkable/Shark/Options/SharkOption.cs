using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

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
    /// Whether to enable Swagger/OpenAPI document generation. Default is true.
    /// </summary>
    public bool UseSwaggerDoc { get; set; } = true;
    /// <summary>
    /// endpoint path format, default is camel case
    /// </summary>
    public EndpointFormat Format { get; set; } = EndpointFormat.CamelCase;
    /// <summary>
    /// a property indicates current environment is in aot mode or not
    /// </summary>
    public bool AotMode => InternalShark.AotMode;
    /// <summary>
    /// Options for the global exception handler middleware.
    /// </summary>
    public ExceptionHandlerOptions ExceptionHandlerOptions { get; set; } = new();
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
    /// configure swagger gen
    /// </summary>
    /// <param name="options"></param>
    public void ConfigureSwaggerGen(Action<SwaggerGenOptions>? options)
    {
        SwaggerGenConfigure = options;
    }
    /// <summary>
    /// configure swagger gen
    /// </summary>
    /// <param name="options"></param>
    public void ConfigureAutoCrud(Action<SqlSugarOptions>? options)
    {
        SqlSugarOptionsConfigure = options;
    }
    /// <summary>
    /// configure swagger ui options
    /// </summary>
    /// <param name="options"></param>
    public void ConfigureSwaggerUi(Action<SwaggerUIOptions>? options)
    {
        SwaggerUIOptionsConfigure = options;
    }
    /// <summary>
    /// get the swagger gen configuration
    /// </summary>
    internal static Action<SwaggerGenOptions>? SwaggerGenConfigure{ get; private set; }
    /// <summary>
    /// get the sqlsugar configuration
    /// </summary>
    internal static Action<SqlSugarOptions>? SqlSugarOptionsConfigure{ get; private set; }
    /// <summary>
    /// get the swagger Ui configuration
    /// </summary>
    internal static Action<SwaggerUIOptions>? SwaggerUIOptionsConfigure { get; private set; }
}

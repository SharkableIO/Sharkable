using Microsoft.AspNetCore.OpenApi;

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
    /// Configures the OpenAPI document generation options.
    /// </summary>
    public void ConfigureOpenApi(Action<OpenApiOptions>? options)
    {
        OpenApiConfigure = options;
    }
    /// <summary>
    /// Configures AutoCrud (SqlSugar) options.
    /// </summary>
    public void ConfigureAutoCrud(Action<SqlSugarOptions>? options)
    {
        SqlSugarOptionsConfigure = options;
    }
    /// <summary>
    /// Stores the OpenAPI configuration action provided via <see cref="ConfigureOpenApi"/>.
    /// </summary>
    internal static Action<OpenApiOptions>? OpenApiConfigure { get; private set; }
    /// <summary>
    /// Stores the SqlSugar configuration action provided via <see cref="ConfigureAutoCrud"/>.
    /// </summary>
    internal static Action<SqlSugarOptions>? SqlSugarOptionsConfigure{ get; private set; }
}

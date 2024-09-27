using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Sharkable;

public sealed class SharkOption : ISharkOption
{
    public const string Default = "Sharkable";
    /// <summary>
    /// set up the default api path,default is "api"
    /// </summary>
    public string ApiPrefix { get; set; } = "api";
    /// <summary>
    /// decide wheather to show the swargger document or not
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

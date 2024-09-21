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
}

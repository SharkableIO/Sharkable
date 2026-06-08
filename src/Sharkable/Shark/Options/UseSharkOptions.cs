using Swashbuckle.AspNetCore.Swagger;

namespace Sharkable;

public sealed class UseSharkOptions : ISharkOption
{
    /// <summary>
    /// When true (default), UseShark wires the global exception handler middleware.
    /// </summary>
    public bool EnableExceptionHandler { get; set; } = true;
    /// <summary>
    /// When true, endpoint return values that are not <see cref="IResult"/> are
    /// automatically wrapped in <see cref="UnifiedResult{T}"/>.
    /// Default is false (opt-in).
    /// </summary>
    public bool EnableAutoWrap { get; set; } = false;
    public void ConfigureSwaggerOptions(Action<SwaggerOptions>? options)
    {
        UseSwaggerConfigure = options;
    }
    public static Action<SwaggerOptions>? UseSwaggerConfigure { get; private set; }
}

using Swashbuckle.AspNetCore.Swagger;

namespace Sharkable;

public sealed class UseSharkOptions : ISharkOption
{
    public void ConfigureSwaggerOptions(Action<SwaggerOptions>? options)
    {
        UseSwaggerConfigure = options;
    }
    public static Action<SwaggerOptions>? UseSwaggerConfigure { get; private set; }
}

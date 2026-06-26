using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Sharkable;

/// <summary>
/// Extension methods for registering structured log redaction via <see cref="RedactingLogger{T}"/>.
/// </summary>
public static class LoggingExtensions
{
    /// <summary>
    /// Replaces the default <see cref="ILogger{T}"/> with a redacting wrapper that
    /// masks configured sensitive field values in structured log output.
    /// </summary>
    /// <param name="builder">The <see cref="ILoggingBuilder"/> to configure.</param>
    /// <param name="configure">Optional callback to configure redacted fields and replacement text.</param>
    public static ILoggingBuilder AddRedactingFormatter(this ILoggingBuilder builder, Action<RedactingLogOptions>? configure = null)
    {
        if (configure != null)
        {
            var options = new RedactingLogOptions();
            configure(options);
            builder.Services.AddSingleton(options);
        }
        else
        {
            builder.Services.TryAddSingleton<RedactingLogOptions>();
        }

        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(RedactingLogger<>)));
        return builder;
    }

    internal static ILoggingBuilder AddRedactingFormatter(this ILoggingBuilder builder, RedactingLogOptions options)
    {
        builder.Services.AddSingleton(options);
        builder.Services.Replace(ServiceDescriptor.Singleton(typeof(ILogger<>), typeof(RedactingLogger<>)));
        return builder;
    }
}

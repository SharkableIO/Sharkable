using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class RedactingLogger<T> : ILogger<T>
{
    private readonly RedactingLogger _logger;

    public RedactingLogger(ILoggerFactory factory, RedactingLogOptions options)
    {
        var inner = factory.CreateLogger(typeof(T).FullName ?? typeof(T).Name);
        _logger = new RedactingLogger(inner, options);
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        => _logger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _logger.BeginScope(state);
}

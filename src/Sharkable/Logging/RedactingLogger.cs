using Microsoft.Extensions.Logging;

namespace Sharkable;

internal sealed class RedactingLogger : ILogger
{
    private readonly ILogger _inner;
    private readonly RedactingLogOptions _options;
    private readonly HashSet<string> _redactKeys;

    public RedactingLogger(ILogger inner, RedactingLogOptions options)
    {
        _inner = inner;
        _options = options;
        _redactKeys = new HashSet<string>(options.RedactFields, StringComparer.OrdinalIgnoreCase);
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (_redactKeys.Count == 0)
        {
            _inner.Log(logLevel, eventId, state, exception, formatter);
            return;
        }

        if (state is IReadOnlyList<KeyValuePair<string, object?>> pairs)
        {
            var needsRedaction = false;
            foreach (var kvp in pairs)
            {
                if (kvp.Key.Length > 0 && kvp.Key[0] != '{' && _redactKeys.Contains(kvp.Key))
                {
                    needsRedaction = true;
                    break;
                }
            }

            if (needsRedaction)
            {
                _inner.Log(logLevel, eventId, state, exception, (s, ex) =>
                {
                    var formatted = formatter(s, ex);
                    if (string.IsNullOrEmpty(formatted))
                        return formatted;

                    var replacements = new List<(string oldValue, string newValue)>();
                    foreach (var kvp in pairs)
                    {
                        if (_redactKeys.Contains(kvp.Key) && kvp.Value != null)
                        {
                            var valStr = kvp.Value.ToString();
                            if (!string.IsNullOrEmpty(valStr) && formatted.Contains(valStr, StringComparison.Ordinal))
                                replacements.Add((valStr, _options.RedactWith));
                        }
                    }

                    if (replacements.Count == 0)
                        return formatted;

                    replacements.Sort((a, b) => b.oldValue.Length.CompareTo(a.oldValue.Length));
                    foreach (var (oldVal, newVal) in replacements)
                        formatted = formatted.Replace(oldVal, newVal);

                    return formatted;
                });
                return;
            }
        }

        _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _inner.BeginScope(state);
}

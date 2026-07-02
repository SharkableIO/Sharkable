using System.Text;
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

                    // SHARK-SEC-M006: replace structurally by reconstructing
                    // the formatted message from a key/value rewrite rather
                    // than substring search. Substring search over-redacts
                    // (password="p4ss" + unrelated "the password is p4ss"
                    // both get rewritten) and under-redacts (the value may
                    // be JSON-escaped or split across the formatter output).
                    return RedactByKey(formatted, pairs);
                });
                return;
            }
        }

        _inner.Log(logLevel, eventId, state, exception, formatter);
    }

    /// <summary>
    /// SHARK-SEC-M006: rebuild the formatted message using the structured
    /// state. The formatter's output is <c>{OriginalFormat}</c> with each
    /// <c>{Key}</c> placeholder substituted from the matching KV pair; we
    /// rewrite the substituted values in place so we never run a substring
    /// match against unrelated text.
    /// </summary>
    private string RedactByKey(
        string formatted,
        IReadOnlyList<KeyValuePair<string, object?>> pairs)
    {
        var originalFormat = pairs.FirstOrDefault(p => p.Key == "{OriginalFormat}").Value as string;
        if (string.IsNullOrEmpty(originalFormat))
        {
            // Fall back to safe substring-based replacement only when the
            // formatter didn't expose the original template. To reduce
            // over-redaction, only consider exact key=value pairs that the
            // caller explicitly listed — never partial matches.
            var sb = new StringBuilder(formatted);
            foreach (var kvp in pairs)
            {
                if (!_redactKeys.Contains(kvp.Key) || kvp.Value == null) continue;
                var valStr = kvp.Value.ToString();
                if (string.IsNullOrEmpty(valStr)) continue;
                sb.Replace(valStr, _options.RedactWith);
            }
            return sb.ToString();
        }

        // Walk the template, copying literal characters and substituting
        // values for each {Key} placeholder. We only redact the substituted
        // value of redacted keys — never touch unrelated text.
        var output = new StringBuilder(originalFormat.Length + 32);
        var i = 0;
        while (i < originalFormat.Length)
        {
            var ch = originalFormat[i];
            if (ch == '{')
            {
                var close = originalFormat.IndexOf('}', i + 1);
                if (close > 0)
                {
                    var key = originalFormat.Substring(i + 1, close - i - 1);
                    var separator = key.IndexOf(':');
                    if (separator >= 0) key = key[..separator];
                    var pair = pairs.FirstOrDefault(p =>
                        string.Equals(p.Key, key, StringComparison.Ordinal));
                    if (pair.Value != null)
                    {
                        if (_redactKeys.Contains(pair.Key))
                            output.Append(_options.RedactWith);
                        else
                            output.Append(pair.Value);
                    }
                    else
                    {
                        // placeholder for a key we don't have; copy template slice as-is
                        output.Append(originalFormat, i, close - i + 1);
                    }
                    i = close + 1;
                    continue;
                }
            }
            output.Append(ch);
            i++;
        }
        return output.ToString();
    }

    public bool IsEnabled(LogLevel logLevel) => _inner.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => _inner.BeginScope(state);
}

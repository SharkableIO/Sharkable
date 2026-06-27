using Microsoft.Extensions.Logging;

namespace Sharkable.Tests;

public sealed class ListLogger : ILogger
{
    public List<string> Messages { get; } = [];
    public LogLevel EnabledLevel { get; set; } = LogLevel.Trace;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Messages.Add(formatter(state, exception));
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= EnabledLevel;
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}

public class RedactingLoggerTests
{
    private static RedactingLogOptions DefaultOptions() => new()
    {
        RedactFields = ["password", "secret", "token"],
        RedactWith = "***REDACTED***",
    };

    [Fact]
    public void Redacts_Password_Value_From_Formatted_Message()
    {
        var inner = new ListLogger();
        var options = DefaultOptions();
        var logger = new RedactingLogger(inner, options);

        var state = new List<KeyValuePair<string, object?>>
        {
            new("{OriginalFormat}", "password={password}"),
            new("password", "my-secret-pass"),
        };

        logger.Log(LogLevel.Information, 0, state, null, (s, _) =>
        {
            var pairs = (IReadOnlyList<KeyValuePair<string, object?>>)s;
            return pairs[0].Value?.ToString()?.Replace("{password}", pairs[1].Value?.ToString()) ?? "";
        });

        var msg = Assert.Single(inner.Messages);
        Assert.DoesNotContain("my-secret-pass", msg);
        Assert.Contains("***REDACTED***", msg);
    }

    [Fact]
    public void Does_Not_Redact_NonConfigured_Field()
    {
        var inner = new ListLogger();
        var options = DefaultOptions();
        var logger = new RedactingLogger(inner, options);

        var state = new List<KeyValuePair<string, object?>>
        {
            new("{OriginalFormat}", "username={username}"),
            new("username", "alice"),
        };

        logger.Log(LogLevel.Information, 0, state, null, (s, _) =>
        {
            var pairs = (IReadOnlyList<KeyValuePair<string, object?>>)s;
            return pairs[0].Value?.ToString()?.Replace("{username}", pairs[1].Value?.ToString()) ?? "";
        });

        var msg = Assert.Single(inner.Messages);
        Assert.Contains("alice", msg);
    }

    [Fact]
    public void IsEnabled_Delegates_To_Inner()
    {
        var inner = new ListLogger { EnabledLevel = LogLevel.Warning };
        var logger = new RedactingLogger(inner, DefaultOptions());
        Assert.False(logger.IsEnabled(LogLevel.Information));
        Assert.True(logger.IsEnabled(LogLevel.Warning));
    }

    [Fact]
    public void Skips_Redaction_When_No_RedactKeys()
    {
        var inner = new ListLogger();
        var options = new RedactingLogOptions { RedactFields = [], RedactWith = "XXX" };
        var logger = new RedactingLogger(inner, options);

        var state = new List<KeyValuePair<string, object?>>
        {
            new("{OriginalFormat}", "password={password}"),
            new("password", "my-secret-pass"),
        };

        logger.Log(LogLevel.Information, 0, state, null, (s, _) =>
        {
            var pairs = (IReadOnlyList<KeyValuePair<string, object?>>)s;
            return pairs[0].Value?.ToString()?.Replace("{password}", pairs[1].Value?.ToString()) ?? "";
        });

        var msg = Assert.Single(inner.Messages);
        Assert.Contains("my-secret-pass", msg);
    }
}

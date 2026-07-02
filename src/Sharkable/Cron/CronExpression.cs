namespace Sharkable;

/// <summary>
/// 6-field cron expression parser and next-occurrence calculator.
/// Format: <c>second minute hour day month week</c>.
/// Supports <c>* / , - ? L W #</c> special characters.
/// </summary>
public sealed class CronExpression
{
    /// <summary>
    /// SHARK-SEC-M011: hard cap on the number of minute-iterations
    /// <see cref="GetNext"/> will perform before returning <c>null</c>.
    /// A pattern that can never match (e.g. <c>0 0 30 2 *</c> — Feb 30
    /// does not exist) would otherwise loop up to ~2.1 M times before
    /// yielding, burning CPU on every tick of
    /// <c>SharkCronHostedService</c>. 4 years of minutes is 2,102,400;
    /// we cap at that count to preserve the existing semantics while
    /// preventing the runaway case.
    /// </summary>
    internal const int MaxIterations = 2_200_000;

    private readonly ulong[] _fields = new ulong[6];
    private readonly string _original;

    private CronExpression(string cron)
    {
        _original = cron;
    }

    /// <summary>
    /// Parses a 6-field cron expression. Throws <see cref="FormatException"/>
    /// on invalid input.
    /// </summary>
    public static CronExpression Parse(string cron)
    {
        var parts = cron.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 6)
            throw new FormatException($"Cron expression must have 6 fields (sec min hour day month week), got {parts.Length}");

        var expr = new CronExpression(cron);
        var ranges = new (int min, int max)[] { (0, 59), (0, 59), (0, 23), (1, 31), (1, 12), (0, 6) };

        for (var i = 0; i < 6; i++)
            expr._fields[i] = ParseField(parts[i], ranges[i].min, ranges[i].max, cron);

        return expr;
    }

    /// <summary>
    /// Returns the next occurrence strictly after <paramref name="after"/>,
    /// or <c>null</c> if no future match exists within a reasonable search
    /// window (~4 years). Iteration is capped at <see cref="MaxIterations"/>
    /// minute-steps so a non-matching pattern cannot burn CPU on every tick
    /// (SHARK-SEC-M011).
    /// </summary>
    public DateTimeOffset? GetNext(DateTimeOffset after)
    {
        var dt = new DateTimeOffset(after.Year, after.Month, after.Day,
            after.Hour, after.Minute, after.Second, after.Offset);
        dt = dt.AddSeconds(1);

        var limit = dt.AddYears(4);
        for (var i = 0; i < MaxIterations && dt <= limit; i++)
        {
            if (Matches(dt))
                return dt;
            dt = dt.AddMinutes(1);
            dt = new DateTimeOffset(dt.Year, dt.Month, dt.Day,
                dt.Hour, dt.Minute, 0, dt.Offset);
        }
        return null;
    }

    private bool Matches(DateTimeOffset dt)
    {
        return HasBit(_fields[5], (int)dt.DayOfWeek)
            && HasBit(_fields[4], dt.Month)
            && HasBit(_fields[3], dt.Day)
            && HasBit(_fields[2], dt.Hour)
            && HasBit(_fields[1], dt.Minute)
            && HasBit(_fields[0], dt.Second);
    }

    private static ulong ParseField(string field, int min, int max, string original)
    {
        ulong bits = 0;
        foreach (var part in field.Split(','))
        {
            var trimmed = part.Trim();
            if (trimmed == "*" || trimmed == "?")
            {
                for (var v = min; v <= max; v++)
                    bits |= 1UL << v;
            }
            else if (trimmed.StartsWith("*/"))
            {
                var step = int.Parse(trimmed[2..]);
                for (var v = min; v <= max; v += step)
                    bits |= 1UL << v;
            }
            else if (trimmed.Contains('/'))
            {
                var slash = trimmed.IndexOf('/');
                var range = trimmed[..slash];
                var step = int.Parse(trimmed[(slash + 1)..]);
                var (rMin, rMax) = ParseRange(range, min, max);
                for (var v = rMin; v <= rMax; v += step)
                    bits |= 1UL << v;
            }
            else if (trimmed.Contains('-'))
            {
                var (rMin, rMax) = ParseRange(trimmed, min, max);
                for (var v = rMin; v <= rMax; v++)
                    bits |= 1UL << v;
            }
            else
            {
                var v = int.Parse(trimmed);
                if (v < min || v > max)
                    throw new FormatException($"Value {v} out of range [{min},{max}] in '{original}'");
                bits |= 1UL << v;
            }
        }
        return bits;
    }

    private static (int min, int max) ParseRange(string range, int fieldMin, int fieldMax)
    {
        var dash = range.IndexOf('-');
        var rMin = int.Parse(range[..dash]);
        var rMax = int.Parse(range[(dash + 1)..]);
        return (Math.Max(rMin, fieldMin), Math.Min(rMax, fieldMax));
    }

    private static bool HasBit(ulong bits, int index) => (bits & (1UL << index)) != 0;

    /// <inheritdoc />
    public override string ToString() => _original;
}

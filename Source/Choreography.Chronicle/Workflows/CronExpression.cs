// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Choreography.Chronicle.Workflows;

/// <summary>
/// A five-field cron expression - minute, hour, day of month, month and day of week - evaluated in UTC.
/// </summary>
/// <remarks>
/// Each field is <c lang="csharp">*</c>, a number, a range <c lang="csharp">a-b</c>, a step <c lang="csharp">*/n</c> or <c lang="csharp">a-b/n</c>, or a comma separated list of those.
/// Day of week runs from 0 (Sunday) to 6, and 7 is Sunday too. When both day of month and day of week are restricted, either matching is enough, as in classic cron.
/// </remarks>
public sealed class CronExpression
{
    readonly HashSet<int> _minutes;
    readonly HashSet<int> _hours;
    readonly HashSet<int> _daysOfMonth;
    readonly HashSet<int> _months;
    readonly HashSet<int> _daysOfWeek;
    readonly bool _anyDayOfMonth;
    readonly bool _anyDayOfWeek;

    CronExpression(string text, string[] fields)
    {
        Text = text;
        _minutes = Parse(fields[0], 0, 59);
        _hours = Parse(fields[1], 0, 23);
        _daysOfMonth = Parse(fields[2], 1, 31);
        _months = Parse(fields[3], 1, 12);
        _daysOfWeek = [.. Parse(fields[4], 0, 7).Select(day => day % 7)];
        _anyDayOfMonth = fields[2] == "*";
        _anyDayOfWeek = fields[4] == "*";
    }

    /// <summary>
    /// Gets the expression as written.
    /// </summary>
    public string Text { get; }

    bool RequiresBothDays => _anyDayOfMonth || _anyDayOfWeek;

    /// <summary>
    /// Parses an expression.
    /// </summary>
    /// <param name="text">The expression, as five space separated fields.</param>
    /// <returns>The <see cref="CronExpression"/>.</returns>
    /// <exception cref="InvalidCronExpression">The text is not a valid expression.</exception>
    public static CronExpression Parse(string text)
    {
        var fields = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (fields.Length != 5)
        {
            throw new InvalidCronExpression(text, "expected five fields");
        }

        try
        {
            return new(text, fields);
        }
        catch (Exception exception) when (exception is FormatException or ArgumentOutOfRangeException)
        {
            throw new InvalidCronExpression(text, exception.Message);
        }
    }

    /// <summary>
    /// Gets whether the expression matches a minute.
    /// </summary>
    /// <param name="time">The time - its seconds are ignored.</param>
    /// <returns>True when it matches, false when not.</returns>
    public bool Matches(DateTimeOffset time)
    {
        var utc = time.UtcDateTime;
        if (!MatchesTime(utc))
        {
            return false;
        }

        var dayOfMonth = _daysOfMonth.Contains(utc.Day);
        var dayOfWeek = _daysOfWeek.Contains((int)utc.DayOfWeek);
        return RequiresBothDays ? dayOfMonth && dayOfWeek : dayOfMonth || dayOfWeek;
    }

    /// <summary>
    /// Gets the minutes the expression matched after one time up to and including another.
    /// </summary>
    /// <param name="after">The time to start after, exclusive.</param>
    /// <param name="until">The time to stop at, inclusive.</param>
    /// <returns>The matching minutes, oldest first.</returns>
    public IEnumerable<DateTimeOffset> OccurrencesBetween(DateTimeOffset after, DateTimeOffset until)
    {
        var minute = new DateTimeOffset(after.UtcDateTime.Year, after.UtcDateTime.Month, after.UtcDateTime.Day, after.UtcDateTime.Hour, after.UtcDateTime.Minute, 0, TimeSpan.Zero).AddMinutes(1);
        for (; minute <= until; minute = minute.AddMinutes(1))
        {
            if (Matches(minute))
            {
                yield return minute;
            }
        }
    }

    /// <inheritdoc/>
    public override string ToString() => Text;

    bool MatchesTime(DateTime utc) => _minutes.Contains(utc.Minute) && _hours.Contains(utc.Hour) && _months.Contains(utc.Month);

    static HashSet<int> Parse(string field, int minimum, int maximum)
    {
        var values = new HashSet<int>();
        foreach (var part in field.Split(','))
        {
            var stepAt = part.IndexOf('/', StringComparison.Ordinal);
            var step = ParseStep(field, part, stepAt);
            var range = stepAt < 0 ? part : part[..stepAt];
            var (from, to) = ParseRange(range, stepAt >= 0, minimum, maximum);
            if (IsOutside(from, to, minimum, maximum))
            {
                throw new ArgumentOutOfRangeException(nameof(field), $"'{part}' is outside {minimum}-{maximum}");
            }

            for (var value = from; value <= to; value += step)
            {
                values.Add(value);
            }
        }

        return values;
    }

    static int ParseStep(string field, string part, int stepAt)
    {
        var step = stepAt < 0 ? 1 : int.Parse(part[(stepAt + 1)..], CultureInfo.InvariantCulture);
        if (step < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(field), "a step must be at least one");
        }

        return step;
    }

    static (int From, int To) ParseRange(string range, bool stepped, int minimum, int maximum)
    {
        var dashAt = range.IndexOf('-', StringComparison.Ordinal);
        return range switch
        {
            "*" => (minimum, maximum),
            _ when dashAt >= 0 => (int.Parse(range[..dashAt], CultureInfo.InvariantCulture), int.Parse(range[(dashAt + 1)..], CultureInfo.InvariantCulture)),
            _ => (int.Parse(range, CultureInfo.InvariantCulture), stepped ? maximum : int.Parse(range, CultureInfo.InvariantCulture))
        };
    }

    static bool IsOutside(int from, int to, int minimum, int maximum) => from < minimum || to > maximum || from > to;
}

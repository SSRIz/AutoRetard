#nullable enable
using System.Collections.Generic;

namespace AutoRetard;

/// <summary>
/// One selectable time unit with its conversion factor and allowed range.
/// ToString() returns the label so a ComboBox shows it directly.
/// </summary>
internal sealed record TimeUnitOption(
    string Label,
    string SingularLabel,
    int MillisecondsPerUnit,
    decimal Min,
    decimal Max,
    int MaxDecimals)
{
    public override string ToString() => Label;

    /// <summary>The fixed list of units, in dropdown order.</summary>
    public static IReadOnlyList<TimeUnitOption> All { get; } = new List<TimeUnitOption>
    {
        new("Milliseconds", "Millisecond", 1,         InputValidator.MinIntervalMs, InputValidator.MaxIntervalMs, 0),
        new("Seconds",      "Second",      1_000,     0.01m, 3_600m, 3),
        new("Minutes",      "Minute",      60_000,    0.01m, 60m,    3),
        new("Hours",        "Hour",        3_600_000, 0.01m, 1m,     3),
    };
}
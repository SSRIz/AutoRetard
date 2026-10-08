#nullable enable
using System;
using System.Globalization;
using System.Windows.Forms;

namespace AutoRetard;

/// <summary>A validated interval: exact whole milliseconds plus text for display.</summary>
internal readonly record struct IntervalSetting(int Milliseconds, string DisplayText);

/// <summary>Pure validation rules for the user's input. No UI code lives here.</summary>
internal static class InputValidator
{
    public const int MinIntervalMs = 10;
    public const int MaxIntervalMs = 3_600_000; // 1 hour

    /// <summary>Culture-independent number text, e.g. 3600 -> "3,600", 1.50 -> "1.5".</summary>
    public static string FormatNumber(decimal value) =>
        value.ToString("#,##0.###", CultureInfo.InvariantCulture);

    /// <summary>Two-line range hint for the selected unit.</summary>
    public static string DescribeRange(TimeUnitOption unit)
    {
        string rangeLine =
            $"Allowed: {FormatNumber(unit.Min)} to {FormatNumber(unit.Max)} {unit.Label.ToLowerInvariant()}";
        string formatLine = unit.MaxDecimals == 0
            ? "Whole numbers only."
            : $"Up to {unit.MaxDecimals} decimal places. Use a dot: 1.5";
        return rangeLine + Environment.NewLine + formatLine;
    }

    public static bool TryParseInterval(
        string? text,
        TimeUnitOption unit,
        out IntervalSetting setting,
        out string error)
    {
        setting = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "Enter an interval value.";
            return false;
        }

        string trimmed = text.Trim();

        // Character check first: only ASCII digits and at most one dot can ever reach the parser.
        // This rejects signs, letters (NaN, Infinity, e-notation), spaces and thousands separators.
        int dotCount = 0;
        int decimals = 0;
        foreach (char c in trimmed)
        {
            if (c >= '0' && c <= '9')
            {
                if (dotCount > 0)
                {
                    decimals++;
                }
            }
            else if (c == '.')
            {
                dotCount++;
            }
            else if (c == ',')
            {
                error = "Use a dot (.) for decimals, not a comma.";
                return false;
            }
            else
            {
                error = "Use digits 0-9 and an optional dot only.";
                return false;
            }
        }

        if (dotCount > 1 || trimmed[0] == '.' || trimmed[^1] == '.')
        {
            error = "Invalid number format. Examples: 5, 1.5, 0.25";
            return false;
        }

        if (decimals > unit.MaxDecimals)
        {
            error = unit.MaxDecimals == 0
                ? $"{unit.Label} must be a whole number (no decimal point)."
                : $"Use at most {unit.MaxDecimals} decimal places.";
            return false;
        }

        // decimal is exact (no binary rounding) and cannot represent NaN or Infinity.
        if (!decimal.TryParse(trimmed, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal value))
        {
            error = "That number is too large.";
            return false;
        }

        if (value <= 0m)
        {
            error = "Interval must be greater than zero.";
            return false;
        }

        if (value < unit.Min || value > unit.Max)
        {
            error = $"Value must be between {FormatNumber(unit.Min)} and {FormatNumber(unit.Max)} " +
                    $"{unit.Label.ToLowerInvariant()}.";
            return false;
        }

        // With at most 3 decimals this is always a whole number of ms. Checked defensively anyway.
        decimal totalMs = value * unit.MillisecondsPerUnit;
        if (totalMs != decimal.Truncate(totalMs) || totalMs < MinIntervalMs || totalMs > MaxIntervalMs)
        {
            error = $"Interval must be between {FormatNumber(MinIntervalMs)} and {FormatNumber(MaxIntervalMs)} ms.";
            return false;
        }

        string unitLabel = value == 1m ? unit.SingularLabel : unit.Label;
        setting = new IntervalSetting((int)totalMs, $"{FormatNumber(value)} {unitLabel}");
        return true;
    }

    public static bool TryGetUnit(object? selectedItem, out TimeUnitOption unit, out string error)
    {
        if (selectedItem is TimeUnitOption option)
        {
            unit = option;
            error = string.Empty;
            return true;
        }

        unit = TimeUnitOption.All[0];
        error = "Choose a time unit from the list.";
        return false;
    }

    public static bool TryGetKey(object? selectedItem, out KeyOption key, out string error)
    {
        if (selectedItem is KeyOption option)
        {
            key = option;
            error = string.Empty;
            return true;
        }

        key = new KeyOption("None", Keys.None);
        error = "Choose a key from the list.";
        return false;
    }
}
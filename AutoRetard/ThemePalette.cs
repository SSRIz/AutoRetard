#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace AutoRetard;

/// <summary>
/// A complete set of colors for one theme, grouped by role.
/// MainForm never contains a color value; it only asks the palette for a role
/// (for example <c>StartBack</c>), so a color is defined in exactly one place per theme.
/// </summary>
internal sealed record ThemePalette(
    // Form and text
    Color FormBack,
    Color Text,
    Color HintText,
    Color ErrorText,
    Color Divider,
    // Inputs (interval text box and the two drop-down lists)
    Color InputBack,
    Color InputText,
    Color InputReadOnlyBack,
    // Disabled controls
    Color DisabledText,
    Color ButtonDisabledBack,
    // Neutral buttons (the unselected Light / Dark switch)
    Color ButtonBack,
    Color ButtonText,
    Color ButtonBorder,
    // Accent (the selected Light / Dark switch)
    Color Accent,
    Color AccentText,
    // Status banner
    Color StatusStoppedBack,
    Color StatusStoppedText,
    Color StatusRunningBack,
    Color StatusRunningText,
    // Action buttons
    Color StartBack,
    Color StartText,
    Color StopBack,
    Color StopText)
{
    public static ThemePalette Light { get; } = new(
        FormBack: Color.FromArgb(243, 243, 243),
        Text: Color.FromArgb(32, 32, 32),
        HintText: Color.FromArgb(96, 96, 96),
        ErrorText: Color.FromArgb(176, 32, 32),
        Divider: Color.FromArgb(222, 222, 222),
        InputBack: Color.White,
        InputText: Color.FromArgb(32, 32, 32),
        InputReadOnlyBack: Color.FromArgb(232, 232, 232),
        DisabledText: Color.FromArgb(110, 110, 110),
        ButtonDisabledBack: Color.FromArgb(236, 236, 236),
        ButtonBack: Color.FromArgb(225, 225, 225),
        ButtonText: Color.FromArgb(32, 32, 32),
        ButtonBorder: Color.FromArgb(160, 160, 160),
        Accent: Color.FromArgb(0, 95, 184),
        AccentText: Color.White,
        StatusStoppedBack: Color.FromArgb(225, 225, 225),
        StatusStoppedText: Color.FromArgb(32, 32, 32),
        StatusRunningBack: Color.FromArgb(27, 120, 60),
        StatusRunningText: Color.White,
        StartBack: Color.FromArgb(27, 120, 60),
        StartText: Color.White,
        StopBack: Color.FromArgb(176, 32, 32),
        StopText: Color.White);

    public static ThemePalette Dark { get; } = new(
        FormBack: Color.FromArgb(32, 32, 32),
        Text: Color.FromArgb(235, 235, 235),
        HintText: Color.FromArgb(170, 170, 170),
        ErrorText: Color.FromArgb(255, 138, 128),
        Divider: Color.FromArgb(62, 62, 62),
        InputBack: Color.FromArgb(45, 45, 45),
        InputText: Color.FromArgb(240, 240, 240),
        InputReadOnlyBack: Color.FromArgb(38, 38, 38),
        DisabledText: Color.FromArgb(150, 150, 150),
        ButtonDisabledBack: Color.FromArgb(42, 42, 42),
        ButtonBack: Color.FromArgb(58, 58, 58),
        ButtonText: Color.FromArgb(240, 240, 240),
        ButtonBorder: Color.FromArgb(110, 110, 110),
        Accent: Color.FromArgb(40, 110, 190),
        AccentText: Color.White,
        StatusStoppedBack: Color.FromArgb(52, 52, 52),
        StatusStoppedText: Color.FromArgb(235, 235, 235),
        StatusRunningBack: Color.FromArgb(30, 130, 66),
        StatusRunningText: Color.White,
        StartBack: Color.FromArgb(30, 130, 66),
        StartText: Color.White,
        StopBack: Color.FromArgb(192, 57, 43),
        StopText: Color.White);

    /// <summary>
    /// Slightly lighter tint shown while the mouse is over a button.
    /// The only place where hover tints are calculated, so every button behaves the same.
    /// </summary>
    public static Color Hover(Color baseColor) => ControlPaint.Light(baseColor, 0.1f);

    /// <summary>Slightly darker tint shown while a button is being pressed.</summary>
    public static Color Pressed(Color baseColor) => ControlPaint.Dark(baseColor, 0.1f);
}
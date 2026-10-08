#nullable enable
using System.Collections.Generic;
using System.Windows.Forms;

namespace AutoRetard;

/// <summary>
/// One selectable key: a display label plus the Windows Forms key it represents.
/// ToString() returns the label so a ComboBox shows it directly.
/// </summary>
internal sealed record KeyOption(string Label, Keys Key)
{
    /// <summary>
    /// The key reserved for the application's own global Start / Stop shortcut.
    /// It is deliberately NOT offered in <see cref="All"/>, so a later version that sends keys
    /// can never send the very key that toggles this application.
    /// </summary>
    public const Keys ReservedStartStopKey = Keys.F6;

    public override string ToString() => Label;

    /// <summary>The fixed, curated list of keys the user can choose from.</summary>
    public static IReadOnlyList<KeyOption> All { get; } = Build();

    private static List<KeyOption> Build()
    {
        var list = new List<KeyOption>
        {
            new("Space", Keys.Space),
            new("Enter", Keys.Enter),
            new("Tab", Keys.Tab),
            new("Left Arrow", Keys.Left),
            new("Up Arrow", Keys.Up),
            new("Right Arrow", Keys.Right),
            new("Down Arrow", Keys.Down),
        };

        for (int i = 0; i < 26; i++)
        {
            list.Add(new KeyOption(((char)('A' + i)).ToString(), (Keys)((int)Keys.A + i)));
        }

        for (int i = 0; i <= 9; i++)
        {
            list.Add(new KeyOption(i.ToString(), (Keys)((int)Keys.D0 + i)));
        }

        for (int i = 1; i <= 12; i++)
        {
            Keys key = (Keys)((int)Keys.F1 + (i - 1));
            if (key == ReservedStartStopKey)
            {
                continue; // F6 is the application's Start / Stop shortcut, not an automation key.
            }

            list.Add(new KeyOption("F" + i, key));
        }

        return list;
    }
}
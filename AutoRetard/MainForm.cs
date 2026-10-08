#nullable enable
using System;
using System.Drawing;
using System.Windows.Forms;

namespace AutoRetard;

/// <summary>
/// Phase 1 main window: header with light / dark switch, status banner,
/// key + interval (value and unit) inputs, validation text, and Start / Stop / F6.
/// F6 is a system-wide shortcut for the same Start / Stop logic as the buttons.
/// This phase does NOT send any keys and does NOT wait for the interval.
/// </summary>
internal sealed class MainForm : Form
{
    // Layout metrics. All values are logical pixels at 96 DPI: AutoScaleMode.Dpi scales them
    // (and the point-size fonts below scale themselves) for the current display.
    private const int ContentMinimumWidth = 640;
    private const int ColumnGap = 12;   // between side-by-side controls (value / unit, Start / Stop / F6)
    private const int GroupGap = 20;    // between the key group and the interval group
    private const int SectionGap = 14;  // between major sections
    private const int CaptionGap = 4;   // between a caption and the control it describes

    // The id only has to be unique among hotkeys registered by this one window (there is just one).
    private const int StartStopHotkeyId = 1;

    // Visible name of the shortcut. The key itself is KeyOption.ReservedStartStopKey (F6);
    // if that key ever changes, this text and the "F6" button caption must change with it.
    private const string ShortcutName = "F6 \u2014 Start/Stop";
    private const string PhaseNote = "Phase 1: interface only. No keys are sent yet.";

    private static readonly Padding ContentPadding = new(28, 18, 28, 18);

    private readonly Font _baseFont = new("Segoe UI", 10f);
    private readonly Font _titleFont = new("Segoe UI", 16f, FontStyle.Bold);
    private readonly Font _captionFont = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _inputFont = new("Segoe UI", 13f);
    private readonly Font _stateFont = new("Segoe UI", 28f, FontStyle.Bold);
    private readonly Font _detailFont = new("Segoe UI", 12f);
    private readonly Font _buttonFont = new("Segoe UI", 13f, FontStyle.Bold);
    private readonly Font _errorFont = new("Segoe UI", 10f, FontStyle.Bold);
    private readonly Font _symbolFont = new("Segoe UI Symbol", 10f);
    private readonly ToolTip _toolTip = new();
    private readonly GlobalHotkey _startStopHotkey = new(StartStopHotkeyId);

    private readonly TableLayoutPanel _statusTable = new();
    private readonly Label _statusLabel = new();
    private readonly Label _statusDetailLabel = new();
    private readonly Label _titleLabel = new();
    private readonly Label _subtitleLabel = new();
    private readonly Label _keyCaption = new();
    private readonly Label _intervalCaption = new();
    private readonly ComboBox _keyComboBox = new();
    private readonly TextBox _intervalTextBox = new();
    private readonly ComboBox _unitComboBox = new();
    private readonly Label _hintLabel = new();
    private readonly Label _errorLabel = new();
    private readonly Panel _dividerPanel = new();
    private readonly Label _noteLabel = new();
    private readonly Button _startButton = new();
    private readonly Button _stopButton = new();
    private readonly Button _f6Button = new();
    private readonly RadioButton _lightButton = new();
    private readonly RadioButton _darkButton = new();

    private bool _isRunning;
    private bool _isDark;

    private ThemePalette Palette => _isDark ? ThemePalette.Dark : ThemePalette.Light;

    public MainForm()
    {
        Text = "AutoRetard";
        Font = _baseFont;
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        StartPosition = FormStartPosition.CenterScreen;
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        BuildLayout();
        WireEvents();
        UpdateRangeHint();
        SetStopped();
    }

    // ------------------------------------------------------------------
    // Layout
    // ------------------------------------------------------------------

    private void BuildLayout()
    {
        // Root: one flexible column. Every row sizes itself to its content, so nothing has a fixed height.
        TableLayoutPanel root = ConfigureAutoTable(new TableLayoutPanel(), columns: 1, rows: 8);
        root.Padding = ContentPadding;
        root.MinimumSize = new Size(ContentMinimumWidth + ContentPadding.Horizontal, 0);
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        Control header = BuildHeader();
        Control status = BuildStatusBanner();
        Control inputs = BuildInputGrid();
        Control actions = BuildActionRow();

        // Range hint (always two lines) and validation message.
        _hintLabel.AutoSize = true;
        _hintLabel.Margin = new Padding(0, 8, 0, 0);

        _errorLabel.AutoSize = true;
        _errorLabel.Font = _errorFont;
        _errorLabel.Margin = new Padding(0, 2, 0, 0);
        ClearError();

        // Divider and Phase 1 note.
        _dividerPanel.AutoSize = false;
        _dividerPanel.Height = 1;
        _dividerPanel.Dock = DockStyle.Top;
        _dividerPanel.Margin = new Padding(0, SectionGap, 0, 10);

        _noteLabel.AutoSize = true;
        _noteLabel.Text = PhaseNote;
        _noteLabel.Margin = Padding.Empty;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(status, 0, 1);
        root.Controls.Add(inputs, 0, 2);
        root.Controls.Add(_hintLabel, 0, 3);
        root.Controls.Add(_errorLabel, 0, 4);
        root.Controls.Add(actions, 0, 5);
        root.Controls.Add(_dividerPanel, 0, 6);
        root.Controls.Add(_noteLabel, 0, 7);

        // Tab order, set in one place: top to bottom, with the theme switch (inside the header) reached last.
        Control[] tabOrder =
        {
            status, inputs, _hintLabel, _errorLabel, actions, _dividerPanel, _noteLabel, header,
        };
        for (int i = 0; i < tabOrder.Length; i++)
        {
            tabOrder[i].TabIndex = i;
        }

        Controls.Add(root);
        AcceptButton = _startButton;
    }

    private Control BuildHeader()
    {
        _titleLabel.Text = "AutoRetard";
        _titleLabel.Font = _titleFont;
        _titleLabel.AutoSize = true;
        _titleLabel.Margin = Padding.Empty;

        _subtitleLabel.Text = "Keyboard auto-presser";
        _subtitleLabel.Font = _baseFont;
        _subtitleLabel.AutoSize = true;
        _subtitleLabel.Margin = new Padding(1, 0, 0, 0);

        var titleStack = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            Anchor = AnchorStyles.Left,
            Margin = Padding.Empty,
            TabStop = false,
        };
        titleStack.Controls.Add(_titleLabel);
        titleStack.Controls.Add(_subtitleLabel);

        ConfigureThemeButton(_lightButton, "\u2600  Light", "Light theme");
        ConfigureThemeButton(_darkButton, "\u263E  Dark", "Dark theme");
        _lightButton.Checked = true;
        _lightButton.TabIndex = 0;
        _darkButton.TabIndex = 1;

        // The two buttons sit edge to edge so they read as one segmented switch.
        // The panel is the accessibility "group"; each button reports its own selected state.
        var themePanel = new FlowLayoutPanel
        {
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Anchor = AnchorStyles.Right,
            Margin = Padding.Empty,
            TabStop = false,
            TabIndex = 1,
            AccessibleName = "Color theme",
            AccessibleRole = AccessibleRole.Grouping,
            AccessibleDescription = "Choose the Light or Dark color theme.",
        };
        themePanel.Controls.Add(_lightButton);
        themePanel.Controls.Add(_darkButton);

        TableLayoutPanel header = ConfigureAutoTable(new TableLayoutPanel(), columns: 2, rows: 1);
        header.Margin = new Padding(0, 0, 0, SectionGap);
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        header.Controls.Add(titleStack, 0, 0);
        header.Controls.Add(themePanel, 1, 0);
        return header;
    }

    private Control BuildStatusBanner()
    {
        // Both labels size from their text and font, and both table rows are AutoSize,
        // so the large STOPPED / RUNNING text and the detail line can never be cut off.
        _statusLabel.AutoSize = true;
        _statusLabel.Font = _stateFont;
        _statusLabel.Anchor = AnchorStyles.None;
        _statusLabel.TextAlign = ContentAlignment.MiddleCenter;
        _statusLabel.Margin = Padding.Empty;

        _statusDetailLabel.AutoSize = true;
        _statusDetailLabel.Font = _detailFont;
        _statusDetailLabel.Anchor = AnchorStyles.None;
        _statusDetailLabel.TextAlign = ContentAlignment.MiddleCenter;
        _statusDetailLabel.Margin = new Padding(0, 3, 0, 0);

        ConfigureAutoTable(_statusTable, columns: 1, rows: 2);
        _statusTable.Padding = new Padding(18, 10, 18, 10);
        _statusTable.Margin = new Padding(0, 0, 0, SectionGap);
        _statusTable.AccessibleName = "Current status";
        _statusTable.AccessibleRole = AccessibleRole.Grouping;
        _statusTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        _statusTable.Controls.Add(_statusLabel, 0, 0);
        _statusTable.Controls.Add(_statusDetailLabel, 0, 1);
        return _statusTable;
    }

    /// <summary>
    /// Two groups side by side: the key on the left, the interval value + unit on the right.
    /// Captions sit in the row above their controls. The window gets wider, not taller.
    /// </summary>
    private Control BuildInputGrid()
    {
        ConfigureCaption(_keyCaption, "Key to press", new Padding(0, 0, GroupGap, CaptionGap));
        ConfigureCaption(_intervalCaption, "Interval between presses", new Padding(0, 0, 0, CaptionGap));

        ConfigureComboBox(_keyComboBox, "Key to press", new Padding(0, 0, GroupGap, 0));
        _keyComboBox.MaxDropDownItems = 10;
        foreach (KeyOption option in KeyOption.All)
        {
            _keyComboBox.Items.Add(option);
        }
        _keyComboBox.SelectedIndex = 0;

        Control intervalRow = BuildIntervalRow();

        TableLayoutPanel grid = ConfigureAutoTable(new TableLayoutPanel(), columns: 2, rows: 2);
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36f));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 64f));
        grid.Controls.Add(_keyCaption, 0, 0);
        grid.Controls.Add(_intervalCaption, 1, 0);
        grid.Controls.Add(_keyComboBox, 0, 1);
        grid.Controls.Add(intervalRow, 1, 1);

        // Keyboard order inside the grid: key, then interval value, then unit.
        _keyCaption.TabIndex = 0;
        _intervalCaption.TabIndex = 1;
        _keyComboBox.TabIndex = 2;
        intervalRow.TabIndex = 3;
        return grid;
    }

    private Control BuildIntervalRow()
    {
        _intervalTextBox.Font = _inputFont;
        _intervalTextBox.MaxLength = 12;
        _intervalTextBox.Text = "1";
        _intervalTextBox.BorderStyle = BorderStyle.FixedSingle;
        _intervalTextBox.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        _intervalTextBox.Margin = new Padding(0, 0, ColumnGap, 0);
        _intervalTextBox.AccessibleName = "Interval value";
        _intervalTextBox.TabIndex = 0;

        ConfigureComboBox(_unitComboBox, "Interval time unit", Padding.Empty);
        foreach (TimeUnitOption unit in TimeUnitOption.All)
        {
            _unitComboBox.Items.Add(unit);
        }
        _unitComboBox.SelectedIndex = 1;
        _unitComboBox.TabIndex = 1;

        TableLayoutPanel row = ConfigureAutoTable(new TableLayoutPanel(), columns: 2, rows: 1);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60f));
        row.Controls.Add(_intervalTextBox, 0, 0);
        row.Controls.Add(_unitComboBox, 1, 0);
        return row;
    }

    /// <summary>[ Start ] [ Stop ] [ F6 ]: one row, same height. F6 is the quieter, bordered shortcut button.</summary>
    private Control BuildActionRow()
    {
        int half = ColumnGap / 2;
        ConfigureActionButton(_startButton, "Start", new Padding(0, 0, half, 0));
        ConfigureActionButton(_stopButton, "Stop", new Padding(half, 0, half, 0));
        ConfigureActionButton(_f6Button, "F6", new Padding(half, 0, 0, 0));
        _startButton.TabIndex = 0;
        _stopButton.TabIndex = 1;
        _f6Button.TabIndex = 2;

        // The F6 button is a visible label for the keyboard shortcut and runs the same Start / Stop toggle.
        // Its tooltip and accessible description are completed once the hotkey registration result is known
        // (ApplyHotkeyStatus), so nothing here claims the global shortcut works before it has been tried.
        _f6Button.AccessibleName = ShortcutName;
        _toolTip.SetToolTip(_f6Button, ShortcutName);
        _toolTip.SetToolTip(_startButton, "Start (F6)");
        _toolTip.SetToolTip(_stopButton, "Stop (F6)");

        TableLayoutPanel row = ConfigureAutoTable(new TableLayoutPanel(), columns: 3, rows: 1);
        row.Margin = new Padding(0, 10, 0, 0);
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));
        row.Controls.Add(_startButton, 0, 0);
        row.Controls.Add(_stopButton, 1, 0);
        row.Controls.Add(_f6Button, 2, 0);
        return row;
    }

    // ------------------------------------------------------------------
    // Small layout / styling helpers (structure only; colors live in ThemePalette)
    // ------------------------------------------------------------------

    /// <summary>Makes a table grow and shrink with its content: every row is AutoSize.</summary>
    private static TableLayoutPanel ConfigureAutoTable(TableLayoutPanel table, int columns, int rows)
    {
        table.AutoSize = true;
        table.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        table.ColumnCount = columns;
        table.RowCount = rows;
        table.Dock = DockStyle.Fill;
        table.Margin = Padding.Empty;
        for (int i = 0; i < rows; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        return table;
    }

    private void ConfigureCaption(Label label, string text, Padding margin)
    {
        label.Text = text;
        label.Font = _captionFont;
        label.AutoSize = true;
        label.Anchor = AnchorStyles.Left;
        label.Margin = margin;
    }

    private void ConfigureComboBox(ComboBox combo, string accessibleName, Padding margin)
    {
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.FlatStyle = FlatStyle.Flat;
        combo.Font = _inputFont;
        combo.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        combo.Margin = margin;
        combo.AccessibleName = accessibleName;
    }

    private void ConfigureActionButton(Button button, string text, Padding margin)
    {
        button.Text = text;
        button.Font = _buttonFont;
        button.AutoSize = true;
        button.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        button.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        button.Margin = margin;
        button.Padding = new Padding(12, 7, 12, 7);
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.UseVisualStyleBackColor = false;
    }

    private void ConfigureThemeButton(RadioButton button, string text, string themeName)
    {
        button.Text = text;
        button.Font = _symbolFont;
        button.Appearance = Appearance.Button;
        button.AutoSize = true;
        button.Margin = Padding.Empty;
        button.Padding = new Padding(10, 6, 10, 6);
        button.TextAlign = ContentAlignment.MiddleCenter;
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 1;
        button.UseVisualStyleBackColor = false;

        // The selected / not selected state is NOT written into these texts: a RadioButton already
        // reports its Checked state to screen readers, and repeating it here would be read out twice.
        button.AccessibleName = themeName;
        button.AccessibleRole = AccessibleRole.RadioButton;
        button.AccessibleDescription = $"Switch to the {themeName.ToLowerInvariant()}. Arrow keys move between Light and Dark.";
        _toolTip.SetToolTip(button, themeName);
    }

    // ------------------------------------------------------------------
    // Events and Start / Stop logic
    // ------------------------------------------------------------------

    private void WireEvents()
    {
        _startButton.Click += OnStartClicked;
        _stopButton.Click += OnStopClicked;
        _f6Button.Click += OnShortcutButtonClicked;
        _lightButton.CheckedChanged += (_, _) =>
        {
            if (_lightButton.Checked)
            {
                SetTheme(dark: false);
            }
        };
        _darkButton.CheckedChanged += (_, _) =>
        {
            if (_darkButton.Checked)
            {
                SetTheme(dark: true);
            }
        };

        _intervalTextBox.TextChanged += (_, _) => ClearError();
        _keyComboBox.SelectedIndexChanged += (_, _) => ClearError();
        _unitComboBox.SelectedIndexChanged += (_, _) =>
        {
            ClearError();
            UpdateRangeHint();
        };
    }

    private void UpdateRangeHint()
    {
        if (_unitComboBox.SelectedItem is TimeUnitOption unit)
        {
            _hintLabel.Text = InputValidator.DescribeRange(unit);
        }
    }

    private void ShowError(string message)
    {
        _errorLabel.Text = message;
    }

    private void ClearError()
    {
        // A single space instead of "": the label keeps one line of height, so the window and the
        // buttons do not jump up and down when a message appears or disappears.
        _errorLabel.Text = " ";
    }

    private void OnStartClicked(object? sender, EventArgs e) => RequestStart();

    private void OnStopClicked(object? sender, EventArgs e) => RequestStop();

    private void OnShortcutButtonClicked(object? sender, EventArgs e) => ToggleRunning();

    /// <summary>
    /// The single Start / Stop toggle. The F6 button, the global F6 hotkey and the in-window
    /// fallback all end up here, and it only calls the same RequestStart / RequestStop as the
    /// Start and Stop buttons. No other code decides when the running state changes.
    /// </summary>
    private void ToggleRunning()
    {
        if (_isRunning)
        {
            RequestStop();
        }
        else
        {
            RequestStart();
        }
    }

    /// <summary>Validates the inputs and, when they are valid, switches to RUNNING. Same rules as before.</summary>
    private void RequestStart()
    {
        if (_isRunning)
        {
            return;
        }

        if (!InputValidator.TryGetKey(_keyComboBox.SelectedItem, out KeyOption key, out string keyError))
        {
            ShowError(keyError);
            FocusIfActive(_keyComboBox);
            return;
        }

        if (!InputValidator.TryGetUnit(_unitComboBox.SelectedItem, out TimeUnitOption unit, out string unitError))
        {
            ShowError(unitError);
            FocusIfActive(_unitComboBox);
            return;
        }

        if (!InputValidator.TryParseInterval(_intervalTextBox.Text, unit, out IntervalSetting interval, out string intervalError))
        {
            ShowError(intervalError);
            FocusIfActive(_intervalTextBox);
            _intervalTextBox.SelectAll();
            return;
        }

        ClearError();
        SetRunning(key.Label, interval);
    }

    private void RequestStop()
    {
        if (!_isRunning)
        {
            return;
        }

        SetStopped();
        FocusIfActive(_startButton);
    }

    /// <summary>
    /// Moves keyboard focus only while this window is the active one, so pressing F6 in another
    /// program never pulls focus (or the window) away from what the person is doing.
    /// </summary>
    private void FocusIfActive(Control control)
    {
        if (ReferenceEquals(ActiveForm, this))
        {
            control.Focus();
        }
    }

    private void SetRunning(string keyLabel, IntervalSetting interval)
    {
        _isRunning = true;
        _statusLabel.Text = "RUNNING";
        _statusDetailLabel.Text = $"{keyLabel} every {interval.DisplayText}";

        _keyComboBox.Enabled = false;
        _unitComboBox.Enabled = false;
        _intervalTextBox.ReadOnly = true;
        _startButton.Enabled = false;
        _stopButton.Enabled = true;

        RefreshAppearance();
        FocusIfActive(_stopButton);
    }

    private void SetStopped()
    {
        _isRunning = false;
        _statusLabel.Text = "STOPPED";
        _statusDetailLabel.Text = "Choose a key and interval, then press Start or F6.";

        _keyComboBox.Enabled = true;
        _unitComboBox.Enabled = true;
        _intervalTextBox.ReadOnly = false;
        _startButton.Enabled = true;
        _stopButton.Enabled = false;

        RefreshAppearance();
    }

    private void SetTheme(bool dark)
    {
        if (_isDark == dark)
        {
            return;
        }

        _isDark = dark;
        RefreshAppearance();
    }

    // ------------------------------------------------------------------
    // Global F6 hotkey
    // ------------------------------------------------------------------
    // F6 is registered with the Windows RegisterHotKey function for the whole time this window has a
    // handle (not only while RUNNING, because F6 must also be able to START). It is released when the
    // handle is destroyed or the form closes, and registered again if the handle is ever recreated.
    // If registration fails, nothing else changes: the buttons still work, F6 still works while this
    // window is active (see ProcessCmdKey), and the window says that global F6 is not available.

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);

        ApplyHotkeyStatus(_startStopHotkey.TryRegister(Handle, KeyOption.ReservedStartStopKey));
    }

    protected override void OnHandleDestroyed(EventArgs e)
    {
        _startStopHotkey.Unregister();
        base.OnHandleDestroyed(e);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _startStopHotkey.Unregister();
        base.OnFormClosed(e);
    }

    protected override void WndProc(ref Message m)
    {
        if (_startStopHotkey.IsHotkeyMessage(m))
        {
            ToggleRunning();
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>
    /// Fallback used only when the global hotkey could not be registered:
    /// F6 still toggles Start / Stop while this window is active (normal WinForms command-key handling,
    /// no hook and no polling). While the hotkey IS registered, Windows delivers F6 through WndProc
    /// instead and this method leaves the key alone, so it can never toggle twice.
    /// </summary>
    protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    {
        if (keyData == KeyOption.ReservedStartStopKey && !_startStopHotkey.IsRegistered)
        {
            // Bit 30 of lParam is set when the key was already down: ignore auto-repeat while it is held.
            bool isAutoRepeat = (msg.LParam.ToInt64() & (1L << 30)) != 0;
            if (!isAutoRepeat)
            {
                ToggleRunning();
            }

            return true;
        }

        return base.ProcessCmdKey(ref msg, keyData);
    }

    /// <summary>Shows what is actually true about F6: tooltip, accessible description and the note line.</summary>
    private void ApplyHotkeyStatus(bool globalHotkeyActive)
    {
        if (globalHotkeyActive)
        {
            _toolTip.SetToolTip(_f6Button, ShortcutName + " (works from any window)");
            _f6Button.AccessibleDescription =
                "Starts or stops, exactly like the Start and Stop buttons. The F6 key does the same, from any window.";
            _noteLabel.Text = PhaseNote;
        }
        else
        {
            _toolTip.SetToolTip(_f6Button, ShortcutName + " (works only while this window is active)");
            _f6Button.AccessibleDescription =
                "Starts or stops, exactly like the Start and Stop buttons. The F6 key does the same, but only while this window is active.";
            _noteLabel.Text = PhaseNote + Environment.NewLine +
                "Global F6 is unavailable (it may be in use by another program). F6 works only while this window is active.";
        }
    }

    // ------------------------------------------------------------------
    // Theme application: every color comes from the current ThemePalette
    // ------------------------------------------------------------------

    private void RefreshAppearance()
    {
        ThemePalette p = Palette;

        ApplyFormColors(p);
        ApplyStatusColors(p);
        ApplyInputColors(p);
        ApplyButtonColors(p);
    }

    private void ApplyFormColors(ThemePalette p)
    {
        // Labels without their own color (title, captions) inherit Text from the form.
        BackColor = p.FormBack;
        ForeColor = p.Text;

        _subtitleLabel.ForeColor = p.HintText;
        _hintLabel.ForeColor = p.HintText;
        _noteLabel.ForeColor = p.HintText;
        _errorLabel.ForeColor = p.ErrorText;
        _dividerPanel.BackColor = p.Divider;
    }

    private void ApplyStatusColors(ThemePalette p)
    {
        Color back = _isRunning ? p.StatusRunningBack : p.StatusStoppedBack;
        Color text = _isRunning ? p.StatusRunningText : p.StatusStoppedText;

        // The two status labels inherit their background from the banner table.
        _statusTable.BackColor = back;
        _statusLabel.ForeColor = text;
        _statusDetailLabel.ForeColor = text;
        _statusTable.AccessibleDescription = $"{_statusLabel.Text}. {_statusDetailLabel.Text}";
    }

    private void ApplyInputColors(ThemePalette p)
    {
        // While "running" the inputs are locked, so show them with the muted background.
        Color back = _isRunning ? p.InputReadOnlyBack : p.InputBack;

        _intervalTextBox.BackColor = back;
        _intervalTextBox.ForeColor = p.InputText;
        _keyComboBox.BackColor = back;
        _keyComboBox.ForeColor = p.InputText;
        _unitComboBox.BackColor = back;
        _unitComboBox.ForeColor = p.InputText;
    }

    private void ApplyButtonColors(ThemePalette p)
    {
        StyleActionButton(_startButton, p, p.StartBack, p.StartText);
        StyleActionButton(_stopButton, p, p.StopBack, p.StopText);
        StyleNeutralButton(_f6Button, p);
        StyleThemeButton(_lightButton, p, selected: !_isDark);
        StyleThemeButton(_darkButton, p, selected: _isDark);
    }

    private static void StyleActionButton(Button button, ThemePalette p, Color enabledBack, Color enabledText)
    {
        if (button.Enabled)
        {
            StyleFlatButton(button, enabledBack, enabledText, enabledBack,
                ThemePalette.Hover(enabledBack), ThemePalette.Pressed(enabledBack));
        }
        else
        {
            StyleFlatButton(button, p.ButtonDisabledBack, p.DisabledText, p.ButtonBorder,
                p.ButtonDisabledBack, p.ButtonDisabledBack);
        }
    }

    /// <summary>Quiet, bordered look for the F6 shortcut button, so it reads as secondary to Start and Stop.</summary>
    private static void StyleNeutralButton(Button button, ThemePalette p)
    {
        StyleFlatButton(button, p.ButtonBack, p.ButtonText, p.ButtonBorder,
            ThemePalette.Hover(p.ButtonBack), ThemePalette.Pressed(p.ButtonBack));
    }

    private static void StyleThemeButton(RadioButton button, ThemePalette p, bool selected)
    {
        // Only the selected button is a keyboard tab stop; the arrow keys move between the two.
        button.TabStop = selected;
        button.FlatAppearance.CheckedBackColor = p.Accent;

        if (selected)
        {
            // Filled with the accent color: clearly different from the neutral, unselected button.
            StyleFlatButton(button, p.Accent, p.AccentText, p.ButtonBorder, p.Accent, p.Accent);
        }
        else
        {
            StyleFlatButton(button, p.ButtonBack, p.ButtonText, p.ButtonBorder,
                ThemePalette.Hover(p.ButtonBack), ThemePalette.Pressed(p.ButtonBack));
        }
    }

    /// <summary>The one place that applies a color set to a flat-style button.</summary>
    private static void StyleFlatButton(ButtonBase button, Color back, Color text, Color border, Color hover, Color pressed)
    {
        button.BackColor = back;
        button.ForeColor = text;
        button.FlatAppearance.BorderColor = border;
        button.FlatAppearance.MouseOverBackColor = hover;
        button.FlatAppearance.MouseDownBackColor = pressed;
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        SetStopped();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Normally already done when the handle is destroyed; harmless to repeat.
            _startStopHotkey.Unregister();
        }

        base.Dispose(disposing);
        if (disposing)
        {
            _toolTip.Dispose();
            _symbolFont.Dispose();
            _errorFont.Dispose();
            _buttonFont.Dispose();
            _detailFont.Dispose();
            _stateFont.Dispose();
            _inputFont.Dispose();
            _captionFont.Dispose();
            _titleFont.Dispose();
            _baseFont.Dispose();
        }
    }
}
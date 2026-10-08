#nullable enable
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace AutoRetard;

/// <summary>
/// One system-wide hotkey, registered with the built-in Windows RegisterHotKey function.
///
/// How it works: Windows itself watches for the key and posts a WM_HOTKEY message to the window
/// that registered it. There is no keyboard hook, no polling and no timer, and no elevation is needed.
/// While the hotkey is registered, other programs do not receive that key press.
/// The window must forward its messages to <see cref="IsHotkeyMessage"/> from its WndProc.
/// </summary>
internal sealed class GlobalHotkey
{
    /// <summary>Message Windows sends to the registering window when the hotkey is pressed.</summary>
    private const int WmHotkey = 0x0312;

    /// <summary>MOD_NOREPEAT: do not repeat the message while the key is held down.</summary>
    private const uint ModNoRepeat = 0x4000;

    private readonly int _id;
    private IntPtr _windowHandle = IntPtr.Zero;

    /// <param name="id">
    /// Hotkey identifier. Windows scopes it to the registering window, so it only has to be
    /// unique among the hotkeys registered by that one window (this app registers just one).
    /// </param>
    public GlobalHotkey(int id)
    {
        _id = id;
    }

    /// <summary>True only while Windows has accepted the registration and it has not been released.</summary>
    public bool IsRegistered { get; private set; }

    /// <summary>
    /// Tries to register the key (with no modifiers) for the given window.
    /// Returns false, without throwing, if it fails, for example because another program already owns the key.
    /// Calling it again while already registered does nothing and returns true (no double registration).
    /// </summary>
    public bool TryRegister(IntPtr windowHandle, Keys key)
    {
        if (IsRegistered)
        {
            return true;
        }

        if (windowHandle == IntPtr.Zero)
        {
            return false;
        }

        if (!RegisterHotKey(windowHandle, _id, ModNoRepeat, (uint)key))
        {
            return false;
        }

        _windowHandle = windowHandle;
        IsRegistered = true;
        return true;
    }

    /// <summary>
    /// Releases the hotkey. Safe to call more than once, or when nothing is registered:
    /// it never unregisters an id that was not successfully registered.
    /// </summary>
    public void Unregister()
    {
        if (!IsRegistered)
        {
            return;
        }

        UnregisterHotKey(_windowHandle, _id);
        _windowHandle = IntPtr.Zero;
        IsRegistered = false;
    }

    /// <summary>True when the window message is the press of this hotkey.</summary>
    public bool IsHotkeyMessage(Message message)
    {
        return IsRegistered && message.Msg == WmHotkey && message.WParam == (IntPtr)_id;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
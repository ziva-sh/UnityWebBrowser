// UnityWebBrowser (UWB)
// Copyright (c) 2021-2024 Voltstro-Studios
// 
// This project is under the MIT license.See the LICENSE.md file for more details.

using VoltstroStudios.UnityWebBrowser.Shared;
using Xilium.CefGlue;

namespace UnityWebBrowser.Engine.Cef.Shared.Browser;

internal static class UwbCefClientUtils
{
    // ZIVA PATCH: the character a non-printable key contributes to a CefKeyEvent.
    //
    // Windows and Linux resolve editing commands (backspace, delete, arrows, home/end) from
    // windowsKeyCode alone, so UWB never populated character/unmodifiedCharacter and nobody
    // noticed. macOS does not: Chromium builds its NativeWebKeyboardEvent from those fields, and
    // with them left at '\0' the key reaches Blink carrying no character, resolves to no editing
    // command, and silently does nothing — backspace could not delete text in the Unity dock.
    //
    // Control codes for the ASCII-ish keys, and NSEvent's function-key constants (0xF700 block,
    // NSUpArrowFunctionKey and friends) for the navigation keys, which is what AppKit itself puts
    // in -[NSEvent characters] and therefore what Chromium's Mac path expects. Returns '\0' for
    // everything else — printable keys already ride the Chars branch, which sets Character itself.
    public static char MacCharacterForKey(WindowsKey key) => key switch
    {
        WindowsKey.Back => '\b',
        WindowsKey.Tab => '\t',
        WindowsKey.Return => '\r',
        WindowsKey.Escape => (char)0x1B,
        WindowsKey.Delete => (char)0x7F,
        WindowsKey.Up => (char)0xF700,
        WindowsKey.Down => (char)0xF701,
        WindowsKey.Left => (char)0xF702,
        WindowsKey.Right => (char)0xF703,
        WindowsKey.Home => (char)0xF729,
        WindowsKey.End => (char)0xF72B,
        WindowsKey.PageUp => (char)0xF72C,
        WindowsKey.PageDown => (char)0xF72D,
        _ => '\0'
    };

    public static CefEventFlags GetKeyDirection(WindowsKey key) => key switch
    {
        WindowsKey.LShiftKey | WindowsKey.LControlKey | WindowsKey.LMenu => CefEventFlags.IsLeft,
        WindowsKey.RShiftKey | WindowsKey.RControlKey | WindowsKey.RMenu => CefEventFlags.ShiftDown,
        _ => CefEventFlags.None
    };
    
    public static CefEventFlags KeyToFlag(WindowsKey key) => key switch
    {
        // Stateful keys
        WindowsKey.CapsLock => CefEventFlags.CapsLockOn,
        WindowsKey.NumLock => CefEventFlags.NumLockOn,

        WindowsKey.Shift => CefEventFlags.ShiftDown,
        WindowsKey.ShiftKey => CefEventFlags.ShiftDown,
        WindowsKey.LShiftKey => CefEventFlags.ShiftDown,
        WindowsKey.RShiftKey => CefEventFlags.ShiftDown,

        WindowsKey.Control => CefEventFlags.ControlDown,
        WindowsKey.ControlKey => CefEventFlags.ControlDown,
        WindowsKey.LControlKey => CefEventFlags.ControlDown,
        WindowsKey.RControlKey => CefEventFlags.ControlDown,

        WindowsKey.Alt => CefEventFlags.AltGrDown,
        WindowsKey.Menu => CefEventFlags.AltDown,
        WindowsKey.LMenu => CefEventFlags.AltDown,
        WindowsKey.RMenu => CefEventFlags.AltDown,

        // ZIVA PATCH: Command is the macOS shortcut modifier, and dropping it here meant CEF was
        // never told the key was held — so Cmd+C/Cmd+V/Cmd+A did nothing in the Unity dock no
        // matter how the key event itself was delivered. Windows and Linux were unaffected
        // because they use Ctrl, which is mapped above. Clients send Command as LWin/RWin.
        WindowsKey.LWin => CefEventFlags.CommandDown,
        WindowsKey.RWin => CefEventFlags.CommandDown,

        _ => CefEventFlags.None
    };
}
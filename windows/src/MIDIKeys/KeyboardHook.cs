using System.Runtime.InteropServices;
using MIDIKeys.Core;

namespace MIDIKeys;

/// <summary>
/// Fängt Tastenereignisse systemweit ab (WH_KEYBOARD_LL) — das Gegenstück zum
/// CGEventTap unter macOS. Läuft in beiden Modi: inaktiv reicht er alles durch
/// und horcht nur auf den Umschalt-Shortcut.
///
/// Der Callback läuft auf dem UI-Thread. Windows entfernt einen Hook still,
/// wenn er zu lange braucht (LowLevelHooksTimeout) — darum passiert hier nur
/// Billiges; teure Folgen (Gerät öffnen, Zeichnen) laufen über BeginInvoke.
/// </summary>
internal sealed class KeyboardHook : IDisposable
{
    /// <summary>Kennung für selbst eingespeiste Tasten ("MIDK"), die der Hook ignoriert.</summary>
    public static readonly UIntPtr Magic = 0x4D49444B;

    /// <summary>Unbelegter virtueller Keycode — dasselbe Mittel wie AutoHotkeys MenuMaskKey.</summary>
    private const ushort MenuMaskVk = 0xE8;

    private readonly Func<ushort, Mods, bool, bool, bool> _handler;
    private readonly Native.LowLevelKeyboardProc _proc; // gegen den GC festhalten
    private IntPtr _hook;
    private readonly HashSet<ushort> _down = new();
    private readonly HashSet<ushort> _heldModifiers = new();
    /// <summary>Tasten, deren Drücken durchging — ihr Loslassen geht immer auch durch.</summary>
    private readonly HashSet<ushort> _passedDowns = new();
    /// <summary>Tasten, deren Drücken die Aufnahme geschluckt hat — ihr Loslassen ebenso.</summary>
    private readonly HashSet<ushort> _recordedDowns = new();

    /// <summary>Wenn gesetzt, geht die nächste Tastenkombination an die Hotkey-Aufnahme statt an den Controller.</summary>
    public Action<ushort, Mods>? Recorder { get; set; }

    public KeyboardHook(Func<ushort, Mods, bool, bool, bool> handler)
    {
        _handler = handler;
        _proc = Callback;
    }

    public bool IsRunning => _hook != IntPtr.Zero;

    public bool Start()
    {
        if (_hook != IntPtr.Zero) return true;
        _hook = Native.SetWindowsHookEx(Native.WH_KEYBOARD_LL, _proc, Native.GetModuleHandle(null), 0);
        if (_hook == IntPtr.Zero) Diagnose.Log($"SetWindowsHookEx fehlgeschlagen: {Marshal.GetLastWin32Error()}");
        return _hook != IntPtr.Zero;
    }

    public void Stop()
    {
        if (_hook == IntPtr.Zero) return;
        Native.UnhookWindowsHookEx(_hook);
        _hook = IntPtr.Zero;
    }

    public Mods CurrentMods
    {
        get
        {
            var m = Mods.None;
            foreach (var k in _heldModifiers)
            {
                m |= k switch
                {
                    ScanCodes.LeftShift or ScanCodes.RightShift => Mods.Shift,
                    ScanCodes.LeftCtrl or ScanCodes.RightCtrl => Mods.Ctrl,
                    ScanCodes.LeftAlt or ScanCodes.RightAlt => Mods.Alt,
                    ScanCodes.LeftWin or ScanCodes.RightWin => Mods.Win,
                    _ => Mods.None,
                };
            }
            return m;
        }
    }

    private IntPtr Callback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode < 0) return Native.CallNextHookEx(_hook, nCode, wParam, lParam);
        var data = Marshal.PtrToStructure<Native.KBDLLHOOKSTRUCT>(lParam);
        if (data.dwExtraInfo == Magic) return Native.CallNextHookEx(_hook, nCode, wParam, lParam);

        bool swallow;
        try
        {
            swallow = Process(data);
        }
        catch (Exception e)
        {
            // Nie die Tastatur wegen eines Fehlers verschlucken.
            Diagnose.Log($"Hook-Fehler: {e}");
            swallow = false;
        }
        return swallow ? (IntPtr)1 : Native.CallNextHookEx(_hook, nCode, wParam, lParam);
    }

    private bool Process(Native.KBDLLHOOKSTRUCT data)
    {
        var isUp = (data.flags & Native.LLKHF_UP) != 0;
        var scan = data.scanCode & 0xFF;
        if (scan == 0) scan = Native.MapVirtualKeyEx(data.vkCode, Native.MAPVK_VK_TO_VSC, IntPtr.Zero) & 0xFF;
        var key = (ushort)(scan | ((data.flags & Native.LLKHF_EXTENDED) != 0 ? (uint)ScanCodes.Extended : 0u));

        // Rechte Umschalttaste meldet manche Hardware als "erweitert" — normalisieren.
        if (key == (ScanCodes.Extended | ScanCodes.RightShift)) key = ScanCodes.RightShift;

        if (TapPolicy.ModifierKeys.Contains(key))
        {
            if (isUp) _heldModifiers.Remove(key); else _heldModifiers.Add(key);
            return false;
        }

        var mods = CurrentMods;
        var isRepeat = !isUp && _down.Contains(key);
        if (isUp) _down.Remove(key); else _down.Add(key);

        if (isUp && _recordedDowns.Remove(key)) return true;
        if (!isUp && Recorder is { } recorder)
        {
            _recordedDowns.Add(key);
            recorder(key, mods);
            MaskMenu(mods);
            return true;
        }

        var swallow = _handler(key, mods, !isUp, isRepeat);

        if (isUp)
        {
            // Ging das Drücken an die App, muss das Loslassen auch dorthin —
            // sonst hielte Windows die Taste für gedrückt.
            if (_passedDowns.Remove(key)) return false;
        }
        else
        {
            if (!swallow) _passedDowns.Add(key);
            else MaskMenu(mods);
        }
        return swallow;
    }

    /// <summary>
    /// Wird eine Taste bei gehaltener Alt- oder Win-Taste geschluckt, sähe die
    /// App nur "Alt gedrückt, Alt losgelassen" und würde die Menüleiste
    /// aktivieren (bzw. Windows das Startmenü öffnen). Eine eingeschobene
    /// unbelegte Taste verhindert das.
    /// </summary>
    private void MaskMenu(Mods mods)
    {
        if ((mods & (Mods.Alt | Mods.Win)) == 0) return;
        var inputs = new Native.INPUT[2];
        for (var i = 0; i < 2; i++)
        {
            inputs[i].type = Native.INPUT_KEYBOARD;
            inputs[i].u.ki = new Native.KEYBDINPUT
            {
                wVk = MenuMaskVk,
                dwFlags = i == 1 ? Native.KEYEVENTF_KEYUP : 0,
                dwExtraInfo = Magic,
            };
        }
        Native.SendInput(2, inputs, Marshal.SizeOf<Native.INPUT>());
    }

    public void Dispose() => Stop();
}

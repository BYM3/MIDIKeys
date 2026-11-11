using System.Runtime.InteropServices;
using System.Text;

namespace MIDIKeys;

internal static class Native
{
    // ---- Tastatur-Hook ----
    public const int WH_KEYBOARD_LL = 13;
    public const uint LLKHF_EXTENDED = 0x01;
    public const uint LLKHF_INJECTED = 0x10;
    public const uint LLKHF_UP = 0x80;

    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    public struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    public static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    public static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr GetModuleHandle(string? lpModuleName);

    // ---- Eingabe einspeisen (Menü-Maske) ----
    public const uint INPUT_KEYBOARD = 1;
    public const uint KEYEVENTF_KEYUP = 0x0002;

    [StructLayout(LayoutKind.Sequential)]
    public struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MOUSEINPUT
    {
        public int dx, dy;
        public uint mouseData, dwFlags, time;
        public UIntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] public MOUSEINPUT mi;
        [FieldOffset(0)] public KEYBDINPUT ki;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct INPUT
    {
        public uint type;
        public InputUnion u;
    }

    [DllImport("user32.dll", SetLastError = true)]
    public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    // ---- Tastaturlayout ----
    public const uint MAPVK_VK_TO_VSC = 0;
    public const uint MAPVK_VSC_TO_VK_EX = 3;

    [DllImport("user32.dll")]
    public static extern uint MapVirtualKeyEx(uint uCode, uint uMapType, IntPtr dwhkl);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern int ToUnicodeEx(uint wVirtKey, uint wScanCode, byte[] lpKeyState,
        [Out] StringBuilder pwszBuff, int cchBuff, uint wFlags, IntPtr dwhkl);

    [DllImport("user32.dll")]
    public static extern IntPtr GetKeyboardLayout(uint idThread);

    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    // ---- Fenster ----
    [DllImport("dwmapi.dll")]
    public static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
    public const int DWMWCP_ROUND = 2;

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    public static extern bool DestroyIcon(IntPtr handle);
}

/// <summary>Die klassische Windows-Multimedia-MIDI-API (winmm).</summary>
internal static class WinMm
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MIDIOUTCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public ushort wTechnology;
        public ushort wVoices;
        public ushort wNotes;
        public ushort wChannelMask;
        public uint dwSupport;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct MIDIINCAPS
    {
        public ushort wMid;
        public ushort wPid;
        public uint vDriverVersion;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szPname;
        public uint dwSupport;
    }

    [DllImport("winmm.dll")] public static extern uint midiOutGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    public static extern uint midiOutGetDevCaps(UIntPtr uDeviceID, ref MIDIOUTCAPS caps, uint cbMidiOutCaps);
    [DllImport("winmm.dll")] public static extern uint midiOutOpen(out IntPtr handle, uint deviceId, IntPtr callback, IntPtr instance, uint flags);
    [DllImport("winmm.dll")] public static extern uint midiOutShortMsg(IntPtr handle, uint message);
    [DllImport("winmm.dll")] public static extern uint midiOutReset(IntPtr handle);
    [DllImport("winmm.dll")] public static extern uint midiOutClose(IntPtr handle);

    [DllImport("winmm.dll")] public static extern uint midiInGetNumDevs();
    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    public static extern uint midiInGetDevCaps(UIntPtr uDeviceID, ref MIDIINCAPS caps, uint cbMidiInCaps);

    public static List<string> OutputNames()
    {
        var result = new List<string>();
        var n = midiOutGetNumDevs();
        for (uint i = 0; i < n; i++)
        {
            var caps = new MIDIOUTCAPS();
            if (midiOutGetDevCaps((UIntPtr)i, ref caps, (uint)Marshal.SizeOf<MIDIOUTCAPS>()) == 0)
                result.Add(caps.szPname);
            else
                result.Add("");
        }
        return result;
    }

    public static List<string> InputNames()
    {
        var result = new List<string>();
        var n = midiInGetNumDevs();
        for (uint i = 0; i < n; i++)
        {
            var caps = new MIDIINCAPS();
            result.Add(midiInGetDevCaps((UIntPtr)i, ref caps, (uint)Marshal.SizeOf<MIDIINCAPS>()) == 0 ? caps.szPname : "");
        }
        return result;
    }
}

/// <summary>
/// teVirtualMIDI von Tobias Erichsen — der Treiber, auf dem loopMIDI aufsetzt.
/// Ist er installiert, kann die App wie unter macOS selbst einen MIDI-Eingang
/// namens "MIDIKeys" anlegen. Die DLL liegt dann in System32.
/// </summary>
internal static class TeVirtualMidi
{
    private const string Dll = "teVirtualMIDI64.dll";

    public const uint TE_VM_FLAGS_PARSE_RX = 1;
    public const uint TE_VM_FLAGS_PARSE_TX = 2;
    public const uint TE_VM_FLAGS_INSTANTIATE_RX_ONLY = 4;
    public const uint TE_VM_FLAGS_INSTANTIATE_TX_ONLY = 8;

    [DllImport(Dll, CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern IntPtr virtualMIDICreatePortEx2(string portName, IntPtr callback, IntPtr dwCallbackInstance, uint maxSysexLength, uint flags);

    [DllImport(Dll)]
    public static extern void virtualMIDIClosePort(IntPtr port);

    [DllImport(Dll, SetLastError = true)]
    public static extern bool virtualMIDISendData(IntPtr port, byte[] data, uint length);
}

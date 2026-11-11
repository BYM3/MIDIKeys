using System.Text;
using Microsoft.Win32;
using MIDIKeys.Core;

namespace MIDIKeys;

/// <summary>Schreibt Diagnosezeilen nach %TEMP%\midikeys.log — eine Tray-App hat sonst keinen Ort dafür.</summary>
internal static class Diagnose
{
    public static readonly string Pfad = Path.Combine(Path.GetTempPath(), "midikeys.log");

    /// <summary>MIDIKEYS_LOG_MIDI=1 protokolliert jede gesendete Nachricht — zum Prüfen ohne DAW.</summary>
    public static readonly bool LogMidi = Environment.GetEnvironmentVariable("MIDIKEYS_LOG_MIDI") == "1";

    public static void LogMessage(string ziel, MidiMessage m, bool zugestellt)
    {
        if (!LogMidi) return;
        var b = m.Bytes;
        Log($"MIDI {ziel}: {b[0]:X2} {b[1]:X2} {b[2]:X2}{(zugestellt ? "" : " (kein Ziel)")}");
    }

    public static void Log(string text)
    {
        try { File.AppendAllText(Pfad, $"[{DateTime.Now:HH:mm:ss.fff}] {text}{Environment.NewLine}"); }
        catch (IOException) { }
    }
}

/// <summary>
/// Löst Scancodes in die Zeichen des aktiven Tastaturlayouts auf. Die
/// Klaviatur bleibt layoutunabhängig; nur die Beschriftung folgt dem Layout.
/// </summary>
internal static class KeyLabelResolver
{
    /// <summary>Beschriftungen für ein deutsches Layout — greift, wenn die Auflösung fehlschlägt.</summary>
    public static readonly Dictionary<ushort, string> FallbackLabels = new()
    {
        [ScanCodes.A] = "a", [ScanCodes.S] = "s", [ScanCodes.D] = "d", [ScanCodes.F] = "f",
        [ScanCodes.G] = "g", [ScanCodes.H] = "h", [ScanCodes.J] = "j", [ScanCodes.K] = "k",
        [ScanCodes.L] = "l", [ScanCodes.Semicolon] = "ö", [ScanCodes.Quote] = "ä", [ScanCodes.Backslash] = "#",
        [ScanCodes.W] = "w", [ScanCodes.E] = "e", [ScanCodes.T] = "t", [ScanCodes.Y] = "z",
        [ScanCodes.U] = "u", [ScanCodes.O] = "o", [ScanCodes.P] = "p", [ScanCodes.RightBracket] = "+",
        [ScanCodes.Z] = "y", [ScanCodes.X] = "x", [ScanCodes.Tab] = "⇥",
        [ScanCodes.Grave] = "^", [ScanCodes.D1] = "1", [ScanCodes.IsoBackslash] = "<",
        [ScanCodes.Slash] = "-", [ScanCodes.Escape] = "Esc", [ScanCodes.Space] = "Space",
    };

    private static readonly Dictionary<ushort, string> Named = new()
    {
        [ScanCodes.Tab] = "⇥", [ScanCodes.Escape] = "Esc", [ScanCodes.Space] = "Space",
        [ScanCodes.Enter] = "↵", [ScanCodes.Backspace] = "⌫", [ScanCodes.Delete] = "Del",
        [ScanCodes.Left] = "←", [ScanCodes.Right] = "→", [ScanCodes.Up] = "↑", [ScanCodes.Down] = "↓",
    };

    private static IntPtr CurrentLayout()
    {
        var thread = Native.GetWindowThreadProcessId(Native.GetForegroundWindow(), out _);
        return Native.GetKeyboardLayout(thread);
    }

    public static string Label(ushort key) => Label(key, k => Translate(k, CurrentLayout()));

    public static string Label(ushort key, Func<ushort, string?> aufloesung)
    {
        if (Named.TryGetValue(key, out var named)) return named;
        var aufgeloest = aufloesung(key);
        if (!string.IsNullOrEmpty(aufgeloest)) return aufgeloest;
        return FallbackLabels.TryGetValue(key, out var fb) ? fb : "?";
    }

    private static string? Translate(ushort key, IntPtr hkl)
    {
        var scan = (uint)(key & 0xFF);
        var mapScan = (key & ScanCodes.Extended) != 0 ? 0xE000u | scan : scan;
        var vk = Native.MapVirtualKeyEx(mapScan, Native.MAPVK_VSC_TO_VK_EX, hkl);
        if (vk == 0) return null;
        var sb = new StringBuilder(8);
        // Flag 4: Kein Eingriff in den Tastaturzustand (Tottasten bleiben unberührt).
        var n = Native.ToUnicodeEx(vk, scan, new byte[256], sb, sb.Capacity, 4, hkl);
        if (n == 0) return null;
        var text = n < 0 ? sb.ToString(0, Math.Min(1, sb.Length)) : sb.ToString(0, Math.Min(n, sb.Length));
        return text.Any(char.IsControl) || string.IsNullOrWhiteSpace(text) ? null : text;
    }
}

/// <summary>„Beim Anmelden öffnen“ über HKCU\…\Run — einmalig beim ersten Start eingetragen.</summary>
internal static class LoginItem
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ApprovedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
    private const string ValueName = "MIDIKeys";

    private static string Command => $"\"{Environment.ProcessPath}\"";

    public static void RegisterOnFirstLaunch(AppState state)
    {
        if (state.LoginItemRegistered) return;
        if (SetEnabled(true)) state.LoginItemRegistered = true;
    }

    /// <summary>Immer frisch gelesen — der Eintrag lässt sich auch im Task-Manager abschalten.</summary>
    public static bool IsEnabled
    {
        get
        {
            using var run = Registry.CurrentUser.OpenSubKey(RunKey);
            if (run?.GetValue(ValueName) is not string) return false;
            using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey);
            // Erstes Byte ungerade (z. B. 3) = im Task-Manager deaktiviert.
            return approved?.GetValue(ValueName) is not byte[] { Length: > 0 } b || (b[0] & 1) == 0;
        }
    }

    public static bool SetEnabled(bool an)
    {
        try
        {
            using var run = Registry.CurrentUser.CreateSubKey(RunKey);
            if (an) run.SetValue(ValueName, Command);
            else run.DeleteValue(ValueName, false);
            if (an)
            {
                using var approved = Registry.CurrentUser.OpenSubKey(ApprovedKey, writable: true);
                approved?.DeleteValue(ValueName, false);
            }
            Diagnose.Log($"Anmeldeobjekt {(an ? "eingeschaltet" : "ausgeschaltet")}");
            return true;
        }
        catch (Exception e)
        {
            Diagnose.Log($"Anmeldeobjekt umschalten fehlgeschlagen: {e.Message}");
            return false;
        }
    }
}

/// <summary>Hell/Dunkel und Akzentfarbe aus den Windows-Einstellungen.</summary>
internal static class Theme
{
    private const string Personalize = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    public static bool AppsDark => ReadDword(Personalize, "AppsUseLightTheme", 1) == 0;
    public static bool TaskbarDark => ReadDword(Personalize, "SystemUsesLightTheme", 0) == 0;

    public static Color Accent
    {
        get
        {
            var abgr = ReadDword(@"Software\Microsoft\Windows\DWM", "AccentColor", -1);
            if (abgr == -1) return Color.FromArgb(0x00, 0x67, 0xC0);
            return Color.FromArgb(abgr & 0xFF, (abgr >> 8) & 0xFF, (abgr >> 16) & 0xFF);
        }
    }

    private static int ReadDword(string key, string name, int fallback)
    {
        using var k = Registry.CurrentUser.OpenSubKey(key);
        return k?.GetValue(name) is int v ? v : fallback;
    }
}

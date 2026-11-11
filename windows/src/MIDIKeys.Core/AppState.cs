using System.Text.Json;

namespace MIDIKeys.Core;

/// <summary>Was gemerkt wird. Gegenstück zu den UserDefaults-Schlüsseln der Mac-App.</summary>
public sealed class Settings
{
    public ushort HotkeyKey { get; set; } = Hotkey.Default.Key;
    public int HotkeyModifiers { get; set; } = (int)Hotkey.Default.Modifiers;
    public int OctaveShift { get; set; }
    public bool PianoModeEnabled { get; set; }
    public bool AdvancedLayoutEnabled { get; set; }
    public bool CUndRFrei { get; set; }
    public string? Sprache { get; set; }
    /// <summary>Name des MIDI-Ausgangs; null = automatisch (virtueller Port "MIDIKeys", sonst der erste passende).</summary>
    public string? OutputDevice { get; set; }
    public bool LoginItemRegistered { get; set; }
}

public interface ISettingsStore
{
    Settings Load();
    void Save(Settings settings);
}

public sealed class MemorySettingsStore(Settings? initial = null) : ISettingsStore
{
    public Settings Current { get; private set; } = initial ?? new Settings();
    public Settings Load() => Current;
    public void Save(Settings settings) => Current = settings;
}

/// <summary>Speichert als JSON, z. B. unter %APPDATA%\MIDIKeys\settings.json.</summary>
public sealed class JsonSettingsStore(string path) : ISettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public Settings Load()
    {
        try
        {
            if (File.Exists(path)) return JsonSerializer.Deserialize<Settings>(File.ReadAllText(path), Options) ?? new();
        }
        catch (Exception) { /* kaputte Datei -> Standardwerte */ }
        return new Settings();
    }

    public void Save(Settings settings)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, JsonSerializer.Serialize(settings, Options));
        }
        catch (Exception) { /* nicht kritisch */ }
    }
}

/// <summary>
/// Der Zustand der App. Wird von der Oberfläche gelesen und vom Tastatur-Hook
/// geschrieben — beides auf dem UI-Thread. <see cref="Changed"/> meldet jede Änderung.
/// </summary>
public sealed class AppState
{
    private readonly ISettingsStore _store;
    private readonly Settings _settings;

    public event Action? Changed;

    public AppState(ISettingsStore store)
    {
        _store = store;
        _settings = store.Load();
        var shift = _settings.OctaveShift;
        if (shift >= Keyboard.MinShift && shift <= Keyboard.MaxShift) Keyboard.Shift(shift);
        _sprache = SpracheExtensions.FromCode(_settings.Sprache) ?? SpracheExtensions.AusSystem();
        if (_settings.AdvancedLayoutEnabled) KlemmeLage();
        // Unbekannte Bits aus einer handbearbeiteten Datei verwerfen — sonst passte der Shortcut nie.
        var hk = new Hotkey(_settings.HotkeyKey, (Mods)(_settings.HotkeyModifiers & 0xF));
        _hotkey = Hotkey.IsValid(hk.Modifiers) ? hk : Hotkey.Default;
    }

    private void Save() => _store.Save(_settings);
    internal void NotifyChanged() => Changed?.Invoke();

    private bool _isActive;
    /// <summary>Ob der MIDI-Modus läuft. Bewusst nicht gemerkt.</summary>
    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive == value) return; _isActive = value; NotifyChanged(); }
    }

    public Keyboard Keyboard { get; } = new();

    /// <summary>Gerade gedrückte Tasten — fürs Hervorheben im Cheatsheet.</summary>
    public HashSet<ushort> PressedKeys { get; } = new();

    private Hotkey _hotkey;
    public Hotkey Hotkey
    {
        get => _hotkey;
        set
        {
            _hotkey = value;
            _settings.HotkeyKey = value.Key;
            _settings.HotkeyModifiers = (int)value.Modifiers;
            Save(); NotifyChanged();
        }
    }

    public bool PianoModeEnabled
    {
        get => _settings.PianoModeEnabled;
        set { _settings.PianoModeEnabled = value; Save(); NotifyChanged(); }
    }

    private bool _pianoModeAvailable = true;
    /// <summary>Ob der lokale Klavier-Ton (GS-Wavetable-Synth) verfügbar ist.</summary>
    public bool PianoModeAvailable
    {
        get => _pianoModeAvailable;
        set { _pianoModeAvailable = value; NotifyChanged(); }
    }

    public bool AdvancedLayoutEnabled
    {
        get => _settings.AdvancedLayoutEnabled;
        set
        {
            _settings.AdvancedLayoutEnabled = value;
            if (value) KlemmeLage();
            Save(); NotifyChanged();
        }
    }

    /// <summary>Ob c und r im Grundlayout mit MIDI-Ausgang durchgehen (z. B. für Ableton).</summary>
    public bool CUndRFrei
    {
        get => _settings.CUndRFrei;
        set { _settings.CUndRFrei = value; Save(); NotifyChanged(); }
    }

    private Sprache _sprache;
    public Sprache Sprache
    {
        get => _sprache;
        set { _sprache = value; _settings.Sprache = value.Code(); Save(); NotifyChanged(); }
    }

    public string? OutputDevice
    {
        get => _settings.OutputDevice;
        set { _settings.OutputDevice = value; Save(); NotifyChanged(); }
    }

    public bool LoginItemRegistered
    {
        get => _settings.LoginItemRegistered;
        set { _settings.LoginItemRegistered = value; Save(); }
    }

    private string? _tapError;
    /// <summary>Fehlermeldung, wenn der Tastatur-Hook nicht angelegt werden konnte.</summary>
    public string? TapError
    {
        get => _tapError;
        set { _tapError = value; NotifyChanged(); }
    }

    private string _outputStatus = "";
    /// <summary>Wohin gerade gesendet wird, für die Fußzeile.</summary>
    public string OutputStatus
    {
        get => _outputStatus;
        set { _outputStatus = value; NotifyChanged(); }
    }

    public Func<ushort, KeyAction?> CurrentKeyAction =>
        AdvancedLayoutEnabled ? AdvancedKeyMap.Action : KeyMap.Action;

    public int MaxSemitone => AdvancedLayoutEnabled ? AdvancedKeyMap.MaxSemitone : KeyMap.MaxSemitone;

    /// <summary>
    /// Das Adv.-Layout reicht weiter nach oben: Eine Lage von +4/+5 aus dem
    /// Grundlayout schöbe die höchsten Tasten über MIDI 127. Darum beim
    /// Umschalten auf den erlaubten Bereich zurückziehen.
    /// </summary>
    private void KlemmeLage()
    {
        var zuviel = Keyboard.OctaveShift - AdvancedKeyMap.MaxShift;
        if (zuviel <= 0) return;
        Keyboard.Shift(-zuviel);
        _settings.OctaveShift = Keyboard.OctaveShift;
    }

    public void ShiftOctave(int delta)
    {
        var (min, max) = AdvancedLayoutEnabled
            ? (AdvancedKeyMap.MinShift, AdvancedKeyMap.MaxShift)
            : (Keyboard.MinShift, Keyboard.MaxShift);
        var target = Keyboard.OctaveShift + delta;
        if (target < min || target > max) return;
        if (!Keyboard.Shift(delta)) return;
        _settings.OctaveShift = Keyboard.OctaveShift;
        Save(); NotifyChanged();
    }

    /// <summary>Name des gehaltenen Akkords/Intervalls, oder null.</summary>
    public string? ChordName
    {
        get
        {
            var aktion = CurrentKeyAction;
            var noten = PressedKeys
                .Select(k => aktion(k))
                .Where(a => a is { Kind: KeyActionKind.Note })
                .Select(a => (int)Keyboard.Note(a!.Value.Semitone))
                .ToList();
            if (noten.Count == 0) return null;
            var pitchClasses = noten.Select(n => n % 12).ToHashSet();
            return ChordNamer.Name(pitchClasses, noten.Min() % 12, Sprache);
        }
    }
}

namespace MIDIKeys.Core;

public enum Sprache { Deutsch, English }

public static class SpracheExtensions
{
    /// <summary>Ohne gespeicherte Wahl: Deutsch, wenn das System Deutsch bevorzugt, sonst Englisch.</summary>
    public static Sprache AusSystem(string? cultureName = null)
    {
        var name = cultureName ?? System.Globalization.CultureInfo.CurrentUICulture.Name;
        return name.StartsWith("de", StringComparison.OrdinalIgnoreCase) ? Sprache.Deutsch : Sprache.English;
    }

    /// <summary>Name in der eigenen Sprache.</summary>
    public static string Name(this Sprache s) => s == Sprache.Deutsch ? "Deutsch" : "English";

    public static string Text(this Sprache s, string deutsch, string english) =>
        s == Sprache.Deutsch ? deutsch : english;

    public static string Code(this Sprache s) => s == Sprache.Deutsch ? "de" : "en";

    public static Sprache? FromCode(string? code) => code switch
    {
        "de" => Sprache.Deutsch,
        "en" => Sprache.English,
        _ => null,
    };
}

public static class Notes
{
    /// <summary>Notennamen ohne Oktave, Kreuze für schwarze Tasten. Deutsch mit H, Englisch mit B.</summary>
    public static string[] Names(Sprache sprache) =>
        ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", sprache.Text("H", "B")];

    /// <summary>Notenname mit Oktavzahl nach der Konvention C3 = MIDI 48.</summary>
    public static string Name(int midi, Sprache sprache = Sprache.Deutsch)
    {
        var name = Names(sprache)[((midi % 12) + 12) % 12];
        var octave = (int)Math.Floor(midi / 12.0) - 1;
        return $"{name}{octave}";
    }
}

/// <summary>Die Klaviatur und ihre aktuelle Lage.</summary>
public sealed class Keyboard
{
    /// <summary>Linke weiße Taste im Grundzustand.</summary>
    public const int BaseNote = 48;

    /// <summary>
    /// Zulässige Oktav-Verschiebungen: Alle 20 Tasten bleiben in 0…127 —
    /// bei −4 liegt die tiefste Taste auf 0, bei +5 die höchste auf 127.
    /// </summary>
    public const int MinShift = -4, MaxShift = 5;

    public int OctaveShift { get; private set; }

    /// <summary>Verschiebt die Lage. Gibt false zurück und ändert nichts, wenn das Ziel außerhalb läge.</summary>
    public bool Shift(int delta)
    {
        var target = OctaveShift + delta;
        if (target < MinShift || target > MaxShift) return false;
        OctaveShift = target;
        return true;
    }

    public byte Note(int semitone) => (byte)(BaseNote + OctaveShift * 12 + semitone);

    /// <summary>Aktuelle Lage als "C3–G4".</summary>
    public string RangeDescription(int maxSemitone = KeyMap.MaxSemitone, Sprache sprache = Sprache.Deutsch) =>
        $"{Notes.Name(Note(0), sprache)}–{Notes.Name(Note(maxSemitone), sprache)}";
}

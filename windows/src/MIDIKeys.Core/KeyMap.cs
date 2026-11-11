namespace MIDIKeys.Core;

public enum KeyActionKind { Note, OctaveDown, OctaveUp, Sustain }

/// <summary>Was eine physische Taste im MIDI-Modus auslöst.</summary>
public readonly record struct KeyAction(KeyActionKind Kind, int Semitone = 0)
{
    /// <summary>Halbton-Abstand zur aktuellen Basisnote.</summary>
    public static KeyAction Note(int semitone) => new(KeyActionKind.Note, semitone);
    public static readonly KeyAction OctaveDown = new(KeyActionKind.OctaveDown);
    public static readonly KeyAction OctaveUp = new(KeyActionKind.OctaveUp);
    public static readonly KeyAction Sustain = new(KeyActionKind.Sustain);
}

/// <summary>
/// Die Zuordnung physischer Tasten zur Klaviatur (Grundlayout).
///
/// Bewusst über Scancodes statt über Zeichen: Die Klaviatur ist eine
/// räumliche Anordnung (Home-Reihe = weiße Tasten, obere Reihe = schwarze
/// dazwischen). Die Beschriftung im Cheatsheet wird zur Laufzeit aus dem
/// aktiven Layout aufgelöst.
/// </summary>
public static class KeyMap
{
    // Weiße Tasten, Home-Reihe: a s d f g h j k l ö ä #
    private static readonly (ushort Key, int Semitone)[] WhiteKeys =
    [
        (ScanCodes.A, 0),          // C
        (ScanCodes.S, 2),          // D
        (ScanCodes.D, 4),          // E
        (ScanCodes.F, 5),          // F
        (ScanCodes.G, 7),          // G
        (ScanCodes.H, 9),          // A
        (ScanCodes.J, 11),         // H
        (ScanCodes.K, 12),         // C
        (ScanCodes.L, 14),         // D
        (ScanCodes.Semicolon, 16), // E  (QWERTZ ö)
        (ScanCodes.Quote, 17),     // F  (QWERTZ ä)
        (ScanCodes.Backslash, 19), // G  (QWERTZ #)
    ];

    // Schwarze Tasten, obere Reihe: w e t z u o p +
    private static readonly (ushort Key, int Semitone)[] BlackKeys =
    [
        (ScanCodes.W, 1),             // C#
        (ScanCodes.E, 3),             // D#
        (ScanCodes.T, 6),             // F#
        (ScanCodes.Y, 8),             // G#  (QWERTZ z)
        (ScanCodes.U, 10),            // A#
        (ScanCodes.O, 13),            // C#
        (ScanCodes.P, 15),            // D#
        (ScanCodes.RightBracket, 18), // F#  (QWERTZ +)
    ];

    public const ushort OctaveDownKey = ScanCodes.Z;  // QWERTZ y
    public const ushort OctaveUpKey = ScanCodes.X;
    public const ushort SustainKey = ScanCodes.Tab;

    public const int MaxSemitone = 19;

    /// <summary>Halbton -> Taste, lückenlos über 0…19.</summary>
    public static readonly IReadOnlyDictionary<int, ushort> NoteKeys =
        WhiteKeys.Concat(BlackKeys).ToDictionary(e => e.Semitone, e => e.Key);

    private static readonly Dictionary<ushort, KeyAction> Table = BuildTable();

    private static Dictionary<ushort, KeyAction> BuildTable()
    {
        var result = new Dictionary<ushort, KeyAction>();
        foreach (var (key, semitone) in WhiteKeys.Concat(BlackKeys))
            result[key] = KeyAction.Note(semitone);
        result[OctaveDownKey] = KeyAction.OctaveDown;
        result[OctaveUpKey] = KeyAction.OctaveUp;
        result[SustainKey] = KeyAction.Sustain;
        return result;
    }

    public static KeyAction? Action(ushort key) => Table.TryGetValue(key, out var a) ? a : null;

    /// <summary>True für die schwarzen Tasten — fürs Zeichnen der Klaviatur.</summary>
    public static bool IsBlackKey(int semitone) =>
        (((semitone % 12) + 12) % 12) is 1 or 3 or 6 or 8 or 10;
}

/// <summary>
/// Erweiterte Tastenbelegung ("Adv."): vier statt zwei Tastaturreihen, 37
/// Noten (C3 bis C6) über 39 Tasten. <c>-</c> und <c>q</c> spielen beide F4,
/// <c>ä</c> und <c>2</c> beide F#4. Positionen wie im macOS-Original
/// (deutsches ISO-Layout); <c>&lt;</c> fehlt auf ANSI-Tastaturen.
/// </summary>
public static class AdvancedKeyMap
{
    // Weiße Tasten: < y x c v b n m , . - q w e r t z u i o p ü +
    private static readonly (ushort Key, int Semitone)[] WhiteKeys =
    [
        (ScanCodes.IsoBackslash, 0), // <   C3
        (ScanCodes.Z, 2),            // y   D3
        (ScanCodes.X, 4),            // x   E3
        (ScanCodes.C, 5),            // c   F3
        (ScanCodes.V, 7),            // v   G3
        (ScanCodes.B, 9),            // b   A3
        (ScanCodes.N, 11),           // n   H3
        (ScanCodes.M, 12),           // m   C4
        (ScanCodes.Comma, 14),       // ,   D4
        (ScanCodes.Period, 16),      // .   E4
        (ScanCodes.Slash, 17),       // -   F4
        (ScanCodes.Q, 17),           // q   F4  (Dopplung)
        (ScanCodes.W, 19),           // w   G4
        (ScanCodes.E, 21),           // e   A4
        (ScanCodes.R, 23),           // r   H4
        (ScanCodes.T, 24),           // t   C5
        (ScanCodes.Y, 26),           // z   D5
        (ScanCodes.U, 28),           // u   E5
        (ScanCodes.I, 29),           // i   F5
        (ScanCodes.O, 31),           // o   G5
        (ScanCodes.P, 33),           // p   A5
        (ScanCodes.LeftBracket, 35), // ü   H5
        (ScanCodes.RightBracket, 36),// +   C6
    ];

    // Schwarze Tasten: a s f g h k l ä 2 3 4 6 7 9 0 ß
    private static readonly (ushort Key, int Semitone)[] BlackKeys =
    [
        (ScanCodes.A, 1),       // a   C#3
        (ScanCodes.S, 3),       // s   D#3
        (ScanCodes.F, 6),       // f   F#3
        (ScanCodes.G, 8),       // g   G#3
        (ScanCodes.H, 10),      // h   A#3
        (ScanCodes.K, 13),      // k   C#4
        (ScanCodes.L, 15),      // l   D#4
        (ScanCodes.Quote, 18),  // ä   F#4
        (ScanCodes.D2, 18),     // 2   F#4  (Dopplung)
        (ScanCodes.D3, 20),     // 3   G#4
        (ScanCodes.D4, 22),     // 4   A#4
        (ScanCodes.D6, 25),     // 6   C#5
        (ScanCodes.D7, 27),     // 7   D#5
        (ScanCodes.D9, 30),     // 9   F#5
        (ScanCodes.D0, 32),     // 0   G#5
        (ScanCodes.Minus, 34),  // ß   A#5
    ];

    /// <summary><c>^</c> links neben "1", analog zu <c>y</c> im Grundmodus.</summary>
    public const ushort OctaveDownKey = ScanCodes.Grave;
    /// <summary><c>1</c>, analog zu <c>x</c> im Grundmodus.</summary>
    public const ushort OctaveUpKey = ScanCodes.D1;
    public const ushort SustainKey = KeyMap.SustainKey;

    public const int MaxSemitone = 36;

    /// <summary>Wie <see cref="Keyboard.MinShift"/>/<see cref="Keyboard.MaxShift"/>, für den breiteren Tonumfang.</summary>
    public const int MinShift = -4, MaxShift = 3;

    /// <summary>Halbton -> repräsentative Taste (bei Dopplungen die zuerst genannte).</summary>
    public static readonly IReadOnlyDictionary<int, ushort> NoteKeys = BuildNoteKeys();

    private static Dictionary<int, ushort> BuildNoteKeys()
    {
        var result = new Dictionary<int, ushort>();
        foreach (var (key, semitone) in WhiteKeys.Concat(BlackKeys))
            result.TryAdd(semitone, key);
        return result;
    }

    private static readonly Dictionary<ushort, KeyAction> Table = BuildTable();

    private static Dictionary<ushort, KeyAction> BuildTable()
    {
        var result = new Dictionary<ushort, KeyAction>();
        foreach (var (key, semitone) in WhiteKeys.Concat(BlackKeys))
            result[key] = KeyAction.Note(semitone);
        result[OctaveDownKey] = KeyAction.OctaveDown;
        result[OctaveUpKey] = KeyAction.OctaveUp;
        result[SustainKey] = KeyAction.Sustain;
        return result;
    }

    public static KeyAction? Action(ushort key) => Table.TryGetValue(key, out var a) ? a : null;
}

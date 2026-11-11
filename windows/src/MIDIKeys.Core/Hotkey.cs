namespace MIDIKeys.Core;

/// <summary>Modifier-Zustand, wie ihn der Tastatur-Hook mitführt.</summary>
[Flags]
public enum Mods
{
    None = 0,
    Shift = 1,
    Ctrl = 2,
    Alt = 4,
    /// <summary>Die Windows-Taste — die Rolle von ⌘ auf dem Mac.</summary>
    Win = 8,
}

/// <summary>Eine Tastenkombination zum Umschalten des MIDI-Modus.</summary>
public readonly record struct Hotkey(ushort Key, Mods Modifiers)
{
    /// <summary>Alt + die physische Taste rechts neben dem Punkt (deutsch: <c>-</c>) — wie ⌥- auf dem Mac.</summary>
    public static readonly Hotkey Default = new(ScanCodes.Slash, Mods.Alt);

    public bool Matches(ushort key, Mods mods) => Key == key && mods == Modifiers;

    /// <summary>Ein Shortcut braucht mindestens einen Modifier außer Shift — sonst würde er normales Tippen abfangen.</summary>
    public static bool IsValid(Mods modifiers) => (modifiers & (Mods.Ctrl | Mods.Alt | Mods.Win)) != 0;

    /// <summary>Modifier in Windows-Schreibweise, z. B. "Strg+Alt+".</summary>
    public string ModifierText(Sprache sprache)
    {
        var teile = new List<string>();
        if (Modifiers.HasFlag(Mods.Ctrl)) teile.Add(sprache.Text("Strg", "Ctrl"));
        if (Modifiers.HasFlag(Mods.Win)) teile.Add("Win");
        if (Modifiers.HasFlag(Mods.Alt)) teile.Add("Alt");
        if (Modifiers.HasFlag(Mods.Shift)) teile.Add(sprache.Text("Umschalt", "Shift"));
        return teile.Count == 0 ? "" : string.Join("+", teile) + "+";
    }

    public string DisplayName(string keyLabel, Sprache sprache) => ModifierText(sprache) + keyLabel;

    /// <summary>Wandelt ein aufgezeichnetes Tastenereignis in einen Hotkey; null, wenn untauglich.</summary>
    public static Hotkey? FromRecording(ushort key, Mods mods) => IsValid(mods) ? new Hotkey(key, mods) : null;
}

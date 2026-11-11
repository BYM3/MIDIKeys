namespace MIDIKeys.Core;

public enum DecisionKind { PassThrough, Swallow, NoteOn, NoteOff, Octave, Sustain, Toggle }

/// <summary>Was mit einem Tastenereignis geschehen soll.</summary>
public readonly record struct Decision(DecisionKind Kind, int Value = 0)
{
    public static readonly Decision PassThrough = new(DecisionKind.PassThrough);
    public static readonly Decision Swallow = new(DecisionKind.Swallow);
    public static readonly Decision Toggle = new(DecisionKind.Toggle);
    public static Decision NoteOn(int semitone) => new(DecisionKind.NoteOn, semitone);
    public static Decision NoteOff(int semitone) => new(DecisionKind.NoteOff, semitone);
    public static Decision Octave(int delta) => new(DecisionKind.Octave, delta);
    public static Decision Sustain(bool on) => new(DecisionKind.Sustain, on ? 1 : 0);
}

/// <summary>
/// Die Entscheidungsregeln des Tastatur-Hooks — eine reine Funktion, damit
/// die sicherheitskritischen Durchlass-Regeln ohne echten Hook testbar sind.
/// </summary>
public static class TapPolicy
{
    /// <summary>← → ↓ ↑ und Leertaste.</summary>
    internal static readonly HashSet<ushort> PassThroughKeys =
        [ScanCodes.Left, ScanCodes.Right, ScanCodes.Down, ScanCodes.Up, ScanCodes.Space];

    /// <summary>
    /// Modifier-Tasten selbst. Auf dem Mac erreichen sie den Event-Tap gar
    /// nicht als keyDown; unter Windows schon — sie müssen immer durch,
    /// sonst hingen Strg/Alt/Win im System fest.
    /// </summary>
    public static readonly HashSet<ushort> ModifierKeys =
    [
        ScanCodes.LeftShift, ScanCodes.RightShift, ScanCodes.LeftCtrl, ScanCodes.RightCtrl,
        ScanCodes.LeftAlt, ScanCodes.RightAlt, ScanCodes.LeftWin, ScanCodes.RightWin,
        ScanCodes.CapsLock,
    ];

    /// <summary>Alt+Tab, Alt+F4, Alt+Esc: Fenster wechseln und schließen bleibt immer möglich.</summary>
    internal static readonly HashSet<ushort> AltSystemKeys = [ScanCodes.Tab, ScanCodes.F4, ScanCodes.Escape];

    public static Decision Decide(
        ushort key, Mods mods, bool isKeyDown, bool isRepeat, bool isActive, Hotkey hotkey,
        Func<ushort, KeyAction?>? keyAction = null, IReadOnlySet<ushort>? extraPassThrough = null)
    {
        keyAction ??= KeyMap.Action;

        // 0. Modifier-Tasten gehen immer durch.
        if (ModifierKeys.Contains(key)) return Decision.PassThrough;

        // 1. Der Hotkey zuerst — er könnte selbst Strg/Win enthalten.
        if (hotkey.Matches(key, mods))
            return isKeyDown && !isRepeat ? Decision.Toggle : Decision.Swallow;

        // 2. Alles mit Strg oder Win geht immer durch (Strg+Umschalt+Esc,
        //    Win+L, Strg+C …) — das Gegenstück zur ⌘-Regel auf dem Mac.
        //    Ebenso Alt+Tab, Alt+F4 und Alt+Esc.
        if ((mods & (Mods.Ctrl | Mods.Win)) != 0) return Decision.PassThrough;
        if ((mods & Mods.Alt) != 0 && AltSystemKeys.Contains(key)) return Decision.PassThrough;

        // 3. Im inaktiven Modus ist die Tastatur ganz normal.
        if (!isActive) return Decision.PassThrough;

        // 4. Pfeiltasten und Leertaste gehen immer durch (DAW im Hintergrund
        //    bleibt bedienbar), dazu modusabhängige Extras.
        if (PassThroughKeys.Contains(key) || (extraPassThrough?.Contains(key) ?? false))
            return Decision.PassThrough;

        // 5. Key-Repeat nie in MIDI übersetzen.
        if (isRepeat) return Decision.Swallow;

        // 6. Gemappte Tasten spielen, alles andere wird still geschluckt.
        var action = keyAction(key);
        if (action is null) return Decision.Swallow;
        return action.Value.Kind switch
        {
            KeyActionKind.Note => isKeyDown ? Decision.NoteOn(action.Value.Semitone) : Decision.NoteOff(action.Value.Semitone),
            KeyActionKind.OctaveDown => isKeyDown ? Decision.Octave(-1) : Decision.Swallow,
            KeyActionKind.OctaveUp => isKeyDown ? Decision.Octave(1) : Decision.Swallow,
            KeyActionKind.Sustain => Decision.Sustain(isKeyDown),
            _ => Decision.Swallow,
        };
    }
}

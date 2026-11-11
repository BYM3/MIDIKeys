/// Was mit einem Tastenereignis geschehen soll.
public enum Decision: Equatable, Sendable {
    /// Event unverändert an die nächste App weiterreichen.
    case passThrough
    /// Event verschlucken, sonst nichts tun.
    case swallow
    case noteOn(semitone: Int)
    case noteOff(semitone: Int)
    case octave(Int)
    case sustain(Bool)
    case toggle
}

/// Die Entscheidungsregeln des Event-Taps — bewusst eine reine Funktion,
/// damit die sicherheitskritische Cmd-Regel ohne Systemberechtigungen
/// testbar ist.
public enum TapPolicy {
    /// ← → ↓ ↑ und Leertaste (physische Keycodes, layoutunabhängig).
    static let passThroughKeycodes: Set<UInt16> = [0x7B, 0x7C, 0x7D, 0x7E, 0x31]

    public static func decide(
        keycode: UInt16,
        flags: UInt64,
        isKeyDown: Bool,
        isRepeat: Bool,
        isActive: Bool,
        hotkey: Hotkey,
        keyAction: (UInt16) -> KeyAction? = KeyMap.action(for:),
        extraPassThrough: Set<UInt16> = []
    ) -> Decision {
        // 1. Der Hotkey zuerst — er könnte selbst Cmd enthalten und würde
        //    sonst von Regel 2 durchgereicht, statt umzuschalten.
        if hotkey.matches(keycode: keycode, flags: flags) {
            return isKeyDown && !isRepeat ? .toggle : .swallow
        }

        // 2. Alles mit Cmd geht immer durch. Garantiert Cmd+Opt+Esc
        //    (Sofort beenden) und hält Cmd+Tab/Cmd+Q nutzbar.
        if flags & CGEventFlagsRaw.command != 0 {
            return .passThrough
        }

        // 3. Im inaktiven Modus ist die Tastatur ganz normal.
        guard isActive else { return .passThrough }

        // 4. Pfeiltasten und Leertaste gehen immer durch — so bleibt ein
        //    Programm im Hintergrund (z. B. die DAW, Play/Stop) bedienbar.
        //    Vor Regel 5, damit gehaltene Pfeile per Autorepeat weiterscrollen.
        //    `extraPassThrough` ergänzt das um Tasten, die nur in bestimmten
        //    Betriebsarten durchgehen (siehe `InputController`).
        if passThroughKeycodes.contains(keycode) || extraPassThrough.contains(keycode) {
            return .passThrough
        }

        // 5. Key-Repeat nie in MIDI übersetzen.
        if isRepeat { return .swallow }

        // 6. Gemappte Tasten spielen, alles andere wird still geschluckt.
        //    Welche Tabelle das ist (Grund- oder Adv.-Layout), entscheidet
        //    der Aufrufer über `keyAction` — Standard ist KeyMap, damit
        //    bestehende Aufrufstellen unverändert funktionieren.
        switch keyAction(keycode) {
        case let .note(semitone):
            return isKeyDown ? .noteOn(semitone: semitone) : .noteOff(semitone: semitone)
        case .octaveDown:
            return isKeyDown ? .octave(-1) : .swallow
        case .octaveUp:
            return isKeyDown ? .octave(1) : .swallow
        case .sustain:
            return .sustain(isKeyDown)
        case nil:
            return .swallow
        }
    }
}

/// Was eine physische Taste im MIDI-Modus auslöst.
public enum KeyAction: Equatable, Sendable {
    /// Halbton-Abstand zur aktuellen Basisnote.
    case note(semitone: Int)
    case octaveDown
    case octaveUp
    case sustain
}

/// Die Zuordnung physischer Tasten zur Klaviatur.
///
/// Bewusst über Virtual Keycodes statt über Zeichen: Die Klaviatur ist eine
/// räumliche Anordnung (untere Buchstabenreihe = weiße Tasten, obere Reihe =
/// schwarze dazwischen). Keycodes erhalten diese Geometrie unabhängig vom
/// aktiven Tastaturlayout, und im Event-Tap-Callback wäre eine
/// Zeichendekodierung zu teuer.
///
/// Die Beschriftung fürs Cheatsheet wird umgekehrt zur Laufzeit aus dem
/// aktiven Layout aufgelöst — siehe `KeyLabelResolver`.
public enum KeyMap {
    // Weiße Tasten, Home-Reihe: a s d f g h j k l ö ä #
    private static let whiteKeys: [(keycode: UInt16, semitone: Int)] = [
        (0x00, 0),   // kVK_ANSI_A          C
        (0x01, 2),   // kVK_ANSI_S          D
        (0x02, 4),   // kVK_ANSI_D          E
        (0x03, 5),   // kVK_ANSI_F          F
        (0x05, 7),   // kVK_ANSI_G          G
        (0x04, 9),   // kVK_ANSI_H          A
        (0x26, 11),  // kVK_ANSI_J          H
        (0x28, 12),  // kVK_ANSI_K          C
        (0x25, 14),  // kVK_ANSI_L          D
        (0x29, 16),  // kVK_ANSI_Semicolon  E  (QWERTZ ö)
        (0x27, 17),  // kVK_ANSI_Quote      F  (QWERTZ ä)
        (0x2A, 19),  // kVK_ANSI_Backslash  G  (QWERTZ #)
    ]

    // Schwarze Tasten, obere Reihe: w e t z u o p +
    private static let blackKeys: [(keycode: UInt16, semitone: Int)] = [
        (0x0D, 1),   // kVK_ANSI_W             C#
        (0x0E, 3),   // kVK_ANSI_E             D#
        (0x11, 6),   // kVK_ANSI_T             F#
        (0x10, 8),   // kVK_ANSI_Y             G#  (QWERTZ z sitzt hier)
        (0x20, 10),  // kVK_ANSI_U             A#
        (0x1F, 13),  // kVK_ANSI_O             C#
        (0x23, 15),  // kVK_ANSI_P             D#
        (0x1E, 18),  // kVK_ANSI_RightBracket  F#  (QWERTZ +)
    ]

    public static let octaveDownKeycode: UInt16 = 0x06  // kVK_ANSI_Z (QWERTZ y)
    public static let octaveUpKeycode: UInt16 = 0x07    // kVK_ANSI_X (QWERTZ x)
    public static let sustainKeycode: UInt16 = 0x30     // kVK_Tab

    public static let semitoneRange: ClosedRange<Int> = 0...19

    /// Die 20 Notentasten, aufsteigend nach Tonhöhe.
    public static let noteKeycodes: [UInt16] =
        (whiteKeys + blackKeys).sorted { $0.semitone < $1.semitone }.map(\.keycode)

    private static let table: [UInt16: KeyAction] = {
        var result: [UInt16: KeyAction] = [:]
        for entry in whiteKeys + blackKeys {
            result[entry.keycode] = .note(semitone: entry.semitone)
        }
        result[octaveDownKeycode] = .octaveDown
        result[octaveUpKeycode] = .octaveUp
        result[sustainKeycode] = .sustain
        return result
    }()

    public static func action(for keycode: UInt16) -> KeyAction? {
        table[keycode]
    }

    /// True für die schwarzen Tasten — fürs Zeichnen der Klaviatur.
    public static func isBlackKey(semitone: Int) -> Bool {
        [1, 3, 6, 8, 10].contains(((semitone % 12) + 12) % 12)
    }
}

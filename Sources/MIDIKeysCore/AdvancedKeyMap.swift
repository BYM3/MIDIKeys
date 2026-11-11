/// Erweiterte Tastenbelegung ("Adv."-Modus): dieselbe Klaviatur-Idee wie
/// `KeyMap`, aber über vier statt zwei Tastaturreihen gespannt — 37
/// eindeutige Noten (C3 bis C6) statt 20, über 39 Tasten erreichbar.
///
/// Zwei Notenpositionen haben je zwei gleichwertige Tasten (vom Nutzer
/// bestätigt, nach mehreren Korrekturrunden): `-` und `q` spielen beide F4,
/// `ä` und `2` spielen beide F#4. Die Duplikate verbrauchen dabei keine
/// eigene Position in der Halbtonfolge — die Folge bleibt lückenlos.
///
/// Verifiziert per Rückwärtssuche über `UCKeyTranslate` gegen das aktive
/// Tastaturlayout (Ziffernblock bewusst ausgeschlossen, siehe Kommentar bei
/// `whiteKeys`). `<` wurde gesondert geprüft: kVK_ANSI_Grave (0x32) erzeugt
/// auf diesem deutschen ISO-Layout tatsächlich "<", nicht "^" — das sitzt
/// auf einem anderen Keycode (kVK_ISO_Section, 0x0A).
public enum AdvancedKeyMap {
    // Weiße Tasten: < y x c v b n m , . - q w e r t z u i o p ü +
    private static let whiteKeys: [(keycode: UInt16, semitone: Int)] = [
        (0x32, 0),   // <   C3  (kVK_ANSI_Grave — auf DE-ISO physisch "<")
        (0x06, 2),   // y   D3
        (0x07, 4),   // x   E3
        (0x08, 5),   // c   F3
        (0x09, 7),   // v   G3
        (0x0B, 9),   // b   A3
        (0x2D, 11),  // n   H3
        (0x2E, 12),  // m   C4
        (0x2B, 14),  // ,   D4
        (0x2F, 16),  // .   E4
        (0x2C, 17),  // -   F4
        (0x0C, 17),  // q   F4  (Dopplung — dieselbe Note wie -)
        (0x0D, 19),  // w   G4
        (0x0E, 21),  // e   A4
        (0x0F, 23),  // r   H4
        (0x11, 24),  // t   C5
        (0x10, 26),  // z   D5
        (0x20, 28),  // u   E5
        (0x22, 29),  // i   F5
        (0x1F, 31),  // o   G5
        (0x23, 33),  // p   A5
        (0x21, 35),  // ü   H5
        (0x1E, 36),  // +   C6
    ]

    // Schwarze Tasten: a s f g h k l ä 2 3 4 6 7 9 0 ß
    private static let blackKeys: [(keycode: UInt16, semitone: Int)] = [
        (0x00, 1),   // a   C#3
        (0x01, 3),   // s   D#3
        (0x03, 6),   // f   F#3
        (0x05, 8),   // g   G#3
        (0x04, 10),  // h   A#3
        (0x28, 13),  // k   C#4
        (0x25, 15),  // l   D#4
        (0x27, 18),  // ä   F#4
        (0x13, 18),  // 2   F#4  (Dopplung — dieselbe Note wie ä)
        (0x14, 20),  // 3   G#4
        (0x15, 22),  // 4   A#4  (korrigiert: 5 war falsch, richtig ist 4)
        (0x16, 25),  // 6   C#5
        (0x1A, 27),  // 7   D#5
        (0x19, 30),  // 9   F#5
        (0x1D, 32),  // 0   G#5
        (0x1B, 34),  // ß   A#5  (korrigiert: ´ war falsch, richtig ist ß)
    ]

    /// `^` (kVK_ISO_Section, links neben "1"), analog zu `y` im Grundmodus.
    public static let octaveDownKeycode: UInt16 = 0x0A
    /// `1`, analog zu `x` im Grundmodus.
    public static let octaveUpKeycode: UInt16 = 0x12
    /// Sustain bleibt geteilt mit dem Grundmodus — dieselbe Taste, Tab.
    public static let sustainKeycode: UInt16 = KeyMap.sustainKeycode

    public static let semitoneRange: ClosedRange<Int> = 0...36

    /// Wie `Keyboard.shiftRange`, für den breiteren Tonumfang neu berechnet.
    public static let shiftRange: ClosedRange<Int> = -4...3

    /// Halbton -> eine repräsentative Taste dafür (bei den zwei Dopplungen
    /// die zuerst genannte: `-` statt `q`, `ä` statt `2`). Lückenlos über
    /// `semitoneRange`, anders als bei den Zwischenständen zuvor.
    public static let noteKeycodes: [Int: UInt16] = {
        var result: [Int: UInt16] = [:]
        for entry in whiteKeys + blackKeys where result[entry.semitone] == nil {
            result[entry.semitone] = entry.keycode
        }
        return result
    }()

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
}

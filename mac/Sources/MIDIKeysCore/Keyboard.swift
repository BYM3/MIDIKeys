/// Notennamen ohne Oktave, Kreuze für die schwarzen Tasten. Deutsch mit
/// H, Englisch mit B.
func noteNames(_ sprache: Sprache) -> [String] {
    ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", sprache.text("H", "B")]
}

/// Notenname mit Oktavzahl nach der Konvention C3 = MIDI 48.
public func noteName(forMIDI midi: Int, sprache: Sprache = .deutsch) -> String {
    let name = noteNames(sprache)[((midi % 12) + 12) % 12]
    let octave = Int((Double(midi) / 12.0).rounded(.down)) - 1
    return "\(name)\(octave)"
}

/// Die zweioktavige Klaviatur und ihre aktuelle Lage.
public struct Keyboard {
    /// Linke weiße Taste im Grundzustand.
    public static let baseNote = 48

    /// Zulässige Oktav-Verschiebungen. Die Grenzen sind so gewählt, dass
    /// *alle* 20 Tasten im gültigen MIDI-Bereich 0…127 bleiben:
    /// bei −4 liegt die tiefste Taste auf 0, bei +5 die höchste auf 127.
    public static let shiftRange: ClosedRange<Int> = -4...5

    public private(set) var octaveShift: Int = 0

    public init() {}

    /// Verschiebt die Lage. Gibt `false` zurück und ändert nichts,
    /// wenn das Ziel außerhalb des zulässigen Bereichs läge — kein Wrap-around.
    @discardableResult
    public mutating func shift(by delta: Int) -> Bool {
        let target = octaveShift + delta
        guard Keyboard.shiftRange.contains(target) else { return false }
        octaveShift = target
        return true
    }

    public func note(forSemitone semitone: Int) -> UInt8 {
        UInt8(Keyboard.baseNote + octaveShift * 12 + semitone)
    }

    /// Aktuelle Lage als "C3–G4". `maxSemitone` bestimmt die obere Grenze —
    /// Standard ist der Grundmodus (20 Tasten); das Adv.-Layout übergibt
    /// seinen eigenen, weiteren Wert.
    public func rangeDescription(
        maxSemitone: Int = KeyMap.semitoneRange.upperBound, sprache: Sprache = .deutsch
    ) -> String {
        let low = noteName(forMIDI: Int(note(forSemitone: 0)), sprache: sprache)
        let high = noteName(forMIDI: Int(note(forSemitone: maxSemitone)), sprache: sprache)
        return "\(low)–\(high)"
    }
}

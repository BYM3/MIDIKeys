/// Wohin MIDI-Nachrichten gehen. Das Protokoll existiert, damit die Engine
/// gegen einen Spy testbar ist, ohne CoreMIDI anzufassen.
public protocol MIDISending: AnyObject {
    func send(_ message: MIDIMessage)
}

/// Verwaltet die klingenden Noten und übersetzt Tastenaktionen in MIDI.
@MainActor
public final class MIDIEngine {
    public static let velocity: UInt8 = 100

    private let transport: MIDISending
    /// Halbton der Taste -> tatsächlich gesendete Notennummer.
    /// Nötig, damit das Note-Off die Note trifft, mit der der Ton begann,
    /// auch wenn zwischenzeitlich die Oktave gewechselt wurde.
    private var held: [Int: UInt8] = [:]

    public init(transport: MIDISending) {
        self.transport = transport
    }

    public var heldCount: Int { held.count }

    public func noteOn(semitone: Int, note: UInt8) {
        if let laufend = held[semitone] {
            transport.send(.noteOff(note: laufend))
        }
        held[semitone] = note
        transport.send(.noteOn(note: note, velocity: MIDIEngine.velocity))
    }

    public func noteOff(semitone: Int) {
        guard let note = held.removeValue(forKey: semitone) else { return }
        transport.send(.noteOff(note: note))
    }

    public func sustain(_ on: Bool) {
        transport.send(.controlChange(
            controller: MIDIMessage.sustainController,
            value: on ? 127 : 0
        ))
    }

    /// Beendet alles Klingende. Läuft beim Verlassen des aktiven Modus und
    /// beim Beenden der App, damit keine Töne hängen bleiben.
    public func panic() {
        for (_, note) in held {
            transport.send(.noteOff(note: note))
        }
        held.removeAll()
        transport.send(.controlChange(controller: MIDIMessage.sustainController, value: 0))
        transport.send(.controlChange(controller: MIDIMessage.allNotesOffController, value: 0))
    }
}

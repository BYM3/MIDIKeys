/// Leitet MIDI-Nachrichten exklusiv an eines von zwei Zielen weiter: das
/// externe (CoreMIDI) oder das lokale (Klavier-Ton).
///
/// Note-On folgt dem aktuellen Modus. Note-Off geht garantiert an dasselbe
/// Ziel wie das zugehörige Note-On — auch wenn der Modus zwischenzeitlich
/// umgeschaltet wurde, während die Taste noch gehalten war. Sonst bliebe
/// ein Ton am alten Ziel für immer hängen (in einer DAW etwa unhörbar,
/// aber unbegrenzt weiterklingend).
///
/// Das externe Ziel wird nie zerstört oder neu angelegt — nur die
/// Datenübertragung pausiert. Die virtuelle MIDI-Quelle bleibt darum in
/// anderen Programmen durchgehend gelistet.
public final class TransportRouter: MIDISending {
    private let external: MIDISending
    private let local: MIDISending
    private var routedTo: [UInt8: MIDISending] = [:]

    public var pianoModeEnabled = false

    public init(external: MIDISending, local: MIDISending) {
        self.external = external
        self.local = local
    }

    public func send(_ message: MIDIMessage) {
        let aktuellesZiel: MIDISending = pianoModeEnabled ? local : external

        switch message {
        case let .noteOn(note, _):
            routedTo[note] = aktuellesZiel
            aktuellesZiel.send(message)
        case let .noteOff(note):
            let ziel = routedTo.removeValue(forKey: note) ?? aktuellesZiel
            ziel.send(message)
        case .controlChange:
            external.send(message)
            local.send(message)
        }
    }
}

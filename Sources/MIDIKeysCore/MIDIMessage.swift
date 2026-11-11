/// Eine MIDI-1.0-Kanalnachricht auf Kanal 1.
/// Kanal ist bewusst fix — siehe Nicht-Ziele der Spec.
public enum MIDIMessage: Equatable, Sendable {
    case noteOn(note: UInt8, velocity: UInt8)
    case noteOff(note: UInt8)
    case controlChange(controller: UInt8, value: UInt8)

    public static let sustainController: UInt8 = 64
    public static let allNotesOffController: UInt8 = 123

    /// Die drei Bytes der Nachricht. Datenbytes sind auf 7 Bit maskiert,
    /// damit sie nie versehentlich als Status-Byte gelesen werden.
    public var bytes: [UInt8] {
        switch self {
        case let .noteOn(note, velocity):
            return [0x90, note & 0x7F, velocity & 0x7F]
        case let .noteOff(note):
            return [0x80, note & 0x7F, 0]
        case let .controlChange(controller, value):
            return [0xB0, controller & 0x7F, value & 0x7F]
        }
    }
}

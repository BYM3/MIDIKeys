import Testing
@testable import MIDIKeysCore

@Test func persistenteUniqueIdIstDeterministischUndPositiv() {
    // Deterministisch: derselbe Wert bei jedem Prozessstart, kein Zufall.
    let a = CoreMIDITransport.persistentUniqueID
    let b = CoreMIDITransport.persistentUniqueID
    #expect(a == b)
    #expect(a > 0)
}

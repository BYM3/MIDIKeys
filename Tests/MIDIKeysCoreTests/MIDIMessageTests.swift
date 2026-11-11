import Testing
@testable import MIDIKeysCore

@Test func noteOnErzeugtKorrekteBytes() {
    #expect(MIDIMessage.noteOn(note: 60, velocity: 100).bytes == [0x90, 60, 100])
}

@Test func noteOffErzeugtKorrekteBytes() {
    #expect(MIDIMessage.noteOff(note: 60).bytes == [0x80, 60, 0])
}

@Test func controlChangeErzeugtKorrekteBytes() {
    #expect(MIDIMessage.controlChange(controller: 64, value: 127).bytes == [0xB0, 64, 127])
}

@Test func datenbytesWerdenAufSiebenBitBegrenzt() {
    // Ein Datenbyte darf nie das oberste Bit setzen, sonst liest die DAW es als Status-Byte.
    #expect(MIDIMessage.noteOn(note: 200, velocity: 200).bytes == [0x90, 72, 72])
}

@Test func controllerNummernSindDieErwarteten() {
    #expect(MIDIMessage.sustainController == 64)
    #expect(MIDIMessage.allNotesOffController == 123)
}

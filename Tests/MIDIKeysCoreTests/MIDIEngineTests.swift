import Testing
@testable import MIDIKeysCore

private final class SpyTransport: MIDISending {
    var sent: [MIDIMessage] = []
    func send(_ message: MIDIMessage) { sent.append(message) }
}

@Test @MainActor func noteOnUndNoteOffWerdenGesendet() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.noteOn(semitone: 0, note: 48)
    engine.noteOff(semitone: 0)
    #expect(spy.sent == [.noteOn(note: 48, velocity: 100), .noteOff(note: 48)])
}

@Test @MainActor func noteOffBenutztDieBeimStartGesendeteNote() {
    // Der wichtigste Fall: Wird die Oktave gewechselt, während ein Ton klingt,
    // muss das Note-Off an die ursprüngliche Note gehen — sonst hängt der Ton.
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.noteOn(semitone: 0, note: 48)
    engine.noteOff(semitone: 0)  // die Oktave hat sich zwischenzeitlich geändert
    #expect(spy.sent.last == .noteOff(note: 48))
}

@Test @MainActor func noteOffOhneVorherigesNoteOnSendetNichts() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.noteOff(semitone: 5)
    #expect(spy.sent.isEmpty)
}

@Test @MainActor func erneutesNoteOnAufDerselbenTasteBeendetDenAltenTonZuerst() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.noteOn(semitone: 0, note: 48)
    engine.noteOn(semitone: 0, note: 60)
    #expect(spy.sent == [
        .noteOn(note: 48, velocity: 100),
        .noteOff(note: 48),
        .noteOn(note: 60, velocity: 100),
    ])
}

@Test @MainActor func sustainSendetControlChange64() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.sustain(true)
    engine.sustain(false)
    #expect(spy.sent == [
        .controlChange(controller: 64, value: 127),
        .controlChange(controller: 64, value: 0),
    ])
}

@Test @MainActor func panicBeendetAlleGehaltenenNotenUndSendetAllNotesOff() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.noteOn(semitone: 0, note: 48)
    engine.noteOn(semitone: 4, note: 52)
    engine.sustain(true)
    spy.sent.removeAll()

    engine.panic()

    // Reihenfolge: erst die gehaltenen Noten, dann Sustain lösen, dann CC123.
    let noteOffs = spy.sent.prefix(2)
    #expect(noteOffs.contains(.noteOff(note: 48)))
    #expect(noteOffs.contains(.noteOff(note: 52)))
    #expect(spy.sent[2] == .controlChange(controller: 64, value: 0))
    #expect(spy.sent[3] == .controlChange(controller: 123, value: 0))
    #expect(engine.heldCount == 0)
}

@Test @MainActor func panicOhneGehalteneNotenSendetTrotzdemAllNotesOff() {
    let spy = SpyTransport()
    let engine = MIDIEngine(transport: spy)
    engine.panic()
    #expect(spy.sent == [
        .controlChange(controller: 64, value: 0),
        .controlChange(controller: 123, value: 0),
    ])
}

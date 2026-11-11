import Testing
@testable import MIDIKeysCore

@Test func grundzustandBeginntBeiC3() {
    let keyboard = Keyboard()
    #expect(keyboard.octaveShift == 0)
    #expect(keyboard.note(forSemitone: 0) == 48)
    #expect(keyboard.note(forSemitone: 19) == 67)
}

@Test func oktavShiftVerschiebtUmZwoelfHalbtoene() {
    var keyboard = Keyboard()
    #expect(keyboard.shift(by: 1) == true)
    #expect(keyboard.note(forSemitone: 0) == 60)
    #expect(keyboard.shift(by: -2) == true)
    #expect(keyboard.note(forSemitone: 0) == 36)
}

@Test func shiftKlemmtAnDerUnterenGrenze() {
    var keyboard = Keyboard()
    for _ in 0..<4 { #expect(keyboard.shift(by: -1) == true) }
    #expect(keyboard.octaveShift == -4)
    #expect(keyboard.note(forSemitone: 0) == 0)
    // Ein weiterer Schritt würde unter MIDI 0 fallen und wird abgelehnt.
    #expect(keyboard.shift(by: -1) == false)
    #expect(keyboard.octaveShift == -4)
}

@Test func shiftKlemmtAnDerOberenGrenze() {
    var keyboard = Keyboard()
    for _ in 0..<5 { #expect(keyboard.shift(by: 1) == true) }
    #expect(keyboard.octaveShift == 5)
    // Höchste Taste landet exakt auf MIDI 127.
    #expect(keyboard.note(forSemitone: 19) == 127)
    #expect(keyboard.shift(by: 1) == false)
    #expect(keyboard.octaveShift == 5)
}

@Test func alleTastenBleibenInJederLageImGueltigenBereich() {
    for shift in Keyboard.shiftRange {
        var keyboard = Keyboard()
        _ = keyboard.shift(by: shift)
        for semitone in KeyMap.semitoneRange {
            let note = Int(keyboard.note(forSemitone: semitone))
            #expect(note >= 0 && note <= 127)
        }
    }
}

@Test func notennamenFolgenDerKonventionC3GleichAchtundvierzig() {
    #expect(noteName(forMIDI: 48) == "C3")
    #expect(noteName(forMIDI: 60) == "C4")
    #expect(noteName(forMIDI: 49) == "C#3")
    #expect(noteName(forMIDI: 59) == "H3")
    #expect(noteName(forMIDI: 0) == "C-1")
}

@Test func bereichsbeschreibungZeigtAktuelleLage() {
    var keyboard = Keyboard()
    #expect(keyboard.rangeDescription() == "C3–G4")
    _ = keyboard.shift(by: 1)
    #expect(keyboard.rangeDescription() == "C4–G5")
}

@Test func bereichsbeschreibungRespektiertDenAngegebenenTonumfang() {
    // Fürs Adv.-Layout: 38 statt 20 Tasten, höchster Halbton 37 statt 19.
    let keyboard = Keyboard()
    #expect(keyboard.rangeDescription(maxSemitone: 37) == "C3–C#6")
}

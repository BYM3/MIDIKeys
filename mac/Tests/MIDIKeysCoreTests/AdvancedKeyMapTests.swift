import Testing
@testable import MIDIKeysCore

@Test func weisseTastenLiefernDreiOktavenAmStueck() {
    // < y x c v b n m , . - q w e r t z u i o p ü +
    let erwartet: [(UInt16, Int)] = [
        (0x32, 0), (0x06, 2), (0x07, 4), (0x08, 5), (0x09, 7), (0x0B, 9), (0x2D, 11),
        (0x2E, 12), (0x2B, 14), (0x2F, 16), (0x2C, 17), (0x0C, 17), (0x0D, 19), (0x0E, 21),
        (0x0F, 23), (0x11, 24), (0x10, 26), (0x20, 28), (0x22, 29), (0x1F, 31),
        (0x23, 33), (0x21, 35), (0x1E, 36),
    ]
    for (keycode, semitone) in erwartet {
        #expect(AdvancedKeyMap.action(for: keycode) == .note(semitone: semitone))
    }
}

@Test func advSchwarzeTastenLiegenZwischenDenWeissen() {
    // a s f g h k l ä 2 3 4 6 7 9 0 ß
    let erwartet: [(UInt16, Int)] = [
        (0x00, 1), (0x01, 3), (0x03, 6), (0x05, 8), (0x04, 10), (0x28, 13), (0x25, 15),
        (0x27, 18), (0x13, 18), (0x14, 20), (0x15, 22), (0x16, 25), (0x1A, 27),
        (0x19, 30), (0x1D, 32), (0x1B, 34),
    ]
    for (keycode, semitone) in erwartet {
        #expect(AdvancedKeyMap.action(for: keycode) == .note(semitone: semitone))
    }
}

@Test func advSteuertastenSindZugeordnet() {
    #expect(AdvancedKeyMap.action(for: 0x0A) == .octaveDown)  // ^ (kVK_ISO_Section)
    #expect(AdvancedKeyMap.action(for: 0x12) == .octaveUp)    // 1
    #expect(AdvancedKeyMap.action(for: KeyMap.sustainKeycode) == .sustain)  // Tab bleibt geteilt
}

@Test func zweiPaareTastenTeilenSichJeEinenHalbton() {
    // Bestätigt nach mehreren Korrekturrunden: '-' und 'q' spielen beide
    // F4, 'ä' und '2' spielen beide F#4 — keine zwei unterschiedlichen
    // Noten, echte Dopplungen.
    #expect(AdvancedKeyMap.action(for: 0x2C) == .note(semitone: 17))  // -
    #expect(AdvancedKeyMap.action(for: 0x0C) == .note(semitone: 17))  // q
    #expect(AdvancedKeyMap.action(for: 0x27) == .note(semitone: 18))  // ä
    #expect(AdvancedKeyMap.action(for: 0x13) == .note(semitone: 18))  // 2
}

@Test func siebenunddreissigEindeutigeHalbtoeneOhneLuecke() {
    // 39 Tasten insgesamt (23 weiß + 16 schwarz), aber zwei Dopplungen ->
    // 37 eindeutige Halbtöne. Trotzdem lückenlos: die Dopplungen verbrauchen
    // keine eigene Position, sie teilen sich eine mit ihrem Partner.
    #expect(AdvancedKeyMap.noteKeycodes.count == 37)
    #expect(Set(AdvancedKeyMap.noteKeycodes.keys) == Set(0...36))
}

@Test func advKeinKeycodeIstDoppeltVergeben() {
    // Keycodes selbst sind immer eindeutig — nur die Halbtöne, auf die sie
    // zeigen, können sich bei den zwei bestätigten Dopplungen wiederholen.
    let alle = Array(AdvancedKeyMap.noteKeycodes.values)
        + [AdvancedKeyMap.octaveDownKeycode, AdvancedKeyMap.octaveUpKeycode]
    #expect(Set(alle).count == alle.count)
}

@Test func semitoneRangeReichtBisSechsunddreissig() {
    #expect(AdvancedKeyMap.semitoneRange == 0...36)
}

@Test func schiebebereichHaeltAlleNotenImGueltigenMidiBereich() {
    // 3 Oktaven statt 2 -> weniger Spielraum nach oben als im Grundmodus
    // (-4...5): bei semitoneRange bis 36 wäre +4 schon außerhalb 0...127.
    #expect(AdvancedKeyMap.shiftRange == -4...3)
    for shift in AdvancedKeyMap.shiftRange {
        for semitone in AdvancedKeyMap.semitoneRange {
            let note = Keyboard.baseNote + shift * 12 + semitone
            #expect(note >= 0 && note <= 127)
        }
    }
}

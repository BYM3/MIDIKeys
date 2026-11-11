import Testing
@testable import MIDIKeysCore

@Test func weisseTastenLiefernDurNotenleiter() {
    // a s d f g h j k l ö ä #  ->  C D E F G A H C D E F G
    let erwartet: [(UInt16, Int)] = [
        (0x00, 0), (0x01, 2), (0x02, 4), (0x03, 5),
        (0x05, 7), (0x04, 9), (0x26, 11), (0x28, 12),
        (0x25, 14), (0x29, 16), (0x27, 17), (0x2A, 19),
    ]
    for (keycode, semitone) in erwartet {
        #expect(KeyMap.action(for: keycode) == .note(semitone: semitone))
    }
}

@Test func schwarzeTastenLiegenZwischenDenWeissen() {
    // w e t z u o p +  ->  C# D# F# G# A# C# D# F#
    let erwartet: [(UInt16, Int)] = [
        (0x0D, 1), (0x0E, 3), (0x11, 6), (0x10, 8),
        (0x20, 10), (0x1F, 13), (0x23, 15), (0x1E, 18),
    ]
    for (keycode, semitone) in erwartet {
        #expect(KeyMap.action(for: keycode) == .note(semitone: semitone))
    }
}

@Test func steuertastenSindZugeordnet() {
    #expect(KeyMap.action(for: 0x06) == .octaveDown)  // QWERTZ y
    #expect(KeyMap.action(for: 0x07) == .octaveUp)    // QWERTZ x
    #expect(KeyMap.action(for: 0x30) == .sustain)     // Tab
}

@Test func nichtGemappteTastenLiefernNil() {
    #expect(KeyMap.action(for: 0x31) == nil)  // Leertaste
    #expect(KeyMap.action(for: 0x35) == nil)  // Escape
    #expect(KeyMap.action(for: 0x24) == nil)  // Return
}

@Test func esGibtGenauZwanzigNotentastenOhneLuecken() {
    #expect(KeyMap.noteKeycodes.count == 20)
    let halbtoene = KeyMap.noteKeycodes.compactMap { keycode -> Int? in
        guard case let .note(semitone) = KeyMap.action(for: keycode) else { return nil }
        return semitone
    }
    #expect(halbtoene == Array(0...19))
}

@Test func keinKeycodeIstDoppeltVergeben() {
    let alle = KeyMap.noteKeycodes + [0x06, 0x07, 0x30]
    #expect(Set(alle).count == alle.count)
}

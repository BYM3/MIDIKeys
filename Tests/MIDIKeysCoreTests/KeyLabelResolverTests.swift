import Testing
@testable import MIDIKeysCore

// Hinweis: Diese Tests rufen bewusst NICHT die echte Layout-Auflösung auf.
// `TISCopyCurrentKeyboardLayoutInputSource` braucht eine initialisierte
// GUI-Session und bricht in einem xctest-Prozess mit SIGABRT ab. Getestet
// wird die Auswahllogik; die echte Auflösung wird manuell verifiziert.

@Test func aufgeloestesZeichenGewinnt() {
    #expect(KeyLabelResolver.label(for: 0x00, using: { _ in "q" }) == "q")
}

@Test func leeresErgebnisFaelltAufDieTabelleZurueck() {
    #expect(KeyLabelResolver.label(for: 0x00, using: { _ in "" }) == "a")
}

@Test func fehlgeschlageneAufloesungFaelltAufDieTabelleZurueck() {
    #expect(KeyLabelResolver.label(for: 0x29, using: { _ in nil }) == "ö")
}

@Test func unbekannteKeycodesLiefernEinenPlatzhalter() {
    // Keycode 0xFF existiert nicht und hat keinen Fallback.
    #expect(KeyLabelResolver.label(for: 0xFF, using: { _ in nil }) == "?")
}

@Test func fuerJedeGemappteTasteExistiertEinFallback() {
    let alle = KeyMap.noteKeycodes + [
        KeyMap.octaveDownKeycode, KeyMap.octaveUpKeycode, KeyMap.sustainKeycode,
    ]
    for keycode in alle {
        #expect(KeyLabelResolver.fallbackLabels[keycode] != nil)
    }
}

@Test func fallbackEntsprichtDemDeutschenLayout() {
    #expect(KeyLabelResolver.fallbackLabels[0x29] == "ö")
    #expect(KeyLabelResolver.fallbackLabels[0x27] == "ä")
    #expect(KeyLabelResolver.fallbackLabels[0x2A] == "#")
    #expect(KeyLabelResolver.fallbackLabels[0x1E] == "+")
    #expect(KeyLabelResolver.fallbackLabels[0x10] == "z")
    #expect(KeyLabelResolver.fallbackLabels[0x06] == "y")
}

@Test func fallbacksSindKurzUndEinzeilig() {
    // Sonst zerreißt es das Cheatsheet-Layout.
    for (_, label) in KeyLabelResolver.fallbackLabels {
        #expect(label.count <= 2)
        #expect(label.contains("\n") == false)
    }
}

@Test func advOktaventastenHabenEinenFallback() {
    #expect(KeyLabelResolver.label(for: AdvancedKeyMap.octaveDownKeycode, using: { _ in nil }) == "^")
    #expect(KeyLabelResolver.label(for: AdvancedKeyMap.octaveUpKeycode, using: { _ in nil }) == "1")
}

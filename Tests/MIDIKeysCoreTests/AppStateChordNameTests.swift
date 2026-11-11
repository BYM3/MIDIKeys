import Foundation
import Testing
@testable import MIDIKeysCore

@MainActor
private func frischerStart() -> AppState {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    // Fest auf Deutsch, sonst hinge das Ergebnis an der Systemsprache.
    defaults.set(Sprache.deutsch.rawValue, forKey: "sprache")
    return AppState(defaults: defaults)
}

@Test @MainActor func ohneGehalteneTastenGibtEsKeinenAkkordnamen() {
    let state = frischerStart()
    #expect(state.chordName == nil)
}

@Test @MainActor func erkenntCDurAusDenGehaltenenTasten() {
    let state = frischerStart()
    // a=C(0) d=E(4) g=G(7) -> C-E-G, C-Dur.
    state.pressedKeycodes = [0x00, 0x02, 0x05]
    #expect(state.chordName == "C maj")
}

@Test @MainActor func erkenntDenAkkordAuchUmOktavenVerschoben() {
    // Der Akkord bleibt derselbe, nur in einer anderen Lage — die
    // Tonhöhenklassen ändern sich durch den Oktav-Shift nicht.
    var state = frischerStart()
    state.shiftOctave(2)
    state.pressedKeycodes = [0x00, 0x02, 0x05]
    #expect(state.chordName == "C maj")
}

@Test @MainActor func nichtGemappteOderSteuertastenZaehlenNicht() {
    let state = frischerStart()
    // Tab (Sustain) und y (Oktave) sind keine Noten und dürfen die
    // Akkorderkennung nicht verfälschen.
    state.pressedKeycodes = [0x00, 0x02, 0x05, KeyMap.sustainKeycode, KeyMap.octaveDownKeycode]
    #expect(state.chordName == "C maj")
}

@Test @MainActor func eineEinzelneGehalteneTasteErgibtKeinenAkkordnamen() {
    let state = frischerStart()
    state.pressedKeycodes = [0x00]
    #expect(state.chordName == nil)
}

@Test @MainActor func zweiTastenImQuintabstandZeigenDieQuinte() {
    let state = frischerStart()
    // a=C(0) g=G(7) -> Intervall Quinte.
    state.pressedKeycodes = [0x00, 0x05]
    #expect(state.chordName == "C–G · Quinte")
}

@Test @MainActor func erkenntDieUmkehrungAusDerTatsaechlichGehaltenenTonhoehe() {
    let state = frischerStart()
    // d=E(Halbton 4) g=G(Halbton 7) k=C(Halbton 12, eine Oktave höher) ->
    // tatsächlich tiefster gehaltener Ton ist E: erste Umkehrung von C-Dur.
    state.pressedKeycodes = [0x02, 0x05, 0x28]
    #expect(state.chordName == "C maj/E · 1. Umk.")
}

@Test @MainActor func rootPositionZeigtWeiterhinKeinenSlash() {
    let state = frischerStart()
    // a=C(0) d=E(4) g=G(7) -> C ist der tiefste Ton, Grundstellung.
    state.pressedKeycodes = [0x00, 0x02, 0x05]
    #expect(state.chordName == "C maj")
}

// MARK: - Adv.-Layout

@Test @MainActor func advLayoutStartetAusOhneGespeichertenWert() {
    let state = frischerStart()
    #expect(state.advancedLayoutEnabled == false)
}

@Test @MainActor func advLayoutUeberlebtDenNeustart() {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)

    let ersterStart = AppState(defaults: defaults)
    ersterStart.advancedLayoutEnabled = true

    let zweiterStart = AppState(defaults: defaults)
    #expect(zweiterStart.advancedLayoutEnabled == true)
}

@Test @MainActor func oktavSchiebungRespektiertDenEngerenAdvBereich() {
    let state = frischerStart()
    state.advancedLayoutEnabled = true
    // Grundlayout würde +5 erlauben, Adv.-Layout nur bis +3.
    for _ in 0..<5 { state.shiftOctave(1) }
    #expect(state.keyboard.octaveShift == 3)
}

@Test @MainActor func oktavSchiebungImGrundlayoutBleibtUnveraendert() {
    let state = frischerStart()
    #expect(state.advancedLayoutEnabled == false)
    for _ in 0..<5 { state.shiftOctave(1) }
    #expect(state.keyboard.octaveShift == 5)
}

@Test @MainActor func chordNameFunktioniertAuchImAdvLayout() {
    let state = frischerStart()
    state.advancedLayoutEnabled = true
    // <=C(0) x=E(4) v=G(7) im Adv.-Layout -> C-Dur.
    state.pressedKeycodes = [0x32, 0x07, 0x09]
    #expect(state.chordName == "C maj")
}

@Test @MainActor func akkordnameFolgtDerEingestelltenSprache() {
    let state = frischerStart()
    state.sprache = .english
    // Wie oben: d=E g=G k=C' -> erste Umkehrung von C-Dur.
    state.pressedKeycodes = [0x02, 0x05, 0x28]
    #expect(state.chordName == "C maj/E · 1st inv.")
    // a=C g=G -> Quinte
    state.pressedKeycodes = [0x00, 0x05]
    #expect(state.chordName == "C–G · perfect fifth")
}

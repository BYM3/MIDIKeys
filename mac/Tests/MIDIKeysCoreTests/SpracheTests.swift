import Foundation
import Testing
@testable import MIDIKeysCore

private func frischeDefaults() -> UserDefaults {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    return defaults
}

@Test func spracheFolgtOhneGespeichertenWertDemSystem() {
    #expect(Sprache.ausSystem(["de-DE", "en-US"]) == .deutsch)
    #expect(Sprache.ausSystem(["de-AT"]) == .deutsch)
    #expect(Sprache.ausSystem(["en-GB", "de-DE"]) == .english)
    // Alles, was nicht Deutsch ist, bekommt Englisch.
    #expect(Sprache.ausSystem(["fr-FR"]) == .english)
    #expect(Sprache.ausSystem([]) == .english)
}

@Test @MainActor func spracheUeberlebtDenNeustart() {
    let defaults = frischeDefaults()
    let ersterStart = AppState(defaults: defaults)
    ersterStart.sprache = .english
    #expect(AppState(defaults: defaults).sprache == .english)
    ersterStart.sprache = .deutsch
    #expect(AppState(defaults: defaults).sprache == .deutsch)
}

@Test func textWaehltDieSprache() {
    #expect(Sprache.deutsch.text("Beenden", "Quit") == "Beenden")
    #expect(Sprache.english.text("Beenden", "Quit") == "Quit")
}

@Test func englischeNotennamenNutzenB() {
    #expect(noteName(forMIDI: 59, sprache: .english) == "B3")
    #expect(noteName(forMIDI: 59, sprache: .deutsch) == "H3")
    #expect(noteName(forMIDI: 49, sprache: .english) == "C#3")
}

@Test func englischerTonumfang() {
    var keyboard = Keyboard()
    _ = keyboard.shift(by: -1)
    // C2 + 23 Halbtöne = H3 bzw. B3
    #expect(keyboard.rangeDescription(maxSemitone: 23, sprache: .english) == "C2–B3")
    #expect(keyboard.rangeDescription(maxSemitone: 23, sprache: .deutsch) == "C2–H3")
}

@Test func englischeAkkordnamen() {
    #expect(ChordNamer.name(forPitchClasses: [11, 3, 6], sprache: .english) == "B maj")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7], bass: 4, sprache: .english) == "C maj/E · 1st inv.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7], bass: 7, sprache: .english) == "C maj/G · 2nd inv.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 11], bass: 11, sprache: .english) == "C maj7/B · 3rd inv.")
}

@Test func englischeIntervallnamen() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4], sprache: .english) == "C–E · major third")
    #expect(ChordNamer.name(forPitchClasses: [0, 6], sprache: .english) == "C–F# · tritone")
    #expect(ChordNamer.name(forPitchClasses: [0, 7], sprache: .english) == "C–G · perfect fifth")
    #expect(ChordNamer.name(forPitchClasses: [11, 2], bass: 11, sprache: .english) == "B–D · minor third")
}

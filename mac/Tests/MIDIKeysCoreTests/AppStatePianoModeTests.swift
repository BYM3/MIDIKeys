import Foundation
import Testing
@testable import MIDIKeysCore

private func frischeDefaults() -> UserDefaults {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    return defaults
}

@Test @MainActor func klavierTonStartetAusOhneGespeichertenWert() {
    let state = AppState(defaults: frischeDefaults())
    #expect(state.pianoModeEnabled == false)
}

@Test @MainActor func klavierTonUeberlebtDenNeustart() {
    // Anders als isActive: bewusst gemerkt, wie Oktavlage und Shortcut.
    let defaults = frischeDefaults()
    let ersterStart = AppState(defaults: defaults)
    ersterStart.pianoModeEnabled = true

    let zweiterStart = AppState(defaults: defaults)
    #expect(zweiterStart.pianoModeEnabled == true)
}

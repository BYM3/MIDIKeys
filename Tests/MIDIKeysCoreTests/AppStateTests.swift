import Foundation
import Testing
@testable import MIDIKeysCore

private func frischeDefaults() -> UserDefaults {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    return defaults
}

@Test @MainActor func startetImmerInaktiv() {
    // Nie den Aktiv-Zustand persistieren: Ein Neustart darf nie in eine
    // tote Tastatur führen.
    let defaults = frischeDefaults()
    let ersterStart = AppState(defaults: defaults)
    ersterStart.isActive = true

    let zweiterStart = AppState(defaults: defaults)
    #expect(zweiterStart.isActive == false)
}

@Test @MainActor func oktavlageUeberlebtDenNeustart() {
    let defaults = frischeDefaults()
    let ersterStart = AppState(defaults: defaults)
    ersterStart.shiftOctave(2)
    #expect(ersterStart.keyboard.octaveShift == 2)

    let zweiterStart = AppState(defaults: defaults)
    #expect(zweiterStart.keyboard.octaveShift == 2)
}

@Test @MainActor func shortcutUeberlebtDenNeustart() {
    let defaults = frischeDefaults()
    let neuer = Hotkey(keycode: 0x31, modifiers: CGEventFlagsRaw.command | CGEventFlagsRaw.shift)
    let ersterStart = AppState(defaults: defaults)
    ersterStart.hotkey = neuer

    let zweiterStart = AppState(defaults: defaults)
    #expect(zweiterStart.hotkey == neuer)
}

@Test @MainActor func ohneGespeichertenShortcutGiltDerStandard() {
    let state = AppState(defaults: frischeDefaults())
    #expect(state.hotkey == Hotkey.default)
}

@Test @MainActor func oktavShiftWirdGeklemmt() {
    let state = AppState(defaults: frischeDefaults())
    state.shiftOctave(99)
    #expect(state.keyboard.octaveShift == 0)  // abgelehnt, nicht geklemmt-verschoben
    state.shiftOctave(5)
    #expect(state.keyboard.octaveShift == 5)
}

@Test @MainActor func beschaedigteGespeicherteDatenFuehrenZumStandard() {
    let defaults = frischeDefaults()
    defaults.set(Data([0xDE, 0xAD]), forKey: "hotkey")
    let state = AppState(defaults: defaults)
    #expect(state.hotkey == Hotkey.default)
}

@Test @MainActor func unveraenderteFreigabeLoestKeineNeuzeichnungAus() {
    // Der Freigabe-Timer fragt alle 1,5 s ab. Jede Zuweisung — auch mit
    // gleichem Wert — würde beobachtende SwiftUI-Views neu rechnen lassen.
    let state = AppState(defaults: frischeDefaults())
    let geaendert = Merker()
    withObservationTracking {
        _ = state.hasAccessibility
    } onChange: {
        geaendert.wert = true
    }
    state.refreshAccessibility()
    #expect(!geaendert.wert)
}

/// `onChange` ist `@Sendable` — eine lokale `var` darf es nicht verändern.
private final class Merker: @unchecked Sendable {
    var wert = false
}

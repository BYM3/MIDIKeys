import Foundation
import Testing
@testable import MIDIKeysCore

private final class SpyTransport: MIDISending {
    var sent: [MIDIMessage] = []
    func send(_ message: MIDIMessage) { sent.append(message) }
}

@MainActor
private func aufbau() -> (InputController, AppState, SpyTransport) {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    let state = AppState(defaults: defaults)
    let spy = SpyTransport()
    let controller = InputController(state: state, engine: MIDIEngine(transport: spy))
    return (controller, state, spy)
}

@Test @MainActor func spielenErzeugtNotenUndSchlucktDieTaste() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    let geschluckt = controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(geschluckt == true)
    #expect(spy.sent == [.noteOn(note: 48, velocity: 100)])
}

@Test @MainActor func imInaktivenModusPassiertNichts() {
    let (controller, state, spy) = aufbau()
    state.isActive = false
    let geschluckt = controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(geschluckt == false)
    #expect(spy.sent.isEmpty)
}

@Test @MainActor func cmdKombinationWirdNichtGeschluckt() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    let geschluckt = controller.handle(
        keycode: 0x35, flags: CGEventFlagsRaw.command | CGEventFlagsRaw.option,
        isKeyDown: true, isRepeat: false
    )
    #expect(geschluckt == false)
    #expect(spy.sent.isEmpty)
}

@Test @MainActor func oktavwechselWaehrendEinTonKlingtLaesstIhnNichtHaengen() {
    // Der kritische Fall aus der Spec: 'a' halten, Oktave hoch, 'a' loslassen.
    // Das Note-Off muss an Note 48 gehen, nicht an 60.
    let (controller, state, spy) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: 0x07, flags: 0, isKeyDown: true, isRepeat: false)  // x
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: false, isRepeat: false)

    #expect(state.keyboard.octaveShift == 1)
    #expect(spy.sent == [.noteOn(note: 48, velocity: 100), .noteOff(note: 48)])
}

@Test @MainActor func nachOktavwechselSpielenNeueTastenDieNeueLage() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x07, flags: 0, isKeyDown: true, isRepeat: false)  // x
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)  // a
    #expect(spy.sent == [.noteOn(note: 60, velocity: 100)])
}

@Test @MainActor func tabSchaltetSustainUeberDenController() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x30, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: 0x30, flags: 0, isKeyDown: false, isRepeat: false)
    #expect(spy.sent == [
        .controlChange(controller: 64, value: 127),
        .controlChange(controller: 64, value: 0),
    ])
}

@Test @MainActor func derHotkeySchaltetUm() {
    let (controller, state, _) = aufbau()
    controller.handle(
        keycode: Hotkey.default.keycode, flags: Hotkey.default.modifiers,
        isKeyDown: true, isRepeat: false
    )
    #expect(state.isActive == true)
    controller.handle(
        keycode: Hotkey.default.keycode, flags: Hotkey.default.modifiers,
        isKeyDown: true, isRepeat: false
    )
    #expect(state.isActive == false)
}

@Test @MainActor func deaktivierenBeendetAlleKlingendenToene() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: 0x02, flags: 0, isKeyDown: true, isRepeat: false)
    spy.sent.removeAll()

    controller.setActive(false)

    #expect(spy.sent.contains(.noteOff(note: 48)))
    #expect(spy.sent.contains(.noteOff(note: 52)))
    #expect(spy.sent.last == .controlChange(controller: 123, value: 0))
    #expect(state.isActive == false)
}

@Test @MainActor func advLayoutWirdVomControllerRespektiert() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    state.advancedLayoutEnabled = true
    // <=C3(0) im Adv.-Layout. Im Grundlayout ist diese Taste gar nicht gemappt.
    controller.handle(keycode: 0x32, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(spy.sent == [.noteOn(note: 48, velocity: 100)])
}

@Test @MainActor func advOktaveRespektiertDenEngerenSchiebebereichEndeZuEnde() {
    let (controller, state, _) = aufbau()
    state.isActive = true
    state.advancedLayoutEnabled = true
    for _ in 0..<5 {
        controller.handle(
            keycode: AdvancedKeyMap.octaveUpKeycode, flags: 0, isKeyDown: true, isRepeat: false
        )
    }
    #expect(state.keyboard.octaveShift == 3)
}

// MARK: - Doppel-Esc als Notausstieg

private let esc: UInt16 = 0x35

@MainActor
private func aufbauMitUhr() -> (InputController, AppState, SpyTransport, (Double) -> Void) {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    let state = AppState(defaults: defaults)
    let spy = SpyTransport()
    var jetzt = 100.0
    let controller = InputController(
        state: state, engine: MIDIEngine(transport: spy), uhr: { jetzt }
    )
    return (controller, state, spy, { jetzt = $0 })
}

@MainActor
private func druecke(_ controller: InputController, _ keycode: UInt16, flags: UInt64 = 0) {
    controller.handle(keycode: keycode, flags: flags, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: keycode, flags: flags, isKeyDown: false, isRepeat: false)
}

@Test @MainActor func doppelEscSchaltetDenModusAus() {
    let (controller, state, _, setzeZeit) = aufbauMitUhr()
    state.isActive = true
    druecke(controller, esc)
    #expect(state.isActive)  // ein einzelnes Esc tut nichts
    setzeZeit(100.4)
    druecke(controller, esc)
    #expect(!state.isActive)
}

@Test @MainActor func doppelEscStopptKlingendeToene() {
    let (controller, state, spy, setzeZeit) = aufbauMitUhr()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)  // a halten
    druecke(controller, esc)
    setzeZeit(100.2)
    druecke(controller, esc)
    #expect(spy.sent.contains(.noteOff(note: 48)))
}

@Test @MainActor func zuLangsamesDoppelEscZaehltNicht() {
    let (controller, state, _, setzeZeit) = aufbauMitUhr()
    state.isActive = true
    druecke(controller, esc)
    setzeZeit(100.6)
    druecke(controller, esc)
    #expect(state.isActive)
    // Das zweite Esc startet aber ein neues Fenster.
    setzeZeit(100.9)
    druecke(controller, esc)
    #expect(!state.isActive)
}

@Test @MainActor func andereTasteDazwischenBrichtDoppelEscAb() {
    let (controller, state, _, setzeZeit) = aufbauMitUhr()
    state.isActive = true
    druecke(controller, esc)
    setzeZeit(100.1)
    druecke(controller, 0x00)  // a
    setzeZeit(100.2)
    druecke(controller, esc)
    #expect(state.isActive)
}

@Test @MainActor func doppelEscSchaltetNichtEin() {
    // Esc wird in anderen Programmen ständig gedrückt — das darf die
    // Tastatur nie unbemerkt übernehmen.
    let (controller, state, _, setzeZeit) = aufbauMitUhr()
    state.isActive = false
    druecke(controller, esc)
    setzeZeit(100.2)
    let geschluckt = controller.handle(keycode: esc, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(!state.isActive)
    #expect(!geschluckt)
}

@Test @MainActor func escMitModifierZaehltNichtAlsDoppelEsc() {
    let (controller, state, _, setzeZeit) = aufbauMitUhr()
    state.isActive = true
    druecke(controller, esc, flags: CGEventFlagsRaw.shift)
    setzeZeit(100.2)
    druecke(controller, esc, flags: CGEventFlagsRaw.shift)
    #expect(state.isActive)
}

@Test @MainActor func cUndRGehenNurMitHakenDurch() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    let c: UInt16 = 0x08, r: UInt16 = 0x0F

    // Ohne Haken: beide gesperrt.
    for taste in [c, r] {
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: false) == true)
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: false, isRepeat: false) == true)
    }

    // Mit Haken: beide normale Tasten, auch gehalten.
    state.cUndRFrei = true
    for taste in [c, r] {
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: false) == false)
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: true) == false)
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: false, isRepeat: false) == false)
    }
    #expect(spy.sent.isEmpty)
    #expect(state.pressedKeycodes.isEmpty)

    // Klavier-Modus: trotz Haken gesperrt.
    state.pianoModeEnabled = true
    for taste in [c, r] {
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: false) == true)
    }
    #expect(spy.sent.isEmpty)
    state.pianoModeEnabled = false

    // Adv.-Layout: trotz Haken Noten — c ist F3, r ist H4.
    state.advancedLayoutEnabled = true
    #expect(controller.handle(keycode: c, flags: 0, isKeyDown: true, isRepeat: false) == true)
    #expect(controller.handle(keycode: r, flags: 0, isKeyDown: true, isRepeat: false) == true)
    #expect(spy.sent == [.noteOn(note: 53, velocity: 100), .noteOn(note: 71, velocity: 100)])
}

@Test @MainActor func cUndRFreigabeWirdGemerkt() {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    #expect(AppState(defaults: defaults).cUndRFrei == false)
    AppState(defaults: defaults).cUndRFrei = true
    #expect(AppState(defaults: defaults).cUndRFrei == true)
}

@Test @MainActor func enterUndLoeschenGehenNurMitMidiAusgangDurch() {
    let (controller, state, spy) = aufbau()
    state.isActive = true
    let tasten: [UInt16] = [0x24, 0x4C, 0x33, 0x75]  // Return, Enter, ⌫, ⌦

    for adv in [false, true] {
        state.advancedLayoutEnabled = adv
        for taste in tasten {
            #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: false) == false)
            // Gehaltenes ⌫ soll weiterlöschen.
            #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: true) == false)
            #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: false, isRepeat: false) == false)
        }
    }

    // Klavier-Modus: geschluckt.
    state.pianoModeEnabled = true
    for taste in tasten {
        #expect(controller.handle(keycode: taste, flags: 0, isKeyDown: true, isRepeat: false) == true)
    }
    #expect(spy.sent.isEmpty)
}

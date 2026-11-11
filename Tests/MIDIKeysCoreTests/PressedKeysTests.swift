import Foundation
import Testing
@testable import MIDIKeysCore

private final class StummerTransport: MIDISending {
    func send(_ message: MIDIMessage) {}
}

@MainActor
private func aufbau() -> (InputController, AppState) {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    let state = AppState(defaults: defaults)
    let controller = InputController(state: state, engine: MIDIEngine(transport: StummerTransport()))
    return (controller, state)
}

@Test @MainActor func gedrueckteNotentasteWirdVermerktUndBeimLoslassenEntfernt() {
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(state.pressedKeycodes == [0x00])
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: false, isRepeat: false)
    #expect(state.pressedKeycodes.isEmpty)
}

@Test @MainActor func mehrereTastenGleichzeitig() {
    let (controller, state) = aufbau()
    state.isActive = true
    for keycode: UInt16 in [0x00, 0x02, 0x05] {
        controller.handle(keycode: keycode, flags: 0, isKeyDown: true, isRepeat: false)
    }
    #expect(state.pressedKeycodes == [0x00, 0x02, 0x05])
    controller.handle(keycode: 0x02, flags: 0, isKeyDown: false, isRepeat: false)
    #expect(state.pressedKeycodes == [0x00, 0x05])
}

@Test @MainActor func sustainUndOktavtastenLeuchtenEbenfalls() {
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: KeyMap.sustainKeycode, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: KeyMap.octaveUpKeycode, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(state.pressedKeycodes.contains(KeyMap.sustainKeycode))
    #expect(state.pressedKeycodes.contains(KeyMap.octaveUpKeycode))

    controller.handle(keycode: KeyMap.octaveUpKeycode, flags: 0, isKeyDown: false, isRepeat: false)
    #expect(state.pressedKeycodes.contains(KeyMap.octaveUpKeycode) == false)
}

@Test @MainActor func imInaktivenModusWirdNichtsVermerkt() {
    let (controller, state) = aufbau()
    state.isActive = false
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(state.pressedKeycodes.isEmpty)
}

@Test @MainActor func cmdKombinationenLeuchtenNicht() {
    // Cmd+A geht durch und spielt nichts — darf also auch nicht leuchten.
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: CGEventFlagsRaw.command, isKeyDown: true, isRepeat: false)
    #expect(state.pressedKeycodes.isEmpty)
}

@Test @MainActor func nichtGemappteTastenLeuchtenNicht() {
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x31, flags: 0, isKeyDown: true, isRepeat: false)  // Leertaste
    #expect(state.pressedKeycodes.isEmpty)
}

@Test @MainActor func keyRepeatErzeugtKeineDubletten() {
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: true)
    #expect(state.pressedKeycodes == [0x00])
}

@Test @MainActor func deaktivierenLoeschtAlleLeuchtendenTasten() {
    // Sonst bliebe eine Taste für immer erleuchtet: Nach dem Umschalten
    // kommt für sie kein keyUp mehr an.
    let (controller, state) = aufbau()
    state.isActive = true
    controller.handle(keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false)
    controller.handle(keycode: 0x02, flags: 0, isKeyDown: true, isRepeat: false)
    #expect(state.pressedKeycodes.count == 2)

    controller.setActive(false)
    #expect(state.pressedKeycodes.isEmpty)
}

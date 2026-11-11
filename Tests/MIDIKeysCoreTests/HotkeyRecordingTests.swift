import AppKit
import Testing
@testable import MIDIKeysCore

@Test func nsEventUndCgEventTeilenDieselbenModifierBits() {
    // Falls Apple das je ändert, schlägt dieser Test fehl, statt dass der
    // Recorder still falsche Shortcuts aufzeichnet.
    #expect(UInt64(NSEvent.ModifierFlags.shift.rawValue) == CGEventFlagsRaw.shift)
    #expect(UInt64(NSEvent.ModifierFlags.control.rawValue) == CGEventFlagsRaw.control)
    #expect(UInt64(NSEvent.ModifierFlags.option.rawValue) == CGEventFlagsRaw.option)
    #expect(UInt64(NSEvent.ModifierFlags.command.rawValue) == CGEventFlagsRaw.command)
}

@Test func gueltigeKombinationWirdAufgezeichnet() {
    let hotkey = HotkeyRecording.hotkey(
        fromKeycode: 0x2E, modifierFlagsRaw: CGEventFlagsRaw.control | CGEventFlagsRaw.option
    )
    #expect(hotkey == Hotkey(keycode: 0x2E, modifiers: CGEventFlagsRaw.control | CGEventFlagsRaw.option))
}

@Test func kombinationOhneModifierWirdAbgelehnt() {
    #expect(HotkeyRecording.hotkey(fromKeycode: 0x2E, modifierFlagsRaw: 0) == nil)
    #expect(HotkeyRecording.hotkey(fromKeycode: 0x2E, modifierFlagsRaw: CGEventFlagsRaw.shift) == nil)
}

@Test func fremdeFlagsWerdenAbgeschnitten() {
    let capsLock: UInt64 = 0x10000
    let hotkey = HotkeyRecording.hotkey(
        fromKeycode: 0x2E, modifierFlagsRaw: CGEventFlagsRaw.command | capsLock
    )
    #expect(hotkey?.modifiers == CGEventFlagsRaw.command)
}

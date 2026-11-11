import Foundation
import Testing
@testable import MIDIKeysCore

private let ctrl = CGEventFlagsRaw.control
private let opt = CGEventFlagsRaw.option
private let shift = CGEventFlagsRaw.shift
private let cmd = CGEventFlagsRaw.command

@Test func standardShortcutIstOptMinus() {
    #expect(Hotkey.default.keycode == 0x2C)  // kVK_ANSI_Slash = - auf deutscher Tastatur
    #expect(Hotkey.default.modifiers == opt)
}

@Test func trifftNurBeiExaktGleichenModifiern() {
    let hotkey = Hotkey(keycode: 0x2E, modifiers: ctrl | opt)
    #expect(hotkey.matches(keycode: 0x2E, flags: ctrl | opt) == true)
    #expect(hotkey.matches(keycode: 0x2E, flags: ctrl) == false)
    #expect(hotkey.matches(keycode: 0x2E, flags: ctrl | opt | shift) == false)
    #expect(hotkey.matches(keycode: 0x00, flags: ctrl | opt) == false)
}

@Test func irrelevanteFlagsWerdenIgnoriert() {
    // CapsLock, NumPad und der Function-Key dürfen ein Match nicht verhindern.
    let hotkey = Hotkey(keycode: 0x2E, modifiers: ctrl | opt)
    let capsLock: UInt64 = 0x10000
    let numericPad: UInt64 = 0x200000
    #expect(hotkey.matches(keycode: 0x2E, flags: ctrl | opt | capsLock | numericPad) == true)
}

@Test func anzeigenameFolgtApplesReihenfolge() {
    // Apple-Konvention: Control, Option, Shift, Command.
    let hotkey = Hotkey(keycode: 0x2E, modifiers: cmd | shift | opt | ctrl)
    #expect(hotkey.modifierSymbols == "⌃⌥⇧⌘")
    #expect(hotkey.displayName(keyLabel: "M") == "⌃⌥⇧⌘M")
}

@Test func einShortcutBrauchtMindestensEinenModifier() {
    #expect(Hotkey.isValid(modifiers: 0) == false)
    #expect(Hotkey.isValid(modifiers: shift) == false)  // Shift allein reicht nicht
    #expect(Hotkey.isValid(modifiers: ctrl) == true)
    #expect(Hotkey.isValid(modifiers: cmd) == true)
    #expect(Hotkey.isValid(modifiers: opt | shift) == true)
}

@Test func ueberstehtDenRundlaufDurchDieKodierung() throws {
    let original = Hotkey(keycode: 0x31, modifiers: cmd | shift)
    let data = try JSONEncoder().encode(original)
    let zurueck = try JSONDecoder().decode(Hotkey.self, from: data)
    #expect(zurueck == original)
}

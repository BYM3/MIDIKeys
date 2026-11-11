import CoreGraphics

/// Die Rohwerte der Modifier-Flags, damit `MIDIKeysCore` ohne AppKit auskommt
/// und die Tests lesbar bleiben.
public enum CGEventFlagsRaw {
    public static let shift = CGEventFlags.maskShift.rawValue
    public static let control = CGEventFlags.maskControl.rawValue
    public static let option = CGEventFlags.maskAlternate.rawValue
    public static let command = CGEventFlags.maskCommand.rawValue
}

/// Eine Tastenkombination zum Umschalten des MIDI-Modus.
public struct Hotkey: Equatable, Codable, Sendable {
    public var keycode: UInt16
    public var modifiers: UInt64

    public init(keycode: UInt16, modifiers: UInt64) {
        self.keycode = keycode
        self.modifiers = modifiers & Hotkey.relevantMask
    }

    /// Nur diese vier Flags zählen. CapsLock, NumPad-Kennung und der
    /// Function-Key stecken ebenfalls in `CGEventFlags` und würden ein
    /// Match sonst zufällig verhindern.
    public static let relevantMask: UInt64 =
        CGEventFlagsRaw.shift | CGEventFlagsRaw.control
        | CGEventFlagsRaw.option | CGEventFlagsRaw.command

    /// ⌥- — physische Taste rechts neben dem Punkt (deutsch: `-`).
    public static let `default` = Hotkey(
        keycode: 0x2C,  // kVK_ANSI_Slash
        modifiers: CGEventFlagsRaw.option
    )

    public func matches(keycode: UInt16, flags: UInt64) -> Bool {
        self.keycode == keycode && (flags & Hotkey.relevantMask) == modifiers
    }

    /// Ein Shortcut braucht mindestens einen Modifier außer Shift — sonst
    /// würde er im inaktiven Modus normales Tippen abfangen.
    public static func isValid(modifiers: UInt64) -> Bool {
        let ohneShift = modifiers & (CGEventFlagsRaw.control
            | CGEventFlagsRaw.option | CGEventFlagsRaw.command)
        return ohneShift != 0
    }

    /// Modifier-Symbole in Apples Reihenfolge: Control, Option, Shift, Command.
    public var modifierSymbols: String {
        var result = ""
        if modifiers & CGEventFlagsRaw.control != 0 { result += "⌃" }
        if modifiers & CGEventFlagsRaw.option != 0 { result += "⌥" }
        if modifiers & CGEventFlagsRaw.shift != 0 { result += "⇧" }
        if modifiers & CGEventFlagsRaw.command != 0 { result += "⌘" }
        return result
    }

    public func displayName(keyLabel: String) -> String {
        modifierSymbols + keyLabel
    }
}

/// Wandelt ein aufgezeichnetes Tastenereignis in einen Hotkey.
public enum HotkeyRecording {
    /// Gibt `nil` zurück, wenn die Kombination als Shortcut untauglich ist.
    public static func hotkey(fromKeycode keycode: UInt16, modifierFlagsRaw raw: UInt64) -> Hotkey? {
        let modifiers = raw & Hotkey.relevantMask
        guard Hotkey.isValid(modifiers: modifiers) else { return nil }
        return Hotkey(keycode: keycode, modifiers: modifiers)
    }
}

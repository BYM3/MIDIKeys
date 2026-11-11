import Carbon.HIToolbox
import Foundation

/// Löst physische Keycodes in die Zeichen des aktiven Tastaturlayouts auf.
///
/// Die Klaviatur selbst ist bewusst layoutunabhängig über Keycodes definiert;
/// nur die Beschriftung im Cheatsheet folgt dem Layout. Wechselt der Nutzer
/// auf ein US-Layout, zeigt das Cheatsheet automatisch die dann korrekten
/// Zeichen, ohne dass sich die Klaviatur verschiebt.
public enum KeyLabelResolver {
    /// Beschriftungen für ein deutsches Layout — greift, wenn die Auflösung
    /// über das aktive Layout fehlschlägt.
    public static let fallbackLabels: [UInt16: String] = [
        0x00: "a", 0x01: "s", 0x02: "d", 0x03: "f", 0x05: "g", 0x04: "h",
        0x26: "j", 0x28: "k", 0x25: "l", 0x29: "ö", 0x27: "ä", 0x2A: "#",
        0x0D: "w", 0x0E: "e", 0x11: "t", 0x10: "z", 0x20: "u", 0x1F: "o",
        0x23: "p", 0x1E: "+",
        0x06: "y", 0x07: "x", 0x30: "⇥",
        0x0A: "^", 0x12: "1",  // Adv.-Oktavtasten
    ]

    public static func label(for keycode: UInt16) -> String {
        label(for: keycode, using: translate)
    }

    /// Die Auswahllogik mit injizierbarer Auflösung.
    ///
    /// Existiert, weil `TISCopyCurrentKeyboardLayoutInputSource` eine
    /// initialisierte GUI-Session braucht und in einem xctest-Prozess intern
    /// mit SIGABRT abbricht. So bleibt die Fallback-Entscheidung testbar;
    /// die echte Layout-Auflösung wird manuell verifiziert (Task 16).
    public static func label(
        for keycode: UInt16,
        using aufloesung: (UInt16) -> String?
    ) -> String {
        if let aufgeloest = aufloesung(keycode), aufgeloest.isEmpty == false {
            return aufgeloest
        }
        return fallbackLabels[keycode] ?? "?"
    }

    private static func translate(_ keycode: UInt16) -> String? {
        guard let source = TISCopyCurrentKeyboardLayoutInputSource()?.takeRetainedValue(),
              let pointer = TISGetInputSourceProperty(source, kTISPropertyUnicodeKeyLayoutData)
        else { return nil }
        let data = Unmanaged<CFData>.fromOpaque(pointer).takeUnretainedValue() as Data

        return data.withUnsafeBytes { raw -> String? in
            guard let layout = raw.bindMemory(to: UCKeyboardLayout.self).baseAddress
            else { return nil }
            var deadKeyState: UInt32 = 0
            var length = 0
            // Die Kapazität muss als Konstante übergeben werden: `chars.count`
            // in derselben Argumentliste wie `&chars` verletzt Swifts
            // Exclusivity-Regel und bricht zur Laufzeit mit SIGABRT ab.
            let kapazitaet = 4
            var chars = [UniChar](repeating: 0, count: kapazitaet)
            let status = UCKeyTranslate(
                layout,
                keycode,
                UInt16(kUCKeyActionDisplay),
                0,  // keine Modifier
                UInt32(LMGetKbdType()),
                OptionBits(kUCKeyTranslateNoDeadKeysBit),
                &deadKeyState,
                kapazitaet,
                &length,
                &chars
            )
            guard status == noErr, length > 0 else { return nil }
            return String(utf16CodeUnits: chars, count: length)
        }
    }
}

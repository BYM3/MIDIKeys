import ApplicationServices
import CoreGraphics
import Foundation

/// Fängt Tastenereignisse systemweit ab.
///
/// Läuft in beiden Modi: Im inaktiven Modus reicht er alles durch und horcht
/// nur auf den Toggle-Shortcut. Ein Mechanismus statt zusätzlich
/// `RegisterEventHotKey` — dadurch verhält sich der Shortcut in beide
/// Richtungen identisch.
@MainActor
public final class KeyboardTap {
    private let controller: InputController
    private var tap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?

    public init(controller: InputController) {
        self.controller = controller
    }

    public var isRunning: Bool { tap != nil }

    /// Ob die Bedienungshilfen-Freigabe erteilt ist.
    public static var isTrusted: Bool { AXIsProcessTrusted() }

    /// Öffnet den Systemdialog, der zur Freigabe auffordert.
    public static func requestTrust() {
        // Der Konstantenname `kAXTrustedCheckOptionPrompt` ist eine globale
        // `var` und unter Swift 6 nicht zugreifbar; der Literalwert ist
        // gegen ApplicationServices verifiziert.
        let options = ["AXTrustedCheckOptionPrompt": true]
        _ = AXIsProcessTrustedWithOptions(options as CFDictionary)
    }

    @discardableResult
    public func start() -> Bool {
        guard tap == nil else { return true }

        let mask = (1 << CGEventType.keyDown.rawValue)
            | (1 << CGEventType.keyUp.rawValue)

        guard let port = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: CGEventMask(mask),
            callback: KeyboardTap.callback,
            userInfo: Unmanaged.passUnretained(self).toOpaque()
        ) else {
            // Passiert, wenn die Bedienungshilfen-Freigabe fehlt.
            return false
        }

        let source = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, port, 0)
        CFRunLoopAddSource(CFRunLoopGetMain(), source, .commonModes)
        CGEvent.tapEnable(tap: port, enable: true)

        tap = port
        runLoopSource = source
        return true
    }

    public func stop() {
        guard let port = tap else { return }
        CGEvent.tapEnable(tap: port, enable: false)
        if let source = runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetMain(), source, .commonModes)
        }
        CFMachPortInvalidate(port)
        tap = nil
        runLoopSource = nil
    }

    nonisolated private static let callback: CGEventTapCallBack = { _, type, event, refcon in
        guard let refcon else { return Unmanaged.passUnretained(event) }
        let tap = Unmanaged<KeyboardTap>.fromOpaque(refcon).takeUnretainedValue()

        // macOS schaltet den Tap ab, wenn der Callback zu lange braucht.
        // Das ist das Sicherheitsnetz gegen eine tote Tastatur — wir bauen
        // ihn kontrolliert wieder auf.
        if type == .tapDisabledByTimeout || type == .tapDisabledByUserInput {
            MainActor.assumeIsolated { tap.reenable() }
            return Unmanaged.passUnretained(event)
        }

        // Skalare extrahieren, BEVOR die Isolationsgrenze überquert wird:
        // CGEvent ist nicht Sendable und darf nicht durch die Closure.
        let keycode = UInt16(event.getIntegerValueField(.keyboardEventKeycode))
        let isRepeat = event.getIntegerValueField(.keyboardEventAutorepeat) != 0
        let flags = event.flags.rawValue
        let isKeyDown = (type == .keyDown)

        // Der RunLoop-Source hängt am Main-RunLoop, der Callback läuft also
        // garantiert auf dem Main Thread.
        let swallow = MainActor.assumeIsolated {
            tap.controller.handle(
                keycode: keycode, flags: flags,
                isKeyDown: isKeyDown, isRepeat: isRepeat
            )
        }
        return swallow ? nil : Unmanaged.passUnretained(event)
    }

    private func reenable() {
        guard let port = tap else { return }
        CGEvent.tapEnable(tap: port, enable: true)
    }
}

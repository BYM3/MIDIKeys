import AppKit
import MIDIKeysCore
import SwiftUI

/// Ein Feld, das beim Klick auf die nächste Tastenkombination wartet.
///
/// `leading` steht links in derselben Zeile wie der Aufnahme-Knopf — hier
/// sitzt der Klavier-Ton-Knopf, an der Stelle der früheren „Umschalten“-Beschriftung.
struct HotkeyRecorderView<Leading: View>: View {
    @Binding var hotkey: Hotkey
    let sprache: Sprache
    @ViewBuilder var leading: () -> Leading
    @State private var zeichnetAuf = false
    @State private var monitor: Any?
    @State private var fehler: String?

    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            HStack {
                leading()
                Spacer()
                Text(sprache.text("Umschalten", "Toggle"))
                    .font(.caption)
                    .foregroundStyle(.secondary)
                Button(action: umschalten) {
                    Text(zeichnetAuf ? sprache.text("Taste drücken …", "Press keys …") : anzeige)
                        .frame(minWidth: 90)
                        .monospaced()
                }
                .buttonStyle(.bordered)
                .tint(zeichnetAuf ? .accentColor : nil)
            }
            if let fehler {
                Text(fehler)
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .onDisappear(perform: beenden)
    }

    private var anzeige: String {
        hotkey.displayName(keyLabel: KeyLabelResolver.label(for: hotkey.keycode).uppercased())
    }

    private func umschalten() {
        zeichnetAuf ? beenden() : starten()
    }

    private func starten() {
        fehler = nil
        zeichnetAuf = true
        monitor = NSEvent.addLocalMonitorForEvents(matching: [.keyDown]) { event in
            // Escape bricht ab, ohne etwas zu ändern.
            if event.keyCode == 0x35 {
                beenden()
                return nil
            }
            // NSEvent- und CGEvent-Modifier teilen dieselben Bits.
            if let neuer = HotkeyRecording.hotkey(
                fromKeycode: event.keyCode,
                modifierFlagsRaw: UInt64(event.modifierFlags.rawValue)
            ) {
                hotkey = neuer
                beenden()
            } else {
                fehler = sprache.text("Mindestens ⌃, ⌥ oder ⌘ verwenden.", "Use at least ⌃, ⌥ or ⌘.")
            }
            return nil  // Event verschlucken, damit es nichts auslöst
        }
    }

    private func beenden() {
        zeichnetAuf = false
        if let monitor { NSEvent.removeMonitor(monitor) }
        monitor = nil
    }
}

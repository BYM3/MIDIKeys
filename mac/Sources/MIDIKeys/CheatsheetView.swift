import AppKit
import MIDIKeysCore
import SwiftUI

struct CheatsheetView: View {
    @Bindable var state: AppState
    let controller: InputController
    /// 100 ms: unter der Wahrnehmungsschwelle, deckt aber auch unsauber
    /// angeschlagene Akkorde ab (siehe `Entpreller`).
    @State private var akkord = Entpreller<String?>(nil, verzoegerung: .milliseconds(100))
    @State private var zeigeEinstellungen = false

    var body: some View {
        if zeigeEinstellungen {
            EinstellungenView(state: state) { zeigeEinstellungen = false }
        } else {
            hauptansicht
        }
    }

    private var hauptansicht: some View {
        VStack(alignment: .leading, spacing: 12) {
            kopf
            if !state.hasAccessibility { berechtigungsHinweis }
            if let fehler = state.tapError { fehlerHinweis(fehler) }
            Divider()
            klaviatur
            steuerung
            Divider()
            HotkeyRecorderView(hotkey: $state.hotkey, sprache: state.sprache) {
                HStack(spacing: 6) {
                    klavierTonKnopf
                    advKnopf
                }
            }
            fuss
        }
        .padding(14)
        .frame(width: 360)
    }

    private func t(_ deutsch: String, _ english: String) -> String {
        state.sprache.text(deutsch, english)
    }

    private var kopf: some View {
        HStack {
            Text(akkord.wert ?? "MIDIKeys")
                .font(.headline)
                // Lange Namen wie "G# maj7(no5)/F# · 3. Umk." nie umbrechen.
                .lineLimit(1)
                .minimumScaleFactor(0.8)
                .contentTransition(.opacity)
                .animation(.easeOut(duration: 0.1), value: akkord.wert)
                .onChange(of: state.chordName, initial: true) { _, neu in akkord.setze(neu) }
            Spacer()
            Toggle("", isOn: Binding(
                get: { state.isActive },
                set: { controller.setActive($0) }
            ))
            .labelsHidden()
            .toggleStyle(.switch)
            .disabled(!state.hasAccessibility)
        }
    }

    private var berechtigungsHinweis: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(t("Bedienungshilfen-Freigabe fehlt", "Accessibility permission missing"))
                .font(.subheadline).bold()
            Text(t("Ohne sie kann MIDIKeys keine Tasten lesen. Nach dem Erteilen "
                   + "wirkt die Freigabe sofort — kein Neustart nötig.",
                   "Without it, MIDIKeys can't read keys. Once granted, it takes "
                   + "effect immediately — no restart needed."))
                .font(.caption)
                .foregroundStyle(.secondary)
            Button(t("Freigabe erteilen …", "Grant permission …")) { KeyboardTap.requestTrust() }
                .controlSize(.small)
        }
        .padding(8)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(RoundedRectangle(cornerRadius: 6).fill(.quaternary))
    }

    private func fehlerHinweis(_ text: String) -> some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(t("Tastatur wird nicht gelesen", "Keyboard is not being read"))
                .font(.subheadline).bold()
            Text(text)
                .font(.caption)
                .foregroundStyle(.secondary)
                .fixedSize(horizontal: false, vertical: true)
        }
        .padding(8)
        .frame(maxWidth: .infinity, alignment: .leading)
        .background(RoundedRectangle(cornerRadius: 6).fill(.quaternary))
    }

    /// Klaviatur-Grafik. Im Grundlayout eine einzelne Reihe (12 weiße
    /// Tasten). Im Adv.-Layout zwei gestapelte Reihen statt einer sehr
    /// breiten — sonst würde das Popover auf über 600pt Breite anwachsen.
    /// Der Schnitt bei Halbton 16|17 liegt an der natürlichen Lücke E–F
    /// (keine schwarze Taste dort), damit keine schwarze Taste ihre
    /// benachbarte weiße Taste in der jeweils anderen Reihe verliert.
    private var klaviatur: some View {
        Group {
            if state.advancedLayoutEnabled {
                // Obere Reihe der Grafik = obere Tastaturzone (top+number
                // rows, höhere Töne), untere Reihe = untere Tastaturzone
                // (bottom+home rows, tiefere Töne) — räumlich wie am
                // echten Keyboard, nicht nach Tonhöhe sortiert.
                VStack(spacing: 6) {
                    PianoDiagram(
                        keyboard: state.keyboard, pressed: state.pressedKeycodes,
                        noteKeycodes: AdvancedKeyMap.noteKeycodes, semitoneRange: 17...36,
                        keyAction: AdvancedKeyMap.action(for:), sprache: state.sprache
                    )
                    PianoDiagram(
                        keyboard: state.keyboard, pressed: state.pressedKeycodes,
                        noteKeycodes: AdvancedKeyMap.noteKeycodes, semitoneRange: 0...16,
                        keyAction: AdvancedKeyMap.action(for:), sprache: state.sprache
                    )
                }
            } else {
                PianoDiagram(
                    keyboard: state.keyboard, pressed: state.pressedKeycodes,
                    noteKeycodes: Dictionary(uniqueKeysWithValues: KeyMap.noteKeycodes.enumerated().map { ($0.offset, $0.element) }),
                    semitoneRange: KeyMap.semitoneRange,
                    keyAction: KeyMap.action(for:), sprache: state.sprache
                )
            }
        }
    }

    private var steuerung: some View {
        let oktavAbKeycode = state.advancedLayoutEnabled
            ? AdvancedKeyMap.octaveDownKeycode : KeyMap.octaveDownKeycode
        let oktavAufKeycode = state.advancedLayoutEnabled
            ? AdvancedKeyMap.octaveUpKeycode : KeyMap.octaveUpKeycode
        // Wie die Klaviertasten aus dem aktiven Layout — auf US „z“ statt „y“.
        let oktavAb = KeyLabelResolver.label(for: oktavAbKeycode)
        let oktavAuf = KeyLabelResolver.label(for: oktavAufKeycode)
        let maxSemitone = state.advancedLayoutEnabled
            ? AdvancedKeyMap.semitoneRange.upperBound : KeyMap.semitoneRange.upperBound

        return HStack(spacing: 16) {
            beschriftung(oktavAb, t("Oktave ab", "Octave down"), keycode: oktavAbKeycode)
            beschriftung(oktavAuf, t("Oktave auf", "Octave up"), keycode: oktavAufKeycode)
            beschriftung("⇥", "Sustain", keycode: KeyMap.sustainKeycode)
            Spacer()
            Text(state.keyboard.rangeDescription(maxSemitone: maxSemitone, sprache: state.sprache))
                .font(.system(size: 12, design: .rounded)).bold()
                .monospacedDigit()
        }
        .font(.caption)
    }

    /// Klavier-Ton an/aus. Tippen schaltet um. Solange kein lokaler
    /// Klavier-Ton verfügbar ist (Audio-Sampler konnte nicht gestartet
    /// werden), bleibt er deaktiviert — der normale MIDI-Betrieb läuft
    /// davon unberührt weiter.
    private var klavierTonKnopf: some View {
        pillKnopf("Piano", aktiv: state.pianoModeEnabled, verfuegbar: state.pianoModeAvailable) {
            state.pianoModeEnabled.toggle()
        }
    }

    /// Erweiterte Tastenbelegung (vier statt zwei Reihen, 38 statt 20
    /// Notentasten) an/aus. Immer verfügbar, keine Ausfallbedingung wie
    /// beim Klavier-Ton.
    private var advKnopf: some View {
        pillKnopf("Adv.", aktiv: state.advancedLayoutEnabled, verfuegbar: true) {
            state.advancedLayoutEnabled.toggle()
        }
    }

    /// Knopf in Textgröße: gefüllt mit der Akzentfarbe, solange aktiv,
    /// sonst in der ursprünglichen, neutralen Farbe.
    private func pillKnopf(_ titel: String, aktiv: Bool, verfuegbar: Bool, _ aktion: @escaping () -> Void) -> some View {
        Button(action: aktion) {
            Text(titel)
                .font(.system(size: 12, weight: .semibold, design: .rounded))
                .foregroundStyle(aktiv ? AnyShapeStyle(.white) : AnyShapeStyle(.primary))
                .padding(.horizontal, 8).padding(.vertical, 3)
                .background(
                    RoundedRectangle(cornerRadius: 5)
                        .fill(aktiv ? AnyShapeStyle(Color.accentColor) : AnyShapeStyle(.quaternary))
                )
        }
        .buttonStyle(.plain)
        .disabled(!verfuegbar)
        .opacity(verfuegbar ? 1 : 0.4)
        .animation(.easeOut(duration: 0.1), value: aktiv)
    }

    private func beschriftung(_ taste: String, _ text: String, keycode: UInt16) -> some View {
        let gedrueckt = state.pressedKeycodes.contains(keycode)
        return HStack(spacing: 4) {
            Text(taste)
                .font(.system(size: 11, weight: .semibold, design: .rounded))
                .foregroundStyle(gedrueckt ? AnyShapeStyle(.white) : AnyShapeStyle(.primary))
                .padding(.horizontal, 5).padding(.vertical, 2)
                .background(
                    RoundedRectangle(cornerRadius: 3)
                        .fill(gedrueckt ? AnyShapeStyle(Color.accentColor) : AnyShapeStyle(.quaternary))
                )
            Text(text).foregroundStyle(.secondary)
        }
        .animation(.easeOut(duration: 0.08), value: gedrueckt)
    }

    private var fuss: some View {
        HStack {
            Text(t("Sendet an das MIDI-Gerät „MIDIKeys“", "Sends to the MIDI device “MIDIKeys”"))
                .font(.caption2).foregroundStyle(.secondary)
            Spacer()
            // Systemknopf wie „Beenden“, aber quadratisch: Der Knopf legt
            // 12 pt Rand um den Inhalt, 4 pt Inhalt ergeben 16 × 16 — genau
            // seine Höhe. Das Symbol ragt mittig über den schmalen Rahmen.
            Button { zeigeEinstellungen = true } label: {
                Image(systemName: "gearshape").frame(width: 4)
            }
            .controlSize(.small)
            .help(t("Einstellungen", "Settings"))
            Button(t("Beenden", "Quit")) { NSApplication.shared.terminate(nil) }
                .controlSize(.small)
        }
    }
}

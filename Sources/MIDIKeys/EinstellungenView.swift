import MIDIKeysCore
import SwiftUI

/// Einstellungen, im selben Popover statt in einem eigenen Fenster.
struct EinstellungenView: View {
    @Bindable var state: AppState
    let zurueck: () -> Void
    /// Beim Öffnen frisch gelesen — der Eintrag kann auch in den
    /// Systemeinstellungen umgeschaltet worden sein.
    @State private var beimAnmeldenOeffnen = LoginItem.isEnabled

    var body: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Button(action: zurueck) {
                    Label(t("Zurück", "Back"), systemImage: "chevron.left")
                }
                .buttonStyle(.plain)
                .foregroundStyle(.secondary)
                Spacer()
                Text(t("Einstellungen", "Settings")).font(.headline)
                Spacer()
                // Gleich breiter Platzhalter, damit der Titel mittig sitzt.
                Label(t("Zurück", "Back"), systemImage: "chevron.left").hidden()
            }
            Divider()
            Toggle(t("Beim Anmelden öffnen", "Launch at login"), isOn: Binding(
                get: { beimAnmeldenOeffnen },
                set: { neu in
                    LoginItem.setEnabled(neu)
                    beimAnmeldenOeffnen = LoginItem.isEnabled
                }
            ))
            .toggleStyle(.checkbox)
            Toggle(t("Tasten c und r freigeben (z. B. für Ableton)",
                     "Let the c and r keys through (e.g. for Ableton)"),
                   isOn: $state.cUndRFrei)
                .toggleStyle(.checkbox)
                .help(t(
                    "Gilt nur im Grundlayout mit MIDI-Ausgang – nicht mit Adv. oder Piano.",
                    "Only in the basic layout with MIDI output – not with Adv. or Piano."
                ))
            Picker(t("Sprache", "Language"), selection: $state.sprache) {
                ForEach(Sprache.allCases, id: \.self) { Text($0.name).tag($0) }
            }
            .pickerStyle(.segmented)
            .fixedSize()
        }
        .padding(14)
        .frame(width: 360)
    }

    private func t(_ deutsch: String, _ english: String) -> String {
        state.sprache.text(deutsch, english)
    }
}

import Foundation
import Testing
@testable import MIDIKeysCore

private struct Fehlschlag: Error {}

private func frischeDefaults() -> UserDefaults {
    let suite = "midikeys.tests.\(UUID().uuidString)"
    let defaults = UserDefaults(suiteName: suite)!
    defaults.removePersistentDomain(forName: suite)
    return defaults
}

@Test func ersterStartTraegtAlsAnmeldeobjektEin() {
    var aufrufe = 0
    LoginItem.registerOnFirstLaunch(defaults: frischeDefaults()) { aufrufe += 1 }
    #expect(aufrufe == 1)
}

@Test func spaetereStartsTragenNichtErneutEin() {
    // Wer es in den Systemeinstellungen abschaltet, soll es abgeschaltet
    // behalten — die App darf es beim nächsten Start nicht wieder anlegen.
    let defaults = frischeDefaults()
    var aufrufe = 0
    LoginItem.registerOnFirstLaunch(defaults: defaults) { aufrufe += 1 }
    LoginItem.registerOnFirstLaunch(defaults: defaults) { aufrufe += 1 }
    #expect(aufrufe == 1)
}

@Test func fehlgeschlagenesEintragenWirdBeimNaechstenStartWiederholt() {
    let defaults = frischeDefaults()
    LoginItem.registerOnFirstLaunch(defaults: defaults) { throw Fehlschlag() }
    var aufrufe = 0
    LoginItem.registerOnFirstLaunch(defaults: defaults) { aufrufe += 1 }
    #expect(aufrufe == 1)
}

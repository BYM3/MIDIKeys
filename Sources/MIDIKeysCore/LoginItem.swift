import Foundation
import ServiceManagement

/// „Beim Anmelden öffnen“ — ohne eigenen Schalter in der App. Der Eintrag
/// wird einmalig beim ersten Start angelegt; abschalten lässt er sich danach
/// nur in den Systemeinstellungen (Allgemein › Anmeldeobjekte).
public enum LoginItem {
    static let registeredKey = "loginItemRegistered"

    /// Trägt die App beim ersten Start als Anmeldeobjekt ein. Danach nie
    /// wieder — sonst käme ein in den Systemeinstellungen abgeschalteter
    /// Eintrag beim nächsten Start zurück. Schlägt das Eintragen fehl, wird
    /// es beim nächsten Start erneut versucht.
    public static func registerOnFirstLaunch(
        defaults: UserDefaults = .standard,
        register: () throws -> Void = { try SMAppService.mainApp.register() }
    ) {
        guard !defaults.bool(forKey: registeredKey) else { return }
        do {
            try register()
            defaults.set(true, forKey: registeredKey)
            Diagnose.log("Anmeldeobjekt eingetragen")
        } catch {
            Diagnose.log("Anmeldeobjekt nicht eingetragen: \(error)")
        }
    }

    /// Ob die App gerade als Anmeldeobjekt eingetragen und aktiv ist —
    /// immer frisch abgefragt, weil sie auch in den Systemeinstellungen
    /// umgeschaltet werden kann.
    public static var isEnabled: Bool {
        SMAppService.mainApp.status == .enabled
    }

    /// Schalter in den MIDIKeys-Einstellungen. Das Merkzeichen aus
    /// `registerOnFirstLaunch` bleibt gesetzt, damit ein hier abgeschalteter
    /// Eintrag beim nächsten Start nicht wiederkommt.
    public static func setEnabled(_ an: Bool, defaults: UserDefaults = .standard) {
        defaults.set(true, forKey: registeredKey)
        do {
            if an {
                try SMAppService.mainApp.register()
            } else {
                try SMAppService.mainApp.unregister()
            }
            Diagnose.log("Anmeldeobjekt \(an ? "eingeschaltet" : "ausgeschaltet")")
        } catch {
            Diagnose.log("Anmeldeobjekt umschalten fehlgeschlagen: \(error)")
        }
    }
}

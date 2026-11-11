import Foundation

/// Sprache der Oberfläche — in den Einstellungen umschaltbar, unabhängig
/// von der Systemsprache.
public enum Sprache: String, CaseIterable, Sendable {
    case deutsch = "de"
    case english = "en"

    /// Ohne gespeicherte Wahl: Deutsch, wenn das System Deutsch bevorzugt,
    /// sonst Englisch.
    public static func ausSystem(_ bevorzugt: [String] = Locale.preferredLanguages) -> Sprache {
        bevorzugt.first?.hasPrefix("de") == true ? .deutsch : .english
    }

    /// Name in der eigenen Sprache — so findet man sie auch, wenn man die
    /// gerade eingestellte nicht versteht.
    public var name: String {
        switch self {
        case .deutsch: "Deutsch"
        case .english: "English"
        }
    }

    /// Wählt den passenden Text. Die App hat nur zwei Sprachen und eine
    /// Handvoll Texte — beide nebeneinander an der Aufrufstelle bleiben
    /// übersichtlicher als eine getrennte Übersetzungstabelle.
    public func text(_ deutsch: String, _ english: String) -> String {
        self == .deutsch ? deutsch : english
    }
}

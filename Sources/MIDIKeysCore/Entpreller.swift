import Observation

/// Übernimmt einen neuen Wert erst, wenn er eine Weile stabil geblieben ist.
///
/// Für die Akkordanzeige: Die Finger treffen die Tasten eines Akkords nie
/// exakt gleichzeitig. Ohne Entprellen blitzte zwischen erster und letzter
/// Taste kurz ein Intervall auf (C, C–E, dann erst C maj) — beim Loslassen
/// ebenso. Im Leerlauf läuft nichts; gewartet wird nur nach einer Änderung.
@MainActor
@Observable
public final class Entpreller<Wert: Equatable> {
    public private(set) var wert: Wert
    @ObservationIgnored private let verzoegerung: Duration
    @ObservationIgnored private var ausstehend: Task<Void, Never>?

    public init(_ wert: Wert, verzoegerung: Duration) {
        self.wert = wert
        self.verzoegerung = verzoegerung
    }

    public func setze(_ neu: Wert) {
        ausstehend?.cancel()
        ausstehend = nil
        guard neu != wert else { return }
        ausstehend = Task { [weak self, verzoegerung] in
            try? await Task.sleep(for: verzoegerung)
            guard !Task.isCancelled else { return }
            self?.wert = neu
        }
    }
}

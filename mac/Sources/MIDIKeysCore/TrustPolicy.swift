/// Was zu tun ist, wenn sich der Freigabe-Status geändert hat.
public enum TrustAction: Equatable, Sendable {
    case nothing
    case startTap
    case stopTap
}

/// Entscheidet, ob der Event-Tap angelegt oder abgeräumt werden muss.
///
/// Eigene reine Funktion, weil es für die Bedienungshilfen-Freigabe keine
/// verlässliche Systembenachrichtigung gibt: Der Status wird abgefragt, und
/// die Reaktion darauf soll ohne Systemberechtigungen testbar sein.
public enum TrustPolicy {
    public static func decide(isTrusted: Bool, tapRunning: Bool) -> TrustAction {
        switch (isTrusted, tapRunning) {
        case (true, false): return .startTap
        case (false, true): return .stopTap
        default: return .nothing
        }
    }
}

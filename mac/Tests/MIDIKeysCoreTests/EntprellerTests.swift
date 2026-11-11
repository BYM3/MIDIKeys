import Testing
@testable import MIDIKeysCore

// Echte, aber kurze Wartezeiten: 50 ms Verzögerung, geprüft mit großem
// Abstand davor und danach, damit der Test auf langsamen Maschinen stabil bleibt.
private let verzoegerung = Duration.milliseconds(50)

/// Wartet, bis die Bedingung gilt — statt einer festen Zeit. Laufen viele
/// Tests parallel, ist der Main Actor mitunter länger belegt; eine feste
/// Wartezeit reichte dann gelegentlich nicht, obwohl der Entpreller korrekt
/// arbeitet. Gibt nach spätestens 5 s auf.
@MainActor
private func warteBis(_ bedingung: () -> Bool) async throws {
    let ende = ContinuousClock.now + .seconds(5)
    while !bedingung(), ContinuousClock.now < ende {
        try await Task.sleep(for: .milliseconds(10))
    }
}

@Test @MainActor func neuerWertErscheintErstNachDerVerzoegerung() async throws {
    let anzeige = Entpreller<String?>(nil, verzoegerung: verzoegerung)
    anzeige.setze("C maj")
    #expect(anzeige.wert == nil)
    try await warteBis { anzeige.wert != nil }
    #expect(anzeige.wert == "C maj")
}

@Test @MainActor func zwischenstandWirdNieAngezeigt() async throws {
    // C, dann E, dann G in schneller Folge: die Terz blitzt nicht auf.
    let anzeige = Entpreller<String?>(nil, verzoegerung: verzoegerung)
    var gesehen: [String?] = []
    anzeige.setze("C–E · große Terz")
    try await Task.sleep(for: .milliseconds(10))
    gesehen.append(anzeige.wert)
    anzeige.setze("C maj")
    try await warteBis { anzeige.wert != nil }
    gesehen.append(anzeige.wert)
    #expect(gesehen == [nil, "C maj"])
}

@Test @MainActor func gleicherWertStartetDieWartezeitNichtNeu() async throws {
    // Sonst würde jede unrelevante Änderung (z. B. eine Steuertaste) die
    // bereits laufende Anzeige verzögern.
    let anzeige = Entpreller<String?>("C maj", verzoegerung: verzoegerung)
    anzeige.setze("C maj")
    #expect(anzeige.wert == "C maj")
}

@Test @MainActor func rueckkehrZumAngezeigtenWertBrichtDenWechselAb() async throws {
    // C maj gehalten, kurz E angehoben und wieder gedrückt: bleibt C maj.
    let anzeige = Entpreller<String?>("C maj", verzoegerung: verzoegerung)
    anzeige.setze("C–G · Quinte")
    anzeige.setze("C maj")
    try await Task.sleep(for: .milliseconds(200))
    #expect(anzeige.wert == "C maj")
}

import Testing
@testable import MIDIKeysCore

@Test func freigabeErteiltUndTapLaeuftNicht_dannStarten() {
    #expect(TrustPolicy.decide(isTrusted: true, tapRunning: false) == .startTap)
}

@Test func freigabeEntzogenUndTapLaeuft_dannStoppen() {
    // Sonst bliebe ein toter Tap zurück und der Modus wirkte weiter aktiv.
    #expect(TrustPolicy.decide(isTrusted: false, tapRunning: true) == .stopTap)
}

@Test func nichtsZuTunWennZustandPasst() {
    #expect(TrustPolicy.decide(isTrusted: true, tapRunning: true) == .nothing)
    #expect(TrustPolicy.decide(isTrusted: false, tapRunning: false) == .nothing)
}

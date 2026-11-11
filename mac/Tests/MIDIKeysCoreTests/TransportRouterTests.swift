import Testing
@testable import MIDIKeysCore

private final class SpyTransport: MIDISending {
    var sent: [MIDIMessage] = []
    func send(_ message: MIDIMessage) { sent.append(message) }
}

@Test @MainActor func notenGehenImNormalmodusAnsExterneZiel() {
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.send(.noteOn(note: 60, velocity: 100))

    #expect(external.sent == [.noteOn(note: 60, velocity: 100)])
    #expect(local.sent.isEmpty)
}

@Test @MainActor func notenGehenImKlavierModusAnsLokaleZiel() {
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)
    router.pianoModeEnabled = true

    router.send(.noteOn(note: 60, velocity: 100))

    #expect(local.sent == [.noteOn(note: 60, velocity: 100)])
    #expect(external.sent.isEmpty)
}

@Test @MainActor func noteOffGehtAnDasZielDesZugehoerigenNoteOns() {
    // Der kritische Fall: eine Note wird extern gestartet, dann wird
    // mitten im Halten auf Klavier-Modus umgeschaltet. Das Note-Off muss
    // trotzdem extern ankommen, sonst haengt der Ton in der DAW fuer immer.
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.send(.noteOn(note: 60, velocity: 100))
    router.pianoModeEnabled = true
    router.send(.noteOff(note: 60))

    #expect(external.sent == [.noteOn(note: 60, velocity: 100), .noteOff(note: 60)])
    #expect(local.sent.isEmpty)
}

@Test @MainActor func noteOffGehtAnDasLokaleZielWennDortGestartet() {
    // Gleicher Fall umgekehrt: lokal gestartet, dann zurueck auf MIDI
    // umgeschaltet, waehrend die Taste noch gehalten wird.
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.pianoModeEnabled = true
    router.send(.noteOn(note: 60, velocity: 100))
    router.pianoModeEnabled = false
    router.send(.noteOff(note: 60))

    #expect(local.sent == [.noteOn(note: 60, velocity: 100), .noteOff(note: 60)])
    #expect(external.sent.isEmpty)
}

@Test @MainActor func mehrereNotenBehaltenIhrEigenesZielUnabhaengigVoneinander() {
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.send(.noteOn(note: 60, velocity: 100))   // extern
    router.pianoModeEnabled = true
    router.send(.noteOn(note: 64, velocity: 100))   // lokal
    router.send(.noteOff(note: 60))                 // muss extern bleiben
    router.send(.noteOff(note: 64))                 // muss lokal bleiben

    #expect(external.sent == [.noteOn(note: 60, velocity: 100), .noteOff(note: 60)])
    #expect(local.sent == [.noteOn(note: 64, velocity: 100), .noteOff(note: 64)])
}

@Test @MainActor func noteOffOhneBekanntesZielFolgtDemAktuellenModus() {
    // Kann eigentlich nicht vorkommen (MIDIEngine sendet nie ein Note-Off
    // ohne vorheriges Note-On), aber falls doch: kein Crash, sinnvoller
    // Fallback statt eines verlorenen Events.
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)
    router.pianoModeEnabled = true

    router.send(.noteOff(note: 60))

    #expect(local.sent == [.noteOff(note: 60)])
    #expect(external.sent.isEmpty)
}

@Test @MainActor func controlChangeGehtImmerAnBeideZiele() {
    // Sustain/All-Notes-Off betreffen den Gesamtzustand des jeweils
    // aktiven Ziels; ans inaktive Ziel mitzusenden ist harmlos, da dort
    // dank der Zielverfolgung ohnehin nichts mehr klingt.
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.send(.controlChange(controller: 64, value: 127))

    #expect(external.sent == [.controlChange(controller: 64, value: 127)])
    #expect(local.sent == [.controlChange(controller: 64, value: 127)])
}

@Test @MainActor func zielVerfolgungRaeumtNachNoteOffAuf() {
    // Nach dem Note-Off darf kein Eintrag mehr fuer diese Note bestehen,
    // sonst koennte ein spaeteres Note-Off derselben Tonhoehe faelschlich
    // das alte Ziel treffen.
    let external = SpyTransport()
    let local = SpyTransport()
    let router = TransportRouter(external: external, local: local)

    router.send(.noteOn(note: 60, velocity: 100))
    router.send(.noteOff(note: 60))
    router.pianoModeEnabled = true
    router.send(.noteOn(note: 60, velocity: 100))
    router.send(.noteOff(note: 60))

    #expect(external.sent == [.noteOn(note: 60, velocity: 100), .noteOff(note: 60)])
    #expect(local.sent == [.noteOn(note: 60, velocity: 100), .noteOff(note: 60)])
}

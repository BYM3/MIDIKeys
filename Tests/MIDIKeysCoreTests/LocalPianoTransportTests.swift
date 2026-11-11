import AVFoundation
import Testing
@testable import MIDIKeysCore

/// Simuliert, was macOS bei einer Konfigurationsänderung des Audiogeräts
/// (Ruhezustand, Kopfhörer, DAW, Samplerate) mit der Audio-Unit macht:
/// Engine gestoppt, Render-Ressourcen neu angelegt. Der Sampler verliert
/// dabei sein geladenes Instrument und spielt nur noch einen Sinus.
private func simuliereKonfigurationswechsel(_ klavier: LocalPianoTransport) throws {
    klavier.engine.stop()
    klavier.sampler.auAudioUnit.deallocateRenderResources()
    try klavier.sampler.auAudioUnit.allocateRenderResources()
}

// Seriell: mehrere AVAudioEngines, die gleichzeitig starten, schlagen
// sporadisch fehl — dann wäre der Konstruktor nil, nicht der Code kaputt.
@Suite(.serialized) struct LocalPianoTransportTests {
    @Test func klavierIstNachDemStartGeladen() throws {
        let klavier = try #require(LocalPianoTransport())
        #expect(klavier.klavierGeladen)
    }

    @Test func nachKonfigurationswechselLaedtDieNaechsteNoteDasKlavierNeu() throws {
        let klavier = try #require(LocalPianoTransport())
        try simuliereKonfigurationswechsel(klavier)
        #expect(!klavier.klavierGeladen) // Voraussetzung: der Sinus-Zustand ist erkennbar

        klavier.send(.noteOn(note: 60, velocity: 100))
        klavier.send(.noteOff(note: 60))

        #expect(klavier.klavierGeladen)
        #expect(klavier.engine.isRunning)
    }

    @Test func klavierWirdAuchBeiLaufenderEngineNeuGeladen() throws {
        // Die Engine kann wieder laufen, das Instrument aber trotzdem fehlen —
        // eine reine isRunning-Prüfung übersieht genau diesen Fall.
        let klavier = try #require(LocalPianoTransport())
        try simuliereKonfigurationswechsel(klavier)
        try klavier.engine.start()

        klavier.send(.noteOn(note: 60, velocity: 100))
        klavier.send(.noteOff(note: 60))

        #expect(klavier.klavierGeladen)
    }

    // MARK: - Energiesparen

    @Test func engineSchlaeftNachDemStart() throws {
        // Die App startet immer inaktiv — dann soll kein Audio-Thread laufen.
        let klavier = try #require(LocalPianoTransport())
        #expect(!klavier.engine.isRunning)
        #expect(klavier.klavierGeladen)
    }

    @Test func aktivierenWecktUndDeaktivierenLegtSchlafen() throws {
        let klavier = try #require(LocalPianoTransport())
        klavier.setAktiv(true)
        #expect(klavier.engine.isRunning)
        klavier.setAktiv(false)
        #expect(!klavier.engine.isRunning)
    }

    @Test func controlChangeWecktDieSchlafendeEngineNicht() throws {
        // Der Router schickt Sustain-CCs immer an beide Ziele — auch wenn der
        // Klavier-Ton aus ist. Das darf die Engine nicht aufwecken.
        let klavier = try #require(LocalPianoTransport())
        klavier.send(.controlChange(controller: 64, value: 127))
        klavier.send(.noteOff(note: 60))
        #expect(!klavier.engine.isRunning)
    }

    @Test func noteOnSpieltAuchBeiSchlafenderEngine() throws {
        // Sicherheitsnetz: Kommt doch eine Note an, wird sie auch gespielt.
        let klavier = try #require(LocalPianoTransport())
        klavier.send(.noteOn(note: 60, velocity: 100))
        #expect(klavier.engine.isRunning)
        klavier.send(.noteOff(note: 60))
    }
}

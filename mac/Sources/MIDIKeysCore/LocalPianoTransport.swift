import AVFoundation

/// Spielt Noten lokal hörbar über Apples eingebautes General-MIDI-Soundset
/// ab (Programm 0 = Acoustic Grand Piano) — keine eigenen Samples nötig.
///
/// Übernimmt eine andere App (z. B. eine DAW) das Audiogerät oder ändert
/// dessen Konfiguration (Samplerate, exklusiver Zugriff), kann macOS'
/// `AVAudioEngine` intern stumm werden: `startNote` läuft weiter ohne
/// Fehler, es kommt aber kein Ton mehr — bis die Engine neu gestartet wird.
/// Außerdem setzt macOS dabei die Audio-Unit zurück (auch beim Aufwachen
/// aus dem Ruhezustand oder beim Wechsel auf Kopfhörer/Bluetooth): Der
/// Sampler verliert sein Klavier und spielt nur noch sein eingebautes
/// Default-Instrument, einen Sinus — transponiert, aber eben kein Klavier.
/// Deshalb wird sowohl auf die dafür vorgesehene Systembenachrichtigung
/// gehört als auch vor jeder Note sicherheitshalber geprüft: Engine läuft?
/// Klavier noch geladen? Beides kostet je nur Hundertstel Millisekunden.
///
/// Energiesparen: Eine laufende Engine hält einen Audio-Thread wach, der
/// alle paar Millisekunden rechnet — auch in völliger Stille. Darum schläft
/// sie, solange niemand spielen kann (`setAktiv(false)`), und wacht erst
/// beim Einschalten wieder auf. Ein Start kostet nur 4–7 ms.
/// `@unchecked Sendable`, weil die Systembenachrichtigung `self` schwach in
/// einen `@Sendable`-Callback einfängt: Der eigentliche Zugriff auf
/// `engine`/`sampler` geschieht dort erst nach dem Sprung auf den Main
/// Thread (`starteBeiBedarfNeu`); `send(_:)` läuft ohnehin immer über den
/// MainActor-isolierten Event-Tap-Pfad — kein gleichzeitiger Zugriff von
/// zwei Threads.
public final class LocalPianoTransport: MIDISending, @unchecked Sendable {
    let engine = AVAudioEngine()
    let sampler = AVAudioUnitSampler()
    private var beobachter: NSObjectProtocol?
    private var sollLaufen = false

    private static let soundbank = URL(fileURLWithPath:
        "/System/Library/Components/CoreAudio.component/Contents/Resources/gs_instruments.dls")

    public init?() {
        engine.attach(sampler)
        engine.connect(sampler, to: engine.mainMixerNode, format: nil)

        do {
            try ladeKlavier()
            // Einmal starten, um zu prüfen, dass Audio überhaupt geht —
            // dann gleich wieder schlafen, bis jemand spielen will.
            try engine.start()
            engine.pause()
        } catch {
            return nil
        }

        // Läuft möglicherweise auf einem Hintergrund-Thread (CoreAudio
        // entscheidet, welcher); der eigentliche Neustart wird darum auf
        // den Main Thread verschoben, statt engine/sampler direkt von dort
        // anzufassen.
        beobachter = NotificationCenter.default.addObserver(
            forName: .AVAudioEngineConfigurationChange, object: engine, queue: nil
        ) { [weak self] _ in
            DispatchQueue.main.async {
                guard let self, self.sollLaufen else { return }
                self.starteBeiBedarfNeu()
            }
        }
    }

    deinit {
        if let beobachter { NotificationCenter.default.removeObserver(beobachter) }
    }

    /// Weckt die Engine (`true`) oder legt sie schlafen (`false`).
    public func setAktiv(_ aktiv: Bool) {
        sollLaufen = aktiv
        if aktiv {
            starteBeiBedarfNeu()
        } else {
            // pause() statt stop(): stop() verwirft das geladene Instrument
            // (Sampler fiele auf den Sinus zurück), pause() nicht — und hält
            // den Audio-Thread genauso an.
            engine.pause()
        }
    }

    public func send(_ message: MIDIMessage) {
        switch message {
        case let .noteOn(note, velocity):
            // Sicherheitsnetz zusätzlich zur Benachrichtigung: falls die
            // Engine steht oder das Klavier verloren hat. Nur hier — ein
            // Note-Off oder Sustain-CC an die schlafende Engine ist ohnehin
            // stumm und soll sie nicht aufwecken.
            starteBeiBedarfNeu()
            sampler.startNote(note, withVelocity: velocity, onChannel: 0)
        case let .noteOff(note):
            sampler.stopNote(note, onChannel: 0)
        case let .controlChange(controller, value):
            sampler.sendController(controller, withValue: value, onChannel: 0)
        }
    }

    /// Ob noch das Klavier geladen ist — oder der Sampler auf sein
    /// eingebautes Default-Instrument zurückgefallen ist (ein Sinus).
    var klavierGeladen: Bool {
        let instrument = sampler.auAudioUnit.fullState?["Instrument"] as? [String: Any]
        return instrument?["Banks"] != nil
    }

    private func ladeKlavier() throws {
        try sampler.loadSoundBankInstrument(
            at: Self.soundbank,
            program: 0,
            bankMSB: UInt8(kAUSampler_DefaultMelodicBankMSB),
            bankLSB: UInt8(kAUSampler_DefaultBankLSB)
        )
    }

    private func starteBeiBedarfNeu() {
        if !engine.isRunning { try? engine.start() }
        if !klavierGeladen { try? ladeKlavier() }
    }
}

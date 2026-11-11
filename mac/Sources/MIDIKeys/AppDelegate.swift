import AppKit
import MIDIKeysCore
import Observation
import SwiftUI

@MainActor
final class AppDelegate: NSObject, NSApplicationDelegate, NSPopoverDelegate {
    private var statusItem: NSStatusItem?
    private var popover: NSPopover?
    private var state: AppState!
    private var controller: InputController!
    private var router: TransportRouter!
    private var localPiano: LocalPianoTransport?
    private var tap: KeyboardTap!
    private var trustTimer: Timer?

    func applicationDidFinishLaunching(_ notification: Notification) {
        Diagnose.log("--- Start, PID \(ProcessInfo.processInfo.processIdentifier) ---")
        Diagnose.log("Bundle: \(Bundle.main.bundlePath)")
        Diagnose.log("AXIsProcessTrusted beim Start: \(KeyboardTap.isTrusted)")
        state = AppState()
        LoginItem.registerOnFirstLaunch()

        guard let coreMIDI = CoreMIDITransport() else {
            zeigeFehler(state.sprache.text("MIDI konnte nicht gestartet werden.",
                                           "MIDI could not be started."),
                        state.sprache.text("Die virtuelle MIDI-Quelle ließ sich nicht anlegen.",
                                           "The virtual MIDI source could not be created."))
            NSApplication.shared.terminate(nil)
            return
        }

        // Schlägt der lokale Audio-Sampler fehl (etwa kein Audiogerät),
        // bleibt der Klavier-Ton-Schalter im Popover deaktiviert — der
        // normale MIDI-Betrieb läuft davon vollkommen unberührt weiter.
        localPiano = LocalPianoTransport()
        state.pianoModeAvailable = (localPiano != nil)
        if localPiano == nil {
            Diagnose.log("Klavier-Ton nicht verfügbar: Audio-Sampler konnte nicht gestartet werden")
        }

        router = TransportRouter(external: coreMIDI, local: localPiano ?? NullTransport())
        router.pianoModeEnabled = state.pianoModeEnabled

        controller = InputController(state: state, engine: MIDIEngine(transport: router))
        tap = KeyboardTap(controller: controller)

        baueStatusItem()
        starteTap()
        beobachteZustand()
        beobachteKlavierTon()
        beobachteAudioBedarf()
        beginneFreigabeUeberwachung()
    }

    /// Fragt die Bedienungshilfen-Freigabe regelmäßig ab und startet den Tap,
    /// sobald sie erteilt ist. Nötig, weil es dafür keine verlässliche
    /// Systembenachrichtigung gibt — ohne das müsste der Nutzer die App neu
    /// starten oder erst das Popover öffnen, damit die Freigabe greift.
    private func beginneFreigabeUeberwachung() {
        trustTimer = Timer.scheduledTimer(withTimeInterval: 1.5, repeats: true) { [weak self] _ in
            Task { @MainActor [weak self] in self?.pruefeFreigabe() }
        }
        // Erlaubt macOS, das Aufwachen mit anderen Timern zu bündeln.
        trustTimer?.tolerance = 0.5
    }

    private func pruefeFreigabe() {
        let vorher = state.hasAccessibility
        state.refreshAccessibility()
        if vorher != state.hasAccessibility {
            Diagnose.log("Freigabe wechselte auf \(state.hasAccessibility)")
        }
        switch TrustPolicy.decide(isTrusted: state.hasAccessibility, tapRunning: tap.isRunning) {
        case .startTap:
            if tap.start() {
                Diagnose.log("Tap gestartet")
                state.tapError = nil
            } else {
                Diagnose.log("FEHLER: tapCreate schlug fehl trotz erteilter Freigabe")
                state.tapError = state.sprache.text(
                    "Der Event-Tap ließ sich nicht anlegen, obwohl "
                        + "die Freigabe erteilt ist. Hilft ein Neustart der App nicht, "
                        + "entferne MIDIKeys in den Bedienungshilfen und setze den Haken neu.",
                    "The event tap could not be created even though permission "
                        + "is granted. If restarting the app doesn't help, remove "
                        + "MIDIKeys from Accessibility and add it again.")
            }
        case .stopTap:
            // Freigabe wurde entzogen: Modus verlassen, damit keine Töne
            // hängen und der Zustand nicht aktiv aussieht.
            controller.setActive(false)
            tap.stop()
        case .nothing:
            break
        }
    }

    func applicationWillTerminate(_ notification: Notification) {
        // Nie mit klingenden Tönen aussteigen.
        controller?.setActive(false)
        tap?.stop()
        trustTimer?.invalidate()
    }

    // MARK: - Menüleiste

    private func baueStatusItem() {
        let item = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        item.button?.image = IconFactory.icon(active: false)
        item.button?.target = self
        item.button?.action = #selector(statusItemGeklickt)
        statusItem = item
    }

    @objc private func statusItemGeklickt() {
        guard let button = statusItem?.button else { return }
        if let popover, popover.isShown {
            popover.performClose(nil)
            return
        }
        state.refreshAccessibility()
        // Falls die Freigabe zwischenzeitlich erteilt wurde.
        if state.hasAccessibility && !tap.isRunning { starteTap() }

        let popover = NSPopover()
        popover.behavior = .transient
        popover.delegate = self
        popover.contentViewController = NSHostingController(
            rootView: CheatsheetView(state: state, controller: controller)
        )
        popover.show(relativeTo: button.bounds, of: button, preferredEdge: .minY)
        // Damit der Shortcut-Recorder Tastenereignisse bekommt.
        NSApplication.shared.activate()
        self.popover = popover
    }

    /// Gibt das geschlossene Popover frei. Sonst bliebe die SwiftUI-View
    /// darin am Leben und würde bei jeder Zustandsänderung unsichtbar neu
    /// gerechnet.
    func popoverDidClose(_ notification: Notification) {
        popover = nil
    }

    // MARK: - Zustand

    /// Hält das Menüleisten-Symbol am aktuellen Modus.
    private func beobachteZustand() {
        withObservationTracking {
            _ = state.isActive
        } onChange: {
            Task { @MainActor [weak self] in
                guard let self else { return }
                self.statusItem?.button?.image = IconFactory.icon(active: self.state.isActive)
                self.beobachteZustand()  // Tracking ist einmalig, neu aufsetzen
            }
        }
    }

    /// Hält den Router am aktuellen Klavier-Ton-Zustand.
    private func beobachteKlavierTon() {
        withObservationTracking {
            _ = state.pianoModeEnabled
        } onChange: {
            Task { @MainActor [weak self] in
                guard let self else { return }
                self.router.pianoModeEnabled = self.state.pianoModeEnabled
                self.beobachteKlavierTon()  // Tracking ist einmalig, neu aufsetzen
            }
        }
    }

    /// Lässt die Audio-Engine nur laufen, wenn tatsächlich Klavier-Ton
    /// gespielt werden kann — sonst hält sie einen Audio-Thread wach.
    private func beobachteAudioBedarf() {
        let gebraucht = withObservationTracking {
            state.isActive && state.pianoModeEnabled
        } onChange: {
            Task { @MainActor [weak self] in self?.beobachteAudioBedarf() }
        }
        localPiano?.setAktiv(gebraucht)
    }

    private func starteTap() {
        state.refreshAccessibility()
        guard state.hasAccessibility else {
            Diagnose.log("Keine Freigabe -> Systemdialog wird angefordert")
            KeyboardTap.requestTrust()
            return
        }
        if tap.start() {
            Diagnose.log("Tap beim Start angelegt")
            state.tapError = nil
        } else {
            Diagnose.log("FEHLER: tapCreate schlug beim Start fehl")
            // Kein modaler Dialog: Der käme in einer App ohne Dock-Icon nicht
            // nach vorn und würde den Main Thread blockieren.
            state.tapError = state.sprache.text(
                "Der Event-Tap ließ sich nicht anlegen. Prüfe die Bedienungshilfen-Freigabe.",
                "The event tap could not be created. Check the Accessibility permission.")
        }
    }

    private func zeigeFehler(_ titel: String, _ text: String) {
        // Ohne activate() bleibt der Dialog hinter allem und der Main Thread
        // haengt in der modalen Schleife — die App wirkt eingefroren.
        NSApplication.shared.activate()
        let alert = NSAlert()
        alert.messageText = titel
        alert.informativeText = text
        alert.alertStyle = .warning
        alert.runModal()
    }
}

/// Tut nichts. Fällt nur ein, wenn der lokale Audio-Sampler beim Start
/// nicht initialisiert werden konnte — der Klavier-Ton-Schalter bleibt
/// dann ohnehin deaktiviert, sodass dieser Pfad nie tatsächlich genutzt wird.
private final class NullTransport: MIDISending {
    func send(_ message: MIDIMessage) {}
}

import ApplicationServices
import Foundation
import Observation

/// Der beobachtbare Zustand der App. Wird von der Oberfläche gelesen und
/// vom Event-Tap geschrieben — beides auf dem Main Actor.
@MainActor
@Observable
public final class AppState {
    private enum Key {
        static let hotkey = "hotkey"
        static let octaveShift = "octaveShift"
        static let pianoModeEnabled = "pianoModeEnabled"
        static let advancedLayoutEnabled = "advancedLayoutEnabled"
        static let sprache = "sprache"
        /// Alter Name aus der Zeit, als nur `r` am Haken hing — beibehalten,
        /// damit eine gesetzte Wahl erhalten bleibt.
        static let cUndRFrei = "rTasteFrei"
    }

    @ObservationIgnored private let defaults: UserDefaults

    /// Ob der MIDI-Modus läuft. Bewusst nicht persistiert: Nach einem
    /// Neustart soll die Tastatur immer normal funktionieren.
    public var isActive = false

    public private(set) var keyboard = Keyboard()

    /// Physische Keycodes, die gerade gedrückt sind — fürs Hervorheben im
    /// Cheatsheet. Keycodes statt Halbtöne, damit Noten, Sustain und die
    /// Oktavtasten über denselben Mechanismus laufen.
    public internal(set) var pressedKeycodes: Set<UInt16> = []

    public var hotkey: Hotkey = .default {
        didSet {
            guard let data = try? JSONEncoder().encode(hotkey) else { return }
            defaults.set(data, forKey: Key.hotkey)
        }
    }

    /// Ob Tasten lokal hörbar sind (Klavier-Ton) statt an den MIDI-Ausgang
    /// zu gehen. Anders als `isActive` bewusst gemerkt — wie Oktavlage und
    /// Shortcut, auf Nutzerwunsch.
    public var pianoModeEnabled = false {
        didSet { defaults.set(pianoModeEnabled, forKey: Key.pianoModeEnabled) }
    }

    /// Ob der lokale Klavier-Ton überhaupt verfügbar ist. Schlägt das Laden
    /// des Audio-Samplers fehl (etwa kein Audiogerät), bleibt der Schalter
    /// im Popover deaktiviert — der normale MIDI-Betrieb läuft unberührt weiter.
    public var pianoModeAvailable = true

    /// Ob die erweiterte Tastenbelegung (Adv., vier statt zwei Reihen, 38
    /// statt 20 Notentasten) aktiv ist. Wie `pianoModeEnabled` gemerkt.
    /// Unabhängig davon, ob Klavier-Ton oder MIDI-Ausgang aktiv ist — beide
    /// Schalter betreffen unterschiedliche, voneinander unabhängige Dinge
    /// (wohin der Ton geht vs. welche Tasten welche Noten sind).
    public var advancedLayoutEnabled = false {
        didSet { defaults.set(advancedLayoutEnabled, forKey: Key.advancedLayoutEnabled) }
    }

    /// Ob `c` und `r` im Grundlayout mit MIDI-Ausgang als normale Tasten
    /// durchgehen — z. B. für Ableton. Gemerkt, Standard: gesperrt.
    public var cUndRFrei = false {
        didSet { defaults.set(cUndRFrei, forKey: Key.cUndRFrei) }
    }

    /// Sprache der Oberfläche. Gemerkt; ohne gespeicherte Wahl folgt sie
    /// dem System.
    public var sprache: Sprache = .ausSystem() {
        didSet { defaults.set(sprache.rawValue, forKey: Key.sprache) }
    }

    /// Ob die Bedienungshilfen-Freigabe erteilt ist. Ohne sie kann kein
    /// Event-Tap erstellt werden.
    public var hasAccessibility = false

    /// Fehlermeldung, wenn der Event-Tap nicht angelegt werden konnte.
    /// Wird im Popover angezeigt statt in einem modalen Dialog: Eine App
    /// ohne Dock-Icon bringt modale Dialoge nicht nach vorn, der Main Thread
    /// bliebe in der modalen Schleife stecken und die App wirkte eingefroren.
    public var tapError: String?

    public init(defaults: UserDefaults = .standard) {
        self.defaults = defaults

        // Zuweisungen im init lösen didSet nicht aus — genau richtig, sonst
        // würde jeder Start den gerade gelesenen Wert neu schreiben.
        if let data = defaults.data(forKey: Key.hotkey),
           let gespeichert = try? JSONDecoder().decode(Hotkey.self, from: data) {
            hotkey = gespeichert
        }
        let shift = defaults.integer(forKey: Key.octaveShift)
        if Keyboard.shiftRange.contains(shift) {
            keyboard.shift(by: shift)
        }
        pianoModeEnabled = defaults.bool(forKey: Key.pianoModeEnabled)
        advancedLayoutEnabled = defaults.bool(forKey: Key.advancedLayoutEnabled)
        cUndRFrei = defaults.bool(forKey: Key.cUndRFrei)
        if let gespeichert = defaults.string(forKey: Key.sprache).flatMap(Sprache.init(rawValue:)) {
            sprache = gespeichert
        }
        hasAccessibility = AXIsProcessTrusted()
    }

    /// Aktuell gültiger Grundton-Schiebebereich, abhängig vom Layout. Der
    /// breitere Adv.-Tonumfang lässt weniger Spielraum nach oben, bevor eine
    /// Note über MIDI 127 rutschen würde — `Keyboard.shift` selbst kennt das
    /// engere Limit nicht, darum wird hier zusätzlich vorab geprüft.
    private var geltenderSchiebebereich: ClosedRange<Int> {
        advancedLayoutEnabled ? AdvancedKeyMap.shiftRange : Keyboard.shiftRange
    }

    public func shiftOctave(_ delta: Int) {
        guard geltenderSchiebebereich.contains(keyboard.octaveShift + delta) else { return }
        guard keyboard.shift(by: delta) else { return }
        defaults.set(keyboard.octaveShift, forKey: Key.octaveShift)
    }

    public func refreshAccessibility() {
        // Nur bei echter Änderung schreiben: Jede Zuweisung — auch mit
        // gleichem Wert — lässt beobachtende Views neu rechnen, und der
        // Freigabe-Timer fragt alle 1,5 s ab.
        let erteilt = AXIsProcessTrusted()
        if hasAccessibility != erteilt { hasAccessibility = erteilt }
    }

    /// Name des aktuell gehaltenen Dreiklangs (z. B. "C maj"), oder `nil`,
    /// wenn die gehaltenen Tasten keinen bilden. Leitet sich aus
    /// `pressedKeycodes` ab, das ohnehin schon für das Tastatur-Highlighting
    /// gepflegt wird — keine zusätzliche Verkabelung nötig.
    public var chordName: String? {
        let aktion: (UInt16) -> KeyAction?
        if advancedLayoutEnabled {
            aktion = AdvancedKeyMap.action(for:)
        } else {
            aktion = KeyMap.action(for:)
        }
        let noten = pressedKeycodes.compactMap { keycode -> UInt8? in
            guard case let .note(semitone) = aktion(keycode) else { return nil }
            return keyboard.note(forSemitone: semitone)
        }
        guard let tiefsterTon = noten.min() else { return nil }
        let pitchClasses = Set(noten.map { Int($0) % 12 })
        return ChordNamer.name(
            forPitchClasses: pitchClasses, bass: Int(tiefsterTon) % 12, sprache: sprache
        )
    }
}

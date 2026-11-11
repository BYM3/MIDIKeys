import Foundation

/// Übersetzt Tastenereignisse in Zustandsänderungen und MIDI-Nachrichten.
///
/// Bewusst getrennt vom eigentlichen Event-Tap: So ist das Zusammenspiel von
/// Tasten, Oktavwechsel und Notenverwaltung ohne Systemberechtigungen testbar.
@MainActor
public final class InputController {
    private let state: AppState
    private let engine: MIDIEngine
    private let uhr: () -> TimeInterval
    private var letztesEsc: TimeInterval?

    /// Zwei Esc innerhalb dieses Fensters schalten den MIDI-Modus aus —
    /// etwa so lang wie Apples Standard-Doppelklick-Intervall.
    static let doppelEscFenster: TimeInterval = 0.5
    private static let escKeycode: UInt16 = 0x35
    /// kVK_ANSI_C — im Grundlayout keine Note.
    static let cKeycode: UInt16 = 0x08
    /// kVK_ANSI_R — im Grundlayout keine Note.
    static let rKeycode: UInt16 = 0x0F
    /// Return, Enter (Ziffernblock), Rückschritt (⌫), Entf (⌦) — in keinem
    /// der beiden Layouts eine Note.
    static let enterUndLoeschen: Set<UInt16> = [0x24, 0x4C, 0x33, 0x75]

    public init(
        state: AppState, engine: MIDIEngine,
        uhr: @escaping () -> TimeInterval = { ProcessInfo.processInfo.systemUptime }
    ) {
        self.state = state
        self.engine = engine
        self.uhr = uhr
    }

    /// Verarbeitet ein Tastenereignis. Gibt zurück, ob es geschluckt werden soll.
    @discardableResult
    public func handle(
        keycode: UInt16, flags: UInt64, isKeyDown: Bool, isRepeat: Bool
    ) -> Bool {
        if istDoppelEsc(keycode: keycode, flags: flags, isKeyDown: isKeyDown, isRepeat: isRepeat) {
            setActive(false)
            return true
        }

        let aktion: (UInt16) -> KeyAction?
        if state.advancedLayoutEnabled {
            aktion = AdvancedKeyMap.action(for:)
        } else {
            aktion = KeyMap.action(for:)
        }
        let decision = TapPolicy.decide(
            keycode: keycode,
            flags: flags,
            isKeyDown: isKeyDown,
            isRepeat: isRepeat,
            isActive: state.isActive,
            hotkey: state.hotkey,
            keyAction: aktion,
            extraPassThrough: zusaetzlichDurchgelassen
        )

        merkeGedrueckteTaste(keycode: keycode, decision: decision, isKeyDown: isKeyDown)

        switch decision {
        case .passThrough:
            return false
        case .swallow:
            return true
        case let .noteOn(semitone):
            engine.noteOn(semitone: semitone, note: state.keyboard.note(forSemitone: semitone))
            return true
        case let .noteOff(semitone):
            // Die Engine kennt die ursprünglich gesendete Note — nicht neu berechnen,
            // sonst hängt der Ton nach einem Oktavwechsel.
            engine.noteOff(semitone: semitone)
            return true
        case let .octave(delta):
            state.shiftOctave(delta)
            return true
        case let .sustain(on):
            engine.sustain(on)
            return true
        case .toggle:
            setActive(!state.isActive)
            return true
        }
    }

    /// `c` und `r` werden zu normalen Tasten, wenn sie in den Einstellungen
    /// freigegeben sind (`AppState.cUndRFrei`, Nutzerwunsch für Ableton) —
    /// aber nur mit MIDI-Ausgang im Grundlayout: Im Adv.-Layout sind sie die
    /// Noten F3 und H4, im Klavier-Modus gibt es keine DAW zu bedienen.
    ///
    /// Return/Enter und die Löschtasten gehen ebenfalls nur mit MIDI-Ausgang
    /// durch (Nutzerwunsch), dort aber in beiden Layouts.
    private var zusaetzlichDurchgelassen: Set<UInt16> {
        if state.pianoModeEnabled { return [] }
        var tasten = Self.enterUndLoeschen
        if !state.advancedLayoutEnabled && state.cUndRFrei {
            tasten.formUnion([Self.cKeycode, Self.rKeycode])
        }
        return tasten
    }

    /// Notausstieg: zweimal Esc schnell hintereinander gibt die Tastatur
    /// zurück — unabhängig vom eingestellten Hotkey. Wirkt nur im aktiven
    /// Modus und nur in diese Richtung: Esc wird in anderen Programmen
    /// ständig gedrückt und darf die Tastatur nie unbemerkt übernehmen.
    /// Zählt nur ohne Modifier (⌘⌥Esc bleibt „Sofort beenden“) und nur,
    /// wenn keine andere Taste dazwischen gedrückt wurde.
    private func istDoppelEsc(keycode: UInt16, flags: UInt64, isKeyDown: Bool, isRepeat: Bool) -> Bool {
        guard isKeyDown, !isRepeat else { return false }
        guard state.isActive,
              keycode == Self.escKeycode,
              flags & Hotkey.relevantMask == 0 else {
            letztesEsc = nil
            return false
        }
        let jetzt = uhr()
        if let letztesEsc, jetzt - letztesEsc <= Self.doppelEscFenster {
            self.letztesEsc = nil
            return true
        }
        letztesEsc = jetzt
        return false
    }

    /// Pflegt die Anzeige der gedrückten Tasten.
    ///
    /// Richtet sich nach der Entscheidung, nicht nach dem rohen Keycode:
    /// Was durchgelassen wird (etwa eine ⌘-Kombination) leuchtet nicht,
    /// und Key-Repeat ändert nichts.
    private func merkeGedrueckteTaste(keycode: UInt16, decision: Decision, isKeyDown: Bool) {
        switch decision {
        case .noteOn, .noteOff, .octave, .sustain:
            if isKeyDown {
                state.pressedKeycodes.insert(keycode)
            } else {
                state.pressedKeycodes.remove(keycode)
            }
        case .swallow:
            // Deckt das Loslassen der Oktavtasten ab, das als .swallow kommt.
            if !isKeyDown { state.pressedKeycodes.remove(keycode) }
        case .passThrough, .toggle:
            break
        }
    }

    public func setActive(_ active: Bool) {
        guard state.isActive != active else { return }
        if !active {
            // Nie mit klingenden Tönen aussteigen.
            engine.panic()
            // Für gehaltene Tasten kommt nach dem Umschalten kein keyUp mehr —
            // sie blieben sonst für immer erleuchtet.
            state.pressedKeycodes.removeAll()
        }
        state.isActive = active
    }
}

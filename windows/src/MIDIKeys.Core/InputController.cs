namespace MIDIKeys.Core;

/// <summary>
/// Übersetzt Tastenereignisse in Zustandsänderungen und MIDI. Getrennt vom
/// eigentlichen Hook, damit das Zusammenspiel ohne Systemzugriff testbar ist.
/// </summary>
public sealed class InputController
{
    private readonly AppState _state;
    private readonly MidiEngine _engine;
    private readonly Func<double> _uhr;
    private double? _letztesEsc;

    /// <summary>Zwei Esc innerhalb dieses Fensters schalten den MIDI-Modus aus.</summary>
    public const double DoppelEscFenster = 0.5;

    /// <summary>Return, Enter (Ziffernblock), Rücktaste, Entf — in keinem Layout eine Note.</summary>
    internal static readonly HashSet<ushort> EnterUndLoeschen =
        [ScanCodes.Enter, ScanCodes.NumpadEnter, ScanCodes.Backspace, ScanCodes.Delete];

    public InputController(AppState state, MidiEngine engine, Func<double>? uhr = null)
    {
        _state = state;
        _engine = engine;
        _uhr = uhr ?? (() => Environment.TickCount64 / 1000.0);
    }

    /// <summary>Verarbeitet ein Tastenereignis. Gibt zurück, ob es geschluckt werden soll.</summary>
    public bool Handle(ushort key, Mods mods, bool isKeyDown, bool isRepeat)
    {
        if (IstDoppelEsc(key, mods, isKeyDown, isRepeat))
        {
            SetActive(false);
            return true;
        }

        var decision = TapPolicy.Decide(
            key, mods, isKeyDown, isRepeat, _state.IsActive, _state.Hotkey,
            _state.CurrentKeyAction, ZusaetzlichDurchgelassen);

        MerkeGedrueckteTaste(key, decision, isKeyDown);

        switch (decision.Kind)
        {
            case DecisionKind.PassThrough:
                return false;
            case DecisionKind.Swallow:
                return true;
            case DecisionKind.NoteOn:
                _engine.NoteOn(decision.Value, _state.Keyboard.Note(decision.Value));
                return true;
            case DecisionKind.NoteOff:
                // Die Engine kennt die gesendete Note — nicht neu berechnen.
                _engine.NoteOff(decision.Value);
                return true;
            case DecisionKind.Octave:
                _state.ShiftOctave(decision.Value);
                return true;
            case DecisionKind.Sustain:
                _engine.Sustain(decision.Value != 0);
                return true;
            case DecisionKind.Toggle:
                SetActive(!_state.IsActive);
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// c und r gehen durch, wenn freigegeben — nur mit MIDI-Ausgang im
    /// Grundlayout. Return/Enter und Löschtasten gehen mit MIDI-Ausgang immer durch.
    /// </summary>
    private HashSet<ushort> ZusaetzlichDurchgelassen
    {
        get
        {
            if (_state.PianoModeEnabled) return [];
            var tasten = new HashSet<ushort>(EnterUndLoeschen);
            if (!_state.AdvancedLayoutEnabled && _state.CUndRFrei)
            {
                tasten.Add(ScanCodes.C);
                tasten.Add(ScanCodes.R);
            }
            return tasten;
        }
    }

    /// <summary>
    /// Notausstieg: zweimal Esc schnell hintereinander, ohne Modifier, ohne
    /// andere Taste dazwischen. Wirkt nur im aktiven Modus.
    /// </summary>
    private bool IstDoppelEsc(ushort key, Mods mods, bool isKeyDown, bool isRepeat)
    {
        if (!isKeyDown || isRepeat) return false;
        if (TapPolicy.ModifierKeys.Contains(key)) return false;
        if (!_state.IsActive || key != ScanCodes.Escape || mods != Mods.None)
        {
            _letztesEsc = null;
            return false;
        }
        var jetzt = _uhr();
        if (_letztesEsc is { } vorher && jetzt - vorher <= DoppelEscFenster)
        {
            _letztesEsc = null;
            return true;
        }
        _letztesEsc = jetzt;
        return false;
    }

    private void MerkeGedrueckteTaste(ushort key, Decision decision, bool isKeyDown)
    {
        switch (decision.Kind)
        {
            case DecisionKind.NoteOn or DecisionKind.NoteOff or DecisionKind.Octave or DecisionKind.Sustain:
                if (isKeyDown ? _state.PressedKeys.Add(key) : _state.PressedKeys.Remove(key))
                    _state.NotifyChanged();
                break;
            case DecisionKind.Swallow:
                if (!isKeyDown && _state.PressedKeys.Remove(key)) _state.NotifyChanged();
                break;
        }
    }

    public void SetActive(bool active)
    {
        if (_state.IsActive == active) return;
        if (!active)
        {
            _engine.Panic();
            _state.PressedKeys.Clear();
        }
        _state.IsActive = active;
    }
}

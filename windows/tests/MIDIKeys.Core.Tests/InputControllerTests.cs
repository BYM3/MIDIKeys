using static MIDIKeys.Core.ScanCodes;

namespace MIDIKeys.Core.Tests;

public class InputControllerTests
{
    private static (InputController, AppState, SpyTransport) Aufbau()
    {
        var state = new AppState(new MemorySettingsStore());
        var spy = new SpyTransport();
        var controller = new InputController(state, new MidiEngine(spy));
        return (controller, state, spy);
    }

    [Fact]
    public void spielenErzeugtNotenUndSchlucktDieTaste()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        var geschluckt = controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.True(geschluckt);
        Assert.Equal([MidiMessage.NoteOn(48, 100)], spy.Sent);
    }

    [Fact]
    public void imInaktivenModusPassiertNichts()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = false;
        var geschluckt = controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.False(geschluckt);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void winKombinationWirdNichtGeschluckt()
    {
        // Mac: ⌘⌥Esc. Windows: Win+Alt+Esc.
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        var geschluckt = controller.Handle(Escape, Mods.Win | Mods.Alt, isKeyDown: true, isRepeat: false);
        Assert.False(geschluckt);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void strgUmschaltEscWirdNichtGeschluckt()
    {
        // Windows: Strg+Umschalt+Esc öffnet den Task-Manager — die Lebensversicherung.
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        Assert.False(controller.Handle(Escape, Mods.Ctrl | Mods.Shift, isKeyDown: true, isRepeat: false));
        Assert.False(controller.Handle(Escape, Mods.Ctrl | Mods.Shift, isKeyDown: false, isRepeat: false));
        Assert.Empty(spy.Sent);
        Assert.True(state.IsActive);
    }

    [Fact]
    public void modifierTastenSelbstGehenImmerDurch()
    {
        // Windows: Der Hook sieht die Modifier als eigene Tasten — sie dürfen
        // nie geschluckt werden, sonst hinge Strg/Alt/Win im System fest.
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        foreach (var taste in TapPolicy.ModifierKeys)
        {
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: true));
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: false, isRepeat: false));
        }
        Assert.Empty(spy.Sent);
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void oktavwechselWaehrendEinTonKlingtLaesstIhnNichtHaengen()
    {
        // Der kritische Fall aus der Spec: 'a' halten, Oktave hoch, 'a' loslassen.
        // Das Note-Off muss an Note 48 gehen, nicht an 60.
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(X, Mods.None, isKeyDown: true, isRepeat: false);  // x
        controller.Handle(A, Mods.None, isKeyDown: false, isRepeat: false);

        Assert.Equal(1, state.Keyboard.OctaveShift);
        Assert.Equal([MidiMessage.NoteOn(48, 100), MidiMessage.NoteOff(48)], spy.Sent);
    }

    [Fact]
    public void nachOktavwechselSpielenNeueTastenDieNeueLage()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        controller.Handle(X, Mods.None, isKeyDown: true, isRepeat: false);  // x
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);  // a
        Assert.Equal([MidiMessage.NoteOn(60, 100)], spy.Sent);
    }

    [Fact]
    public void tabSchaltetSustainUeberDenController()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        controller.Handle(Tab, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(Tab, Mods.None, isKeyDown: false, isRepeat: false);
        Assert.Equal([MidiMessage.ControlChange(64, 127), MidiMessage.ControlChange(64, 0)], spy.Sent);
    }

    [Fact]
    public void derHotkeySchaltetUm()
    {
        var (controller, state, _) = Aufbau();
        controller.Handle(Hotkey.Default.Key, Hotkey.Default.Modifiers, isKeyDown: true, isRepeat: false);
        Assert.True(state.IsActive);
        controller.Handle(Hotkey.Default.Key, Hotkey.Default.Modifiers, isKeyDown: true, isRepeat: false);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void deaktivierenBeendetAlleKlingendenToene()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(D, Mods.None, isKeyDown: true, isRepeat: false);
        spy.Sent.Clear();

        controller.SetActive(false);

        Assert.Contains(MidiMessage.NoteOff(48), spy.Sent);
        Assert.Contains(MidiMessage.NoteOff(52), spy.Sent);
        Assert.Equal(MidiMessage.ControlChange(123, 0), spy.Sent[^1]);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void advLayoutWirdVomControllerRespektiert()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        state.AdvancedLayoutEnabled = true;
        // <=C3(0) im Adv.-Layout. Im Grundlayout ist diese Taste gar nicht gemappt.
        controller.Handle(IsoBackslash, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Equal([MidiMessage.NoteOn(48, 100)], spy.Sent);
    }

    [Fact]
    public void advOktaveRespektiertDenEngerenSchiebebereichEndeZuEnde()
    {
        var (controller, state, _) = Aufbau();
        state.IsActive = true;
        state.AdvancedLayoutEnabled = true;
        for (var i = 0; i < 5; i++)
            controller.Handle(AdvancedKeyMap.OctaveUpKey, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Equal(3, state.Keyboard.OctaveShift);
    }

    // MARK: - Doppel-Esc als Notausstieg

    private sealed class Uhr
    {
        public double Jetzt = 100.0;
    }

    private static (InputController, AppState, SpyTransport, Uhr) AufbauMitUhr()
    {
        var state = new AppState(new MemorySettingsStore());
        var spy = new SpyTransport();
        var uhr = new Uhr();
        var controller = new InputController(state, new MidiEngine(spy), () => uhr.Jetzt);
        return (controller, state, spy, uhr);
    }

    private static void Druecke(InputController controller, ushort scancode, Mods mods = Mods.None)
    {
        controller.Handle(scancode, mods, isKeyDown: true, isRepeat: false);
        controller.Handle(scancode, mods, isKeyDown: false, isRepeat: false);
    }

    [Fact]
    public void doppelEscSchaltetDenModusAus()
    {
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Druecke(controller, Escape);
        Assert.True(state.IsActive);  // ein einzelnes Esc tut nichts
        uhr.Jetzt = 100.4;
        Druecke(controller, Escape);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void doppelEscStopptKlingendeToene()
    {
        var (controller, state, spy, uhr) = AufbauMitUhr();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);  // a halten
        Druecke(controller, Escape);
        uhr.Jetzt = 100.2;
        Druecke(controller, Escape);
        Assert.Contains(MidiMessage.NoteOff(48), spy.Sent);
    }

    [Fact]
    public void zuLangsamesDoppelEscZaehltNicht()
    {
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Druecke(controller, Escape);
        uhr.Jetzt = 100.6;
        Druecke(controller, Escape);
        Assert.True(state.IsActive);
        // Das zweite Esc startet aber ein neues Fenster.
        uhr.Jetzt = 100.9;
        Druecke(controller, Escape);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void andereTasteDazwischenBrichtDoppelEscAb()
    {
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Druecke(controller, Escape);
        uhr.Jetzt = 100.1;
        Druecke(controller, A);  // a
        uhr.Jetzt = 100.2;
        Druecke(controller, Escape);
        Assert.True(state.IsActive);
    }

    [Fact]
    public void doppelEscSchaltetNichtEin()
    {
        // Esc wird in anderen Programmen ständig gedrückt — das darf die
        // Tastatur nie unbemerkt übernehmen.
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = false;
        Druecke(controller, Escape);
        uhr.Jetzt = 100.2;
        var geschluckt = controller.Handle(Escape, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.False(state.IsActive);
        Assert.False(geschluckt);
    }

    [Fact]
    public void escMitModifierZaehltNichtAlsDoppelEsc()
    {
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Druecke(controller, Escape, Mods.Shift);
        uhr.Jetzt = 100.2;
        Druecke(controller, Escape, Mods.Shift);
        Assert.True(state.IsActive);
    }

    [Fact]
    public void modifierTasteZwischenDenEscsBrichtDoppelEscNichtAb()
    {
        // Windows: Modifier kommen als eigene Tastenereignisse an. Ein bloßes
        // Antippen von Umschalt zwischen den beiden Esc zählt nicht als "andere Taste".
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Druecke(controller, Escape);
        uhr.Jetzt = 100.1;
        Druecke(controller, LeftShift);
        uhr.Jetzt = 100.2;
        Druecke(controller, Escape);
        Assert.False(state.IsActive);
    }

    [Fact]
    public void altEscGehtDurchUndZaehltNichtAlsDoppelEsc()
    {
        // Windows: Alt+Esc wechselt das Fenster und muss immer durchgehen.
        var (controller, state, _, uhr) = AufbauMitUhr();
        state.IsActive = true;
        Assert.False(controller.Handle(Escape, Mods.Alt, isKeyDown: true, isRepeat: false));
        uhr.Jetzt = 100.2;
        Assert.False(controller.Handle(Escape, Mods.Alt, isKeyDown: true, isRepeat: false));
        Assert.True(state.IsActive);
    }

    [Fact]
    public void cUndRGehenNurMitHakenDurch()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        ushort[] cUndR = [C, R];

        // Ohne Haken: beide gesperrt.
        foreach (var taste in cUndR)
        {
            Assert.True(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
            Assert.True(controller.Handle(taste, Mods.None, isKeyDown: false, isRepeat: false));
        }

        // Mit Haken: beide normale Tasten, auch gehalten.
        state.CUndRFrei = true;
        foreach (var taste in cUndR)
        {
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: true));
            Assert.False(controller.Handle(taste, Mods.None, isKeyDown: false, isRepeat: false));
        }
        Assert.Empty(spy.Sent);
        Assert.Empty(state.PressedKeys);

        // Klavier-Modus: trotz Haken gesperrt.
        state.PianoModeEnabled = true;
        foreach (var taste in cUndR)
            Assert.True(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
        Assert.Empty(spy.Sent);
        state.PianoModeEnabled = false;

        // Adv.-Layout: trotz Haken Noten — c ist F3, r ist H4.
        state.AdvancedLayoutEnabled = true;
        Assert.True(controller.Handle(C, Mods.None, isKeyDown: true, isRepeat: false));
        Assert.True(controller.Handle(R, Mods.None, isKeyDown: true, isRepeat: false));
        Assert.Equal([MidiMessage.NoteOn(53, 100), MidiMessage.NoteOn(71, 100)], spy.Sent);
    }

    [Fact]
    public void cUndRFreigabeWirdGemerkt()
    {
        var store = new RundlaufStore();
        Assert.False(new AppState(store).CUndRFrei);
        new AppState(store).CUndRFrei = true;
        Assert.True(new AppState(store).CUndRFrei);
    }

    [Fact]
    public void enterUndLoeschenGehenNurMitMidiAusgangDurch()
    {
        var (controller, state, spy) = Aufbau();
        state.IsActive = true;
        ushort[] tasten = [Enter, NumpadEnter, Backspace, Delete];  // Return, Enter, ⌫, Entf

        foreach (var adv in new[] { false, true })
        {
            state.AdvancedLayoutEnabled = adv;
            foreach (var taste in tasten)
            {
                Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
                // Gehaltenes ⌫ soll weiterlöschen.
                Assert.False(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: true));
                Assert.False(controller.Handle(taste, Mods.None, isKeyDown: false, isRepeat: false));
            }
        }

        // Klavier-Modus: geschluckt.
        state.PianoModeEnabled = true;
        foreach (var taste in tasten)
            Assert.True(controller.Handle(taste, Mods.None, isKeyDown: true, isRepeat: false));
        Assert.Empty(spy.Sent);
    }
}

using static MIDIKeys.Core.ScanCodes;

namespace MIDIKeys.Core.Tests;

public class PressedKeysTests
{
    private static (InputController, AppState) Aufbau()
    {
        var state = new AppState(new MemorySettingsStore());
        var controller = new InputController(state, new MidiEngine(new NullSender()));
        return (controller, state);
    }

    [Fact]
    public void gedrueckteNotentasteWirdVermerktUndBeimLoslassenEntfernt()
    {
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Equal([A], state.PressedKeys);
        controller.Handle(A, Mods.None, isKeyDown: false, isRepeat: false);
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void mehrereTastenGleichzeitig()
    {
        var (controller, state) = Aufbau();
        state.IsActive = true;
        foreach (var scancode in new[] { A, D, G })
            controller.Handle(scancode, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.True(state.PressedKeys.SetEquals([A, D, G]));
        controller.Handle(D, Mods.None, isKeyDown: false, isRepeat: false);
        Assert.True(state.PressedKeys.SetEquals([A, G]));
    }

    [Fact]
    public void sustainUndOktavtastenLeuchtenEbenfalls()
    {
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(KeyMap.SustainKey, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(KeyMap.OctaveUpKey, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Contains(KeyMap.SustainKey, state.PressedKeys);
        Assert.Contains(KeyMap.OctaveUpKey, state.PressedKeys);

        controller.Handle(KeyMap.OctaveUpKey, Mods.None, isKeyDown: false, isRepeat: false);
        Assert.DoesNotContain(KeyMap.OctaveUpKey, state.PressedKeys);
    }

    [Fact]
    public void imInaktivenModusWirdNichtsVermerkt()
    {
        var (controller, state) = Aufbau();
        state.IsActive = false;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void winUndStrgKombinationenLeuchtenNicht()
    {
        // Mac: Cmd+A. Windows: Win+A und Strg+A gehen durch und spielen nichts —
        // dürfen also auch nicht leuchten.
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.Win, isKeyDown: true, isRepeat: false);
        controller.Handle(A, Mods.Ctrl, isKeyDown: true, isRepeat: false);
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void nichtGemappteTastenLeuchtenNicht()
    {
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(Space, Mods.None, isKeyDown: true, isRepeat: false);  // Leertaste
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void keyRepeatErzeugtKeineDubletten()
    {
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: true);
        Assert.Equal([A], state.PressedKeys);
    }

    [Fact]
    public void deaktivierenLoeschtAlleLeuchtendenTasten()
    {
        // Sonst bliebe eine Taste für immer erleuchtet: Nach dem Umschalten
        // kommt für sie kein keyUp mehr an.
        var (controller, state) = Aufbau();
        state.IsActive = true;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        controller.Handle(D, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Equal(2, state.PressedKeys.Count);

        controller.SetActive(false);
        Assert.Empty(state.PressedKeys);
    }

    [Fact]
    public void aenderungDerGedruecktenTastenWirdGemeldet()
    {
        // Windows: Die Oberfläche hängt am Changed-Ereignis statt an @Observable.
        var (controller, state) = Aufbau();
        state.IsActive = true;
        var meldungen = 0;
        state.Changed += () => meldungen++;
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: false);
        Assert.Equal(1, meldungen);
        controller.Handle(A, Mods.None, isKeyDown: true, isRepeat: true);
        Assert.Equal(1, meldungen);  // Key-Repeat ändert nichts
        controller.Handle(A, Mods.None, isKeyDown: false, isRepeat: false);
        Assert.Equal(2, meldungen);
    }
}

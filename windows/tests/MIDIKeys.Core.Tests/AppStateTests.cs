namespace MIDIKeys.Core.Tests;

public class AppStateTests
{
    [Fact]
    public void startetImmerInaktiv()
    {
        // Nie den Aktiv-Zustand persistieren: Ein Neustart darf nie in eine
        // tote Tastatur führen.
        var store = new RundlaufStore();
        var ersterStart = new AppState(store);
        ersterStart.IsActive = true;

        var zweiterStart = new AppState(store);
        Assert.False(zweiterStart.IsActive);
    }

    [Fact]
    public void oktavlageUeberlebtDenNeustart()
    {
        var store = new RundlaufStore();
        var ersterStart = new AppState(store);
        ersterStart.ShiftOctave(2);
        Assert.Equal(2, ersterStart.Keyboard.OctaveShift);

        var zweiterStart = new AppState(store);
        Assert.Equal(2, zweiterStart.Keyboard.OctaveShift);
    }

    [Fact]
    public void shortcutUeberlebtDenNeustart()
    {
        var store = new RundlaufStore();
        var neuer = new Hotkey(ScanCodes.Space, Mods.Win | Mods.Shift);
        var ersterStart = new AppState(store);
        ersterStart.Hotkey = neuer;

        var zweiterStart = new AppState(store);
        Assert.Equal(neuer, zweiterStart.Hotkey);
    }

    [Fact]
    public void ohneGespeichertenShortcutGiltDerStandard()
    {
        var state = new AppState(new MemorySettingsStore());
        Assert.Equal(Hotkey.Default, state.Hotkey);
    }

    [Fact]
    public void oktavShiftWirdGeklemmt()
    {
        var state = new AppState(new MemorySettingsStore());
        state.ShiftOctave(99);
        Assert.Equal(0, state.Keyboard.OctaveShift);  // abgelehnt, nicht geklemmt-verschoben
        state.ShiftOctave(5);
        Assert.Equal(5, state.Keyboard.OctaveShift);
    }

    [Fact]
    public void beschaedigteGespeicherteDatenFuehrenZumStandard()
    {
        // Windows: Ein gespeicherter Shortcut ohne tauglichen Modifier wird
        // verworfen; eine ungültige Oktavlage ebenso.
        var store = new MemorySettingsStore(new Settings
        {
            HotkeyKey = ScanCodes.A, HotkeyModifiers = (int)Mods.Shift, OctaveShift = 42, Sprache = "xx",
        });
        var state = new AppState(store);
        Assert.Equal(Hotkey.Default, state.Hotkey);
        Assert.Equal(0, state.Keyboard.OctaveShift);
    }

    [Fact]
    public void kaputteEinstellungsdateiFuehrtZumStandard()
    {
        // Gegenstück zu den kaputten UserDefaults-Daten: eine zerstörte JSON-Datei.
        var pfad = Path.Combine(Path.GetTempPath(), $"midikeys-tests-{Guid.NewGuid()}", "settings.json");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(pfad)!);
            File.WriteAllBytes(pfad, [0xDE, 0xAD]);
            var state = new AppState(new JsonSettingsStore(pfad));
            Assert.Equal(Hotkey.Default, state.Hotkey);
            Assert.Equal(0, state.Keyboard.OctaveShift);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(pfad)!, recursive: true);
        }
    }

    [Fact]
    public void einstellungsdateiUeberlebtDenNeustart()
    {
        var verzeichnis = Path.Combine(Path.GetTempPath(), $"midikeys-tests-{Guid.NewGuid()}");
        var pfad = Path.Combine(verzeichnis, "settings.json");
        try
        {
            var neuer = new Hotkey(ScanCodes.M, Mods.Ctrl | Mods.Alt);
            var ersterStart = new AppState(new JsonSettingsStore(pfad));
            ersterStart.Hotkey = neuer;
            ersterStart.ShiftOctave(-2);
            ersterStart.PianoModeEnabled = true;
            ersterStart.AdvancedLayoutEnabled = true;
            ersterStart.CUndRFrei = true;
            ersterStart.Sprache = Sprache.English;
            ersterStart.OutputDevice = "loopMIDI Port";

            var zweiterStart = new AppState(new JsonSettingsStore(pfad));
            Assert.Equal(neuer, zweiterStart.Hotkey);
            Assert.Equal(-2, zweiterStart.Keyboard.OctaveShift);
            Assert.True(zweiterStart.PianoModeEnabled);
            Assert.True(zweiterStart.AdvancedLayoutEnabled);
            Assert.True(zweiterStart.CUndRFrei);
            Assert.Equal(Sprache.English, zweiterStart.Sprache);
            Assert.Equal("loopMIDI Port", zweiterStart.OutputDevice);
        }
        finally
        {
            if (Directory.Exists(verzeichnis)) Directory.Delete(verzeichnis, recursive: true);
        }
    }

    [Fact]
    public void unveraenderterAktivZustandLoestKeineNeuzeichnungAus()
    {
        // Windows-Gegenstück zum Freigabe-Test des Macs (dort: Bedienungshilfen):
        // Jede Zuweisung mit gleichem Wert würde die Oberfläche neu zeichnen lassen.
        var state = new AppState(new MemorySettingsStore());
        var meldungen = 0;
        state.Changed += () => meldungen++;
        state.IsActive = false;
        Assert.Equal(0, meldungen);
        state.IsActive = true;
        Assert.Equal(1, meldungen);
    }

    [Fact]
    public void abgelehnterOktavwechselSpeichertNichts()
    {
        var store = new RundlaufStore();
        var state = new AppState(store);
        state.ShiftOctave(99);
        Assert.Equal(0, store.Speicherungen);
    }
}

public class AppStateKlemmTests
{
    [Fact]
    public void advLayoutKlemmtEineZuHoheLage()
    {
        var state = new MIDIKeys.Core.AppState(new MIDIKeys.Core.MemorySettingsStore());
        state.ShiftOctave(5);
        Assert.Equal(5, state.Keyboard.OctaveShift);
        state.AdvancedLayoutEnabled = true;
        Assert.Equal(MIDIKeys.Core.AdvancedKeyMap.MaxShift, state.Keyboard.OctaveShift);
        Assert.True(state.Keyboard.Note(MIDIKeys.Core.AdvancedKeyMap.MaxSemitone) <= 127);
    }

    [Fact]
    public void gespeicherteZuHoheLageWirdBeimStartGeklemmt()
    {
        var store = new MIDIKeys.Core.MemorySettingsStore(new MIDIKeys.Core.Settings { OctaveShift = 5, AdvancedLayoutEnabled = true });
        var state = new MIDIKeys.Core.AppState(store);
        Assert.Equal(MIDIKeys.Core.AdvancedKeyMap.MaxShift, state.Keyboard.OctaveShift);
    }

    [Fact]
    public void unbekannteModifierBitsWerdenVerworfen()
    {
        var store = new MIDIKeys.Core.MemorySettingsStore(new MIDIKeys.Core.Settings { HotkeyModifiers = (int)MIDIKeys.Core.Mods.Alt | 0x100 });
        var state = new MIDIKeys.Core.AppState(store);
        Assert.Equal(MIDIKeys.Core.Mods.Alt, state.Hotkey.Modifiers);
    }
}

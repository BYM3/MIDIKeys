namespace MIDIKeys.Core.Tests;

public class AppStatePianoModeTests
{
    [Fact]
    public void klavierTonStartetAusOhneGespeichertenWert()
    {
        var state = new AppState(new MemorySettingsStore());
        Assert.False(state.PianoModeEnabled);
    }

    [Fact]
    public void klavierTonUeberlebtDenNeustart()
    {
        // Anders als IsActive: bewusst gemerkt, wie Oktavlage und Shortcut.
        var store = new RundlaufStore();
        var ersterStart = new AppState(store);
        ersterStart.PianoModeEnabled = true;

        var zweiterStart = new AppState(store);
        Assert.True(zweiterStart.PianoModeEnabled);
    }
}

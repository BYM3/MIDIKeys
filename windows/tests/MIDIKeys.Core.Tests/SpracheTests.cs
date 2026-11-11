namespace MIDIKeys.Core.Tests;

public class SpracheTests
{
    [Fact]
    public void spracheFolgtOhneGespeichertenWertDemSystem()
    {
        // Windows: eine UI-Kultur statt einer Liste bevorzugter Sprachen.
        Assert.Equal(Sprache.Deutsch, SpracheExtensions.AusSystem("de-DE"));
        Assert.Equal(Sprache.Deutsch, SpracheExtensions.AusSystem("de-AT"));
        Assert.Equal(Sprache.English, SpracheExtensions.AusSystem("en-GB"));
        // Alles, was nicht Deutsch ist, bekommt Englisch.
        Assert.Equal(Sprache.English, SpracheExtensions.AusSystem("fr-FR"));
        Assert.Equal(Sprache.English, SpracheExtensions.AusSystem(""));  // invariante Kultur
    }

    [Fact]
    public void spracheUeberlebtDenNeustart()
    {
        var store = new RundlaufStore();
        var ersterStart = new AppState(store);
        ersterStart.Sprache = Sprache.English;
        Assert.Equal(Sprache.English, new AppState(store).Sprache);
        ersterStart.Sprache = Sprache.Deutsch;
        Assert.Equal(Sprache.Deutsch, new AppState(store).Sprache);
    }

    [Fact]
    public void spracheKodiertSichAlsKuerzel()
    {
        // Wie die rawValues "de"/"en" der Mac-App.
        Assert.Equal("de", Sprache.Deutsch.Code());
        Assert.Equal("en", Sprache.English.Code());
        Assert.Equal(Sprache.Deutsch, SpracheExtensions.FromCode("de"));
        Assert.Equal(Sprache.English, SpracheExtensions.FromCode("en"));
        Assert.Null(SpracheExtensions.FromCode("fr"));
        Assert.Null(SpracheExtensions.FromCode(null));
        Assert.Equal("Deutsch", Sprache.Deutsch.Name());
        Assert.Equal("English", Sprache.English.Name());
    }

    [Fact]
    public void textWaehltDieSprache()
    {
        Assert.Equal("Beenden", Sprache.Deutsch.Text("Beenden", "Quit"));
        Assert.Equal("Quit", Sprache.English.Text("Beenden", "Quit"));
    }

    [Fact]
    public void englischeNotennamenNutzenB()
    {
        Assert.Equal("B3", Notes.Name(59, Sprache.English));
        Assert.Equal("H3", Notes.Name(59, Sprache.Deutsch));
        Assert.Equal("C#3", Notes.Name(49, Sprache.English));
    }

    [Fact]
    public void englischerTonumfang()
    {
        var keyboard = new Keyboard();
        keyboard.Shift(-1);
        // C2 + 23 Halbtöne = H3 bzw. B3
        Assert.Equal("C2–B3", keyboard.RangeDescription(maxSemitone: 23, sprache: Sprache.English));
        Assert.Equal("C2–H3", keyboard.RangeDescription(maxSemitone: 23, sprache: Sprache.Deutsch));
    }

    [Fact]
    public void englischeAkkordnamen()
    {
        Assert.Equal("B maj", ChordNamer.Name(new HashSet<int> { 11, 3, 6 }, sprache: Sprache.English));
        Assert.Equal("C maj/E · 1st inv.", ChordNamer.Name(new HashSet<int> { 0, 4, 7 }, 4, Sprache.English));
        Assert.Equal("C maj/G · 2nd inv.", ChordNamer.Name(new HashSet<int> { 0, 4, 7 }, 7, Sprache.English));
        Assert.Equal("C maj7/B · 3rd inv.", ChordNamer.Name(new HashSet<int> { 0, 4, 7, 11 }, 11, Sprache.English));
    }

    [Fact]
    public void englischeIntervallnamen()
    {
        Assert.Equal("C–E · major third", ChordNamer.Name(new HashSet<int> { 0, 4 }, sprache: Sprache.English));
        Assert.Equal("C–F# · tritone", ChordNamer.Name(new HashSet<int> { 0, 6 }, sprache: Sprache.English));
        Assert.Equal("C–G · perfect fifth", ChordNamer.Name(new HashSet<int> { 0, 7 }, sprache: Sprache.English));
        Assert.Equal("B–D · minor third", ChordNamer.Name(new HashSet<int> { 11, 2 }, 11, Sprache.English));
    }
}

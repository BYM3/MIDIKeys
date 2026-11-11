using static MIDIKeys.Core.ScanCodes;
using static MIDIKeys.Core.Tests.Hilfen;

namespace MIDIKeys.Core.Tests;

public class AppStateChordNameTests
{
    private static void Halte(AppState state, params ushort[] tasten)
    {
        state.PressedKeys.Clear();
        state.PressedKeys.UnionWith(tasten);
    }

    [Fact]
    public void ohneGehalteneTastenGibtEsKeinenAkkordnamen()
    {
        var state = FrischerStart();
        Assert.Null(state.ChordName);
    }

    [Fact]
    public void erkenntCDurAusDenGehaltenenTasten()
    {
        var state = FrischerStart();
        // a=C(0) d=E(4) g=G(7) -> C-E-G, C-Dur.
        Halte(state, A, D, G);
        Assert.Equal("C maj", state.ChordName);
    }

    [Fact]
    public void erkenntDenAkkordAuchUmOktavenVerschoben()
    {
        // Der Akkord bleibt derselbe, nur in einer anderen Lage — die
        // Tonhöhenklassen ändern sich durch den Oktav-Shift nicht.
        var state = FrischerStart();
        state.ShiftOctave(2);
        Halte(state, A, D, G);
        Assert.Equal("C maj", state.ChordName);
    }

    [Fact]
    public void nichtGemappteOderSteuertastenZaehlenNicht()
    {
        var state = FrischerStart();
        // Tab (Sustain) und y (Oktave) sind keine Noten und dürfen die
        // Akkorderkennung nicht verfälschen.
        Halte(state, A, D, G, KeyMap.SustainKey, KeyMap.OctaveDownKey);
        Assert.Equal("C maj", state.ChordName);
    }

    [Fact]
    public void eineEinzelneGehalteneTasteErgibtKeinenAkkordnamen()
    {
        var state = FrischerStart();
        Halte(state, A);
        Assert.Null(state.ChordName);
    }

    [Fact]
    public void zweiTastenImQuintabstandZeigenDieQuinte()
    {
        var state = FrischerStart();
        // a=C(0) g=G(7) -> Intervall Quinte.
        Halte(state, A, G);
        Assert.Equal("C–G · Quinte", state.ChordName);
    }

    [Fact]
    public void erkenntDieUmkehrungAusDerTatsaechlichGehaltenenTonhoehe()
    {
        var state = FrischerStart();
        // d=E(Halbton 4) g=G(Halbton 7) k=C(Halbton 12, eine Oktave höher) ->
        // tatsächlich tiefster gehaltener Ton ist E: erste Umkehrung von C-Dur.
        Halte(state, D, G, K);
        Assert.Equal("C maj/E · 1. Umk.", state.ChordName);
    }

    [Fact]
    public void rootPositionZeigtWeiterhinKeinenSlash()
    {
        var state = FrischerStart();
        // a=C(0) d=E(4) g=G(7) -> C ist der tiefste Ton, Grundstellung.
        Halte(state, A, D, G);
        Assert.Equal("C maj", state.ChordName);
    }

    // MARK: - Adv.-Layout

    [Fact]
    public void advLayoutStartetAusOhneGespeichertenWert()
    {
        var state = FrischerStart();
        Assert.False(state.AdvancedLayoutEnabled);
    }

    [Fact]
    public void advLayoutUeberlebtDenNeustart()
    {
        var store = new RundlaufStore();

        var ersterStart = new AppState(store);
        ersterStart.AdvancedLayoutEnabled = true;

        var zweiterStart = new AppState(store);
        Assert.True(zweiterStart.AdvancedLayoutEnabled);
    }

    [Fact]
    public void oktavSchiebungRespektiertDenEngerenAdvBereich()
    {
        var state = FrischerStart();
        state.AdvancedLayoutEnabled = true;
        // Grundlayout würde +5 erlauben, Adv.-Layout nur bis +3.
        for (var i = 0; i < 5; i++) state.ShiftOctave(1);
        Assert.Equal(3, state.Keyboard.OctaveShift);
    }

    [Fact]
    public void oktavSchiebungImGrundlayoutBleibtUnveraendert()
    {
        var state = FrischerStart();
        Assert.False(state.AdvancedLayoutEnabled);
        for (var i = 0; i < 5; i++) state.ShiftOctave(1);
        Assert.Equal(5, state.Keyboard.OctaveShift);
    }

    [Fact]
    public void chordNameFunktioniertAuchImAdvLayout()
    {
        var state = FrischerStart();
        state.AdvancedLayoutEnabled = true;
        // <=C(0) x=E(4) v=G(7) im Adv.-Layout -> C-Dur.
        Halte(state, IsoBackslash, X, V);
        Assert.Equal("C maj", state.ChordName);
    }

    [Fact]
    public void akkordnameFolgtDerEingestelltenSprache()
    {
        var state = FrischerStart();
        state.Sprache = Sprache.English;
        // Wie oben: d=E g=G k=C' -> erste Umkehrung von C-Dur.
        Halte(state, D, G, K);
        Assert.Equal("C maj/E · 1st inv.", state.ChordName);
        // a=C g=G -> Quinte
        Halte(state, A, G);
        Assert.Equal("C–G · perfect fifth", state.ChordName);
    }
}

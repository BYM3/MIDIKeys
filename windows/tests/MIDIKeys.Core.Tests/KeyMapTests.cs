using static MIDIKeys.Core.ScanCodes;

namespace MIDIKeys.Core.Tests;

public class KeyMapTests
{
    [Fact]
    public void weisseTastenLiefernDurNotenleiter()
    {
        // a s d f g h j k l ö ä #  ->  C D E F G A H C D E F G
        (ushort, int)[] erwartet =
        [
            (A, 0), (S, 2), (D, 4), (F, 5),
            (G, 7), (H, 9), (J, 11), (K, 12),
            (L, 14), (Semicolon, 16), (Quote, 17), (Backslash, 19),
        ];
        foreach (var (scancode, semitone) in erwartet)
            Assert.Equal(KeyAction.Note(semitone), KeyMap.Action(scancode));
    }

    [Fact]
    public void schwarzeTastenLiegenZwischenDenWeissen()
    {
        // w e t z u o p +  ->  C# D# F# G# A# C# D# F#
        (ushort, int)[] erwartet =
        [
            (W, 1), (E, 3), (T, 6), (Y, 8),
            (U, 10), (O, 13), (P, 15), (RightBracket, 18),
        ];
        foreach (var (scancode, semitone) in erwartet)
            Assert.Equal(KeyAction.Note(semitone), KeyMap.Action(scancode));
    }

    [Fact]
    public void steuertastenSindZugeordnet()
    {
        Assert.Equal(KeyAction.OctaveDown, KeyMap.Action(Z));  // QWERTZ y
        Assert.Equal(KeyAction.OctaveUp, KeyMap.Action(X));    // QWERTZ x
        Assert.Equal(KeyAction.Sustain, KeyMap.Action(Tab));   // Tab
    }

    [Fact]
    public void nichtGemappteTastenLiefernNil()
    {
        Assert.Null(KeyMap.Action(Space));   // Leertaste
        Assert.Null(KeyMap.Action(Escape));  // Escape
        Assert.Null(KeyMap.Action(Enter));   // Return
    }

    [Fact]
    public void esGibtGenauZwanzigNotentastenOhneLuecken()
    {
        Assert.Equal(20, KeyMap.NoteKeys.Count);
        var halbtoene = KeyMap.NoteKeys.OrderBy(e => e.Key)
            .Select(e => KeyMap.Action(e.Value))
            .Where(a => a is { Kind: KeyActionKind.Note })
            .Select(a => a!.Value.Semitone);
        Assert.Equal(Enumerable.Range(0, 20), halbtoene);
        Assert.Equal(19, KeyMap.MaxSemitone);
    }

    [Fact]
    public void keinKeycodeIstDoppeltVergeben()
    {
        var alle = KeyMap.NoteKeys.Values.Concat([Z, X, Tab]).ToList();
        Assert.Equal(alle.Count, alle.Distinct().Count());
    }

    [Fact]
    public void schwarzeTastenWerdenAlsSolcheErkannt()
    {
        // Fürs Zeichnen der Klaviatur, auch über die Oktave hinaus und negativ.
        int[] schwarz = [1, 3, 6, 8, 10, 13, 15, 18, -2];
        int[] weiss = [0, 2, 4, 5, 7, 9, 11, 12, 19, -1, -12];
        Assert.All(schwarz, s => Assert.True(KeyMap.IsBlackKey(s)));
        Assert.All(weiss, s => Assert.False(KeyMap.IsBlackKey(s)));
    }
}

using static MIDIKeys.Core.ScanCodes;

namespace MIDIKeys.Core.Tests;

public class AdvancedKeyMapTests
{
    [Fact]
    public void weisseTastenLiefernDreiOktavenAmStueck()
    {
        // < y x c v b n m , . - q w e r t z u i o p ü +
        (ushort, int)[] erwartet =
        [
            (IsoBackslash, 0), (Z, 2), (X, 4), (C, 5), (V, 7), (B, 9), (N, 11),
            (M, 12), (Comma, 14), (Period, 16), (Slash, 17), (Q, 17), (W, 19), (E, 21),
            (R, 23), (T, 24), (Y, 26), (U, 28), (I, 29), (O, 31),
            (P, 33), (LeftBracket, 35), (RightBracket, 36),
        ];
        foreach (var (scancode, semitone) in erwartet)
            Assert.Equal(KeyAction.Note(semitone), AdvancedKeyMap.Action(scancode));
    }

    [Fact]
    public void advSchwarzeTastenLiegenZwischenDenWeissen()
    {
        // a s f g h k l ä 2 3 4 6 7 9 0 ß
        (ushort, int)[] erwartet =
        [
            (A, 1), (S, 3), (F, 6), (G, 8), (H, 10), (K, 13), (L, 15),
            (Quote, 18), (D2, 18), (D3, 20), (D4, 22), (D6, 25), (D7, 27),
            (D9, 30), (D0, 32), (Minus, 34),
        ];
        foreach (var (scancode, semitone) in erwartet)
            Assert.Equal(KeyAction.Note(semitone), AdvancedKeyMap.Action(scancode));
    }

    [Fact]
    public void advSteuertastenSindZugeordnet()
    {
        Assert.Equal(KeyAction.OctaveDown, AdvancedKeyMap.Action(Grave));  // ^ (Mac: kVK_ISO_Section)
        Assert.Equal(KeyAction.OctaveUp, AdvancedKeyMap.Action(D1));       // 1
        Assert.Equal(KeyAction.Sustain, AdvancedKeyMap.Action(KeyMap.SustainKey));  // Tab bleibt geteilt
    }

    [Fact]
    public void zweiPaareTastenTeilenSichJeEinenHalbton()
    {
        // Bestätigt nach mehreren Korrekturrunden: '-' und 'q' spielen beide
        // F4, 'ä' und '2' spielen beide F#4 — keine zwei unterschiedlichen
        // Noten, echte Dopplungen.
        Assert.Equal(KeyAction.Note(17), AdvancedKeyMap.Action(Slash));  // -
        Assert.Equal(KeyAction.Note(17), AdvancedKeyMap.Action(Q));      // q
        Assert.Equal(KeyAction.Note(18), AdvancedKeyMap.Action(Quote));  // ä
        Assert.Equal(KeyAction.Note(18), AdvancedKeyMap.Action(D2));     // 2
    }

    [Fact]
    public void siebenunddreissigEindeutigeHalbtoeneOhneLuecke()
    {
        // 39 Tasten insgesamt (23 weiß + 16 schwarz), aber zwei Dopplungen ->
        // 37 eindeutige Halbtöne. Trotzdem lückenlos: die Dopplungen verbrauchen
        // keine eigene Position, sie teilen sich eine mit ihrem Partner.
        Assert.Equal(37, AdvancedKeyMap.NoteKeys.Count);
        Assert.Equal(Enumerable.Range(0, 37), AdvancedKeyMap.NoteKeys.Keys.Order());
    }

    [Fact]
    public void advKeinKeycodeIstDoppeltVergeben()
    {
        // Scancodes selbst sind immer eindeutig — nur die Halbtöne, auf die sie
        // zeigen, können sich bei den zwei bestätigten Dopplungen wiederholen.
        var alle = AdvancedKeyMap.NoteKeys.Values
            .Append(AdvancedKeyMap.OctaveDownKey).Append(AdvancedKeyMap.OctaveUpKey)
            .ToList();
        Assert.Equal(alle.Count, alle.Distinct().Count());
    }

    [Fact]
    public void semitoneRangeReichtBisSechsunddreissig()
    {
        Assert.Equal(36, AdvancedKeyMap.MaxSemitone);
    }

    [Fact]
    public void schiebebereichHaeltAlleNotenImGueltigenMidiBereich()
    {
        // 3 Oktaven statt 2 -> weniger Spielraum nach oben als im Grundmodus
        // (-4...5): bei semitoneRange bis 36 wäre +4 schon außerhalb 0...127.
        Assert.Equal(-4, AdvancedKeyMap.MinShift);
        Assert.Equal(3, AdvancedKeyMap.MaxShift);
        for (var shift = AdvancedKeyMap.MinShift; shift <= AdvancedKeyMap.MaxShift; shift++)
        {
            for (var semitone = 0; semitone <= AdvancedKeyMap.MaxSemitone; semitone++)
            {
                var note = Keyboard.BaseNote + shift * 12 + semitone;
                Assert.InRange(note, 0, 127);
            }
        }
    }
}

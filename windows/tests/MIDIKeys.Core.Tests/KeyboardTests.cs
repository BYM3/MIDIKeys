namespace MIDIKeys.Core.Tests;

public class KeyboardTests
{
    [Fact]
    public void grundzustandBeginntBeiC3()
    {
        var keyboard = new Keyboard();
        Assert.Equal(0, keyboard.OctaveShift);
        Assert.Equal(48, keyboard.Note(0));
        Assert.Equal(67, keyboard.Note(19));
    }

    [Fact]
    public void oktavShiftVerschiebtUmZwoelfHalbtoene()
    {
        var keyboard = new Keyboard();
        Assert.True(keyboard.Shift(1));
        Assert.Equal(60, keyboard.Note(0));
        Assert.True(keyboard.Shift(-2));
        Assert.Equal(36, keyboard.Note(0));
    }

    [Fact]
    public void shiftKlemmtAnDerUnterenGrenze()
    {
        var keyboard = new Keyboard();
        for (var i = 0; i < 4; i++) Assert.True(keyboard.Shift(-1));
        Assert.Equal(-4, keyboard.OctaveShift);
        Assert.Equal(0, keyboard.Note(0));
        // Ein weiterer Schritt würde unter MIDI 0 fallen und wird abgelehnt.
        Assert.False(keyboard.Shift(-1));
        Assert.Equal(-4, keyboard.OctaveShift);
    }

    [Fact]
    public void shiftKlemmtAnDerOberenGrenze()
    {
        var keyboard = new Keyboard();
        for (var i = 0; i < 5; i++) Assert.True(keyboard.Shift(1));
        Assert.Equal(5, keyboard.OctaveShift);
        // Höchste Taste landet exakt auf MIDI 127.
        Assert.Equal(127, keyboard.Note(19));
        Assert.False(keyboard.Shift(1));
        Assert.Equal(5, keyboard.OctaveShift);
    }

    [Fact]
    public void alleTastenBleibenInJederLageImGueltigenBereich()
    {
        for (var shift = Keyboard.MinShift; shift <= Keyboard.MaxShift; shift++)
        {
            var keyboard = new Keyboard();
            keyboard.Shift(shift);
            for (var semitone = 0; semitone <= KeyMap.MaxSemitone; semitone++)
            {
                // Ungeklemmt rechnen — der byte-Cast in Note() würde einen Überlauf verschleiern.
                var note = Keyboard.BaseNote + keyboard.OctaveShift * 12 + semitone;
                Assert.InRange(note, 0, 127);
                Assert.Equal(note, keyboard.Note(semitone));
            }
        }
    }

    [Fact]
    public void notennamenFolgenDerKonventionC3GleichAchtundvierzig()
    {
        Assert.Equal("C3", Notes.Name(48));
        Assert.Equal("C4", Notes.Name(60));
        Assert.Equal("C#3", Notes.Name(49));
        Assert.Equal("H3", Notes.Name(59));
        Assert.Equal("C-1", Notes.Name(0));
    }

    [Fact]
    public void bereichsbeschreibungZeigtAktuelleLage()
    {
        var keyboard = new Keyboard();
        Assert.Equal("C3–G4", keyboard.RangeDescription());
        keyboard.Shift(1);
        Assert.Equal("C4–G5", keyboard.RangeDescription());
    }

    [Fact]
    public void bereichsbeschreibungRespektiertDenAngegebenenTonumfang()
    {
        // Fürs Adv.-Layout: 38 statt 20 Tasten, höchster Halbton 37 statt 19.
        var keyboard = new Keyboard();
        Assert.Equal("C3–C#6", keyboard.RangeDescription(maxSemitone: 37));
    }
}

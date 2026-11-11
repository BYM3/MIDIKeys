namespace MIDIKeys.Core.Tests;

public class MIDIEngineTests
{
    [Fact]
    public void noteOnUndNoteOffWerdenGesendet()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.NoteOn(0, 48);
        engine.NoteOff(0);
        Assert.Equal([MidiMessage.NoteOn(48, 100), MidiMessage.NoteOff(48)], spy.Sent);
    }

    [Fact]
    public void noteOffBenutztDieBeimStartGesendeteNote()
    {
        // Der wichtigste Fall: Wird die Oktave gewechselt, während ein Ton klingt,
        // muss das Note-Off an die ursprüngliche Note gehen — sonst hängt der Ton.
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.NoteOn(0, 48);
        engine.NoteOff(0);  // die Oktave hat sich zwischenzeitlich geändert
        Assert.Equal(MidiMessage.NoteOff(48), spy.Sent[^1]);
    }

    [Fact]
    public void noteOffOhneVorherigesNoteOnSendetNichts()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.NoteOff(5);
        Assert.Empty(spy.Sent);
    }

    [Fact]
    public void erneutesNoteOnAufDerselbenTasteBeendetDenAltenTonZuerst()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.NoteOn(0, 48);
        engine.NoteOn(0, 60);
        Assert.Equal(
            [MidiMessage.NoteOn(48, 100), MidiMessage.NoteOff(48), MidiMessage.NoteOn(60, 100)],
            spy.Sent);
    }

    [Fact]
    public void sustainSendetControlChange64()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.Sustain(true);
        engine.Sustain(false);
        Assert.Equal([MidiMessage.ControlChange(64, 127), MidiMessage.ControlChange(64, 0)], spy.Sent);
    }

    [Fact]
    public void panicBeendetAlleGehaltenenNotenUndSendetAllNotesOff()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.NoteOn(0, 48);
        engine.NoteOn(4, 52);
        engine.Sustain(true);
        spy.Sent.Clear();

        engine.Panic();

        // Reihenfolge: erst die gehaltenen Noten, dann Sustain lösen, dann CC123.
        var noteOffs = spy.Sent.Take(2).ToList();
        Assert.Contains(MidiMessage.NoteOff(48), noteOffs);
        Assert.Contains(MidiMessage.NoteOff(52), noteOffs);
        Assert.Equal(MidiMessage.ControlChange(64, 0), spy.Sent[2]);
        Assert.Equal(MidiMessage.ControlChange(123, 0), spy.Sent[3]);
        Assert.Equal(0, engine.HeldCount);
    }

    [Fact]
    public void panicOhneGehalteneNotenSendetTrotzdemAllNotesOff()
    {
        var spy = new SpyTransport();
        var engine = new MidiEngine(spy);
        engine.Panic();
        Assert.Equal([MidiMessage.ControlChange(64, 0), MidiMessage.ControlChange(123, 0)], spy.Sent);
    }
}

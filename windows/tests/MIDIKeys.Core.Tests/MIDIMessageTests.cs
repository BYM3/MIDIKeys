namespace MIDIKeys.Core.Tests;

public class MIDIMessageTests
{
    [Fact]
    public void noteOnErzeugtKorrekteBytes()
    {
        Assert.Equal([0x90, 60, 100], MidiMessage.NoteOn(60, 100).Bytes);
    }

    [Fact]
    public void noteOffErzeugtKorrekteBytes()
    {
        Assert.Equal([0x80, 60, 0], MidiMessage.NoteOff(60).Bytes);
    }

    [Fact]
    public void controlChangeErzeugtKorrekteBytes()
    {
        Assert.Equal([0xB0, 64, 127], MidiMessage.ControlChange(64, 127).Bytes);
    }

    [Fact]
    public void datenbytesWerdenAufSiebenBitBegrenzt()
    {
        // Ein Datenbyte darf nie das oberste Bit setzen, sonst liest die DAW es als Status-Byte.
        Assert.Equal([0x90, 72, 72], MidiMessage.NoteOn(200, 200).Bytes);
    }

    [Fact]
    public void controllerNummernSindDieErwarteten()
    {
        Assert.Equal(64, MidiMessage.SustainController);
        Assert.Equal(123, MidiMessage.AllNotesOffController);
    }

    [Fact]
    public void shortMessageIstFuerMidiOutShortMsgGepackt()
    {
        // Windows: midiOutShortMsg erwartet das Status-Byte im niedrigsten Byte.
        Assert.Equal(0x00643C90u, MidiMessage.NoteOn(60, 100).ShortMessage);
        Assert.Equal(0x00003C80u, MidiMessage.NoteOff(60).ShortMessage);
        Assert.Equal(0x007F40B0u, MidiMessage.ControlChange(64, 127).ShortMessage);
        Assert.Equal(0x00484890u, MidiMessage.NoteOn(200, 200).ShortMessage);
    }
}

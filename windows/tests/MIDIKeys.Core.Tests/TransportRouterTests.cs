namespace MIDIKeys.Core.Tests;

public class TransportRouterTests
{
    [Fact]
    public void notenGehenImNormalmodusAnsExterneZiel()
    {
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.Send(MidiMessage.NoteOn(60, 100));

        Assert.Equal([MidiMessage.NoteOn(60, 100)], external.Sent);
        Assert.Empty(local.Sent);
    }

    [Fact]
    public void notenGehenImKlavierModusAnsLokaleZiel()
    {
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local) { PianoModeEnabled = true };

        router.Send(MidiMessage.NoteOn(60, 100));

        Assert.Equal([MidiMessage.NoteOn(60, 100)], local.Sent);
        Assert.Empty(external.Sent);
    }

    [Fact]
    public void noteOffGehtAnDasZielDesZugehoerigenNoteOns()
    {
        // Der kritische Fall: eine Note wird extern gestartet, dann wird
        // mitten im Halten auf Klavier-Modus umgeschaltet. Das Note-Off muss
        // trotzdem extern ankommen, sonst haengt der Ton in der DAW fuer immer.
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.Send(MidiMessage.NoteOn(60, 100));
        router.PianoModeEnabled = true;
        router.Send(MidiMessage.NoteOff(60));

        Assert.Equal([MidiMessage.NoteOn(60, 100), MidiMessage.NoteOff(60)], external.Sent);
        Assert.Empty(local.Sent);
    }

    [Fact]
    public void noteOffGehtAnDasLokaleZielWennDortGestartet()
    {
        // Gleicher Fall umgekehrt: lokal gestartet, dann zurueck auf MIDI
        // umgeschaltet, waehrend die Taste noch gehalten wird.
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.PianoModeEnabled = true;
        router.Send(MidiMessage.NoteOn(60, 100));
        router.PianoModeEnabled = false;
        router.Send(MidiMessage.NoteOff(60));

        Assert.Equal([MidiMessage.NoteOn(60, 100), MidiMessage.NoteOff(60)], local.Sent);
        Assert.Empty(external.Sent);
    }

    [Fact]
    public void mehrereNotenBehaltenIhrEigenesZielUnabhaengigVoneinander()
    {
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.Send(MidiMessage.NoteOn(60, 100));  // extern
        router.PianoModeEnabled = true;
        router.Send(MidiMessage.NoteOn(64, 100));  // lokal
        router.Send(MidiMessage.NoteOff(60));      // muss extern bleiben
        router.Send(MidiMessage.NoteOff(64));      // muss lokal bleiben

        Assert.Equal([MidiMessage.NoteOn(60, 100), MidiMessage.NoteOff(60)], external.Sent);
        Assert.Equal([MidiMessage.NoteOn(64, 100), MidiMessage.NoteOff(64)], local.Sent);
    }

    [Fact]
    public void noteOffOhneBekanntesZielFolgtDemAktuellenModus()
    {
        // Kann eigentlich nicht vorkommen (MidiEngine sendet nie ein Note-Off
        // ohne vorheriges Note-On), aber falls doch: kein Crash, sinnvoller
        // Fallback statt eines verlorenen Events.
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local) { PianoModeEnabled = true };

        router.Send(MidiMessage.NoteOff(60));

        Assert.Equal([MidiMessage.NoteOff(60)], local.Sent);
        Assert.Empty(external.Sent);
    }

    [Fact]
    public void controlChangeGehtImmerAnBeideZiele()
    {
        // Sustain/All-Notes-Off betreffen den Gesamtzustand des jeweils
        // aktiven Ziels; ans inaktive Ziel mitzusenden ist harmlos, da dort
        // dank der Zielverfolgung ohnehin nichts mehr klingt.
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.Send(MidiMessage.ControlChange(64, 127));

        Assert.Equal([MidiMessage.ControlChange(64, 127)], external.Sent);
        Assert.Equal([MidiMessage.ControlChange(64, 127)], local.Sent);
    }

    [Fact]
    public void zielVerfolgungRaeumtNachNoteOffAuf()
    {
        // Nach dem Note-Off darf kein Eintrag mehr fuer diese Note bestehen,
        // sonst koennte ein spaeteres Note-Off derselben Tonhoehe faelschlich
        // das alte Ziel treffen.
        var external = new SpyTransport();
        var local = new SpyTransport();
        var router = new TransportRouter(external, local);

        router.Send(MidiMessage.NoteOn(60, 100));
        router.Send(MidiMessage.NoteOff(60));
        router.PianoModeEnabled = true;
        router.Send(MidiMessage.NoteOn(60, 100));
        router.Send(MidiMessage.NoteOff(60));

        Assert.Equal([MidiMessage.NoteOn(60, 100), MidiMessage.NoteOff(60)], external.Sent);
        Assert.Equal([MidiMessage.NoteOn(60, 100), MidiMessage.NoteOff(60)], local.Sent);
    }
}

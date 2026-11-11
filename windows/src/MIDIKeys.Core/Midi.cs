namespace MIDIKeys.Core;

public enum MidiKind { NoteOn, NoteOff, ControlChange }

/// <summary>Eine MIDI-1.0-Kanalnachricht auf Kanal 1.</summary>
public readonly record struct MidiMessage(MidiKind Kind, byte Data1, byte Data2 = 0)
{
    public const byte SustainController = 64;
    public const byte AllNotesOffController = 123;

    public static MidiMessage NoteOn(byte note, byte velocity) => new(MidiKind.NoteOn, note, velocity);
    public static MidiMessage NoteOff(byte note) => new(MidiKind.NoteOff, note);
    public static MidiMessage ControlChange(byte controller, byte value) => new(MidiKind.ControlChange, controller, value);

    /// <summary>Die drei Bytes; Datenbytes auf 7 Bit maskiert.</summary>
    public byte[] Bytes => Kind switch
    {
        MidiKind.NoteOn => [0x90, (byte)(Data1 & 0x7F), (byte)(Data2 & 0x7F)],
        MidiKind.NoteOff => [0x80, (byte)(Data1 & 0x7F), 0],
        _ => [0xB0, (byte)(Data1 & 0x7F), (byte)(Data2 & 0x7F)],
    };

    /// <summary>Gepackt wie für <c>midiOutShortMsg</c>: Status im niedrigsten Byte.</summary>
    public uint ShortMessage
    {
        get
        {
            var b = Bytes;
            return (uint)(b[0] | (b[1] << 8) | (b[2] << 16));
        }
    }
}

/// <summary>Wohin MIDI-Nachrichten gehen. Existiert, damit die Engine gegen einen Spy testbar ist.</summary>
public interface IMidiSender
{
    void Send(MidiMessage message);
}

public sealed class NullSender : IMidiSender
{
    public void Send(MidiMessage message) { }
}

/// <summary>Verwaltet die klingenden Noten und übersetzt Tastenaktionen in MIDI.</summary>
public sealed class MidiEngine(IMidiSender transport)
{
    public const byte Velocity = 100;

    /// <summary>Halbton -> tatsächlich gesendete Note, damit das Note-Off nach einem Oktavwechsel trifft.</summary>
    private readonly Dictionary<int, byte> _held = new();

    public int HeldCount => _held.Count;

    public void NoteOn(int semitone, byte note)
    {
        if (_held.TryGetValue(semitone, out var laufend)) transport.Send(MidiMessage.NoteOff(laufend));
        _held[semitone] = note;
        transport.Send(MidiMessage.NoteOn(note, Velocity));
    }

    public void NoteOff(int semitone)
    {
        if (!_held.Remove(semitone, out var note)) return;
        transport.Send(MidiMessage.NoteOff(note));
    }

    public void Sustain(bool on) =>
        transport.Send(MidiMessage.ControlChange(MidiMessage.SustainController, on ? (byte)127 : (byte)0));

    /// <summary>Beendet alles Klingende — beim Verlassen des Modus und beim Beenden.</summary>
    public void Panic()
    {
        foreach (var note in _held.Values) transport.Send(MidiMessage.NoteOff(note));
        _held.Clear();
        transport.Send(MidiMessage.ControlChange(MidiMessage.SustainController, 0));
        transport.Send(MidiMessage.ControlChange(MidiMessage.AllNotesOffController, 0));
    }
}

/// <summary>
/// Leitet exklusiv an den MIDI-Ausgang oder den lokalen Klavier-Ton. Note-Off
/// geht garantiert dorthin, wo das Note-On hinging — auch nach Umschalten.
/// </summary>
public sealed class TransportRouter(IMidiSender external, IMidiSender local) : IMidiSender
{
    private readonly Dictionary<byte, IMidiSender> _routedTo = new();

    public bool PianoModeEnabled { get; set; }

    public void Send(MidiMessage message)
    {
        var ziel = PianoModeEnabled ? local : external;
        switch (message.Kind)
        {
            case MidiKind.NoteOn:
                _routedTo[message.Data1] = ziel;
                ziel.Send(message);
                break;
            case MidiKind.NoteOff:
                (_routedTo.Remove(message.Data1, out var alt) ? alt : ziel).Send(message);
                break;
            default:
                external.Send(message);
                local.Send(message);
                break;
        }
    }
}

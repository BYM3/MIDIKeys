using MIDIKeys.Core;

namespace MIDIKeys;

/// <summary>Ein geöffnetes winmm-Ausgabegerät.</summary>
internal sealed class WinMmOutput : IMidiSender, IDisposable
{
    private IntPtr _handle;
    public string Name { get; }

    private WinMmOutput(IntPtr handle, string name)
    {
        _handle = handle;
        Name = name;
    }

    public static WinMmOutput? Open(Func<string, bool> match)
    {
        var names = WinMm.OutputNames();
        for (var i = 0; i < names.Count; i++)
        {
            if (!match(names[i])) continue;
            if (WinMm.midiOutOpen(out var h, (uint)i, IntPtr.Zero, IntPtr.Zero, 0) == 0)
                return new WinMmOutput(h, names[i]);
            Diagnose.Log($"midiOutOpen fehlgeschlagen: {names[i]}");
        }
        return null;
    }

    public void Send(MidiMessage message)
    {
        if (_handle != IntPtr.Zero) WinMm.midiOutShortMsg(_handle, message.ShortMessage);
    }

    public void SendRaw(uint shortMessage)
    {
        if (_handle != IntPtr.Zero) WinMm.midiOutShortMsg(_handle, shortMessage);
    }

    public void Dispose()
    {
        if (_handle == IntPtr.Zero) return;
        WinMm.midiOutReset(_handle);
        WinMm.midiOutClose(_handle);
        _handle = IntPtr.Zero;
    }
}

/// <summary>Ein selbst angelegter virtueller MIDI-Port (über den teVirtualMIDI-Treiber von loopMIDI).</summary>
internal sealed class VirtualMidiPort : IMidiSender, IDisposable
{
    public const string PortName = "MIDIKeys";
    private IntPtr _port;

    private VirtualMidiPort(IntPtr port) => _port = port;

    public static bool DriverAvailable
    {
        get
        {
            var path = Path.Combine(Environment.SystemDirectory, "teVirtualMIDI64.dll");
            return File.Exists(path);
        }
    }

    public static VirtualMidiPort? Create()
    {
        if (!DriverAvailable) return null;
        try
        {
            // Nur die Sende-Seite: Für DAWs erscheint "MIDIKeys" als MIDI-Eingang.
            var port = TeVirtualMidi.virtualMIDICreatePortEx2(
                PortName, IntPtr.Zero, IntPtr.Zero, 0,
                TeVirtualMidi.TE_VM_FLAGS_PARSE_TX | TeVirtualMidi.TE_VM_FLAGS_INSTANTIATE_TX_ONLY);
            if (port == IntPtr.Zero)
            {
                Diagnose.Log($"virtualMIDICreatePortEx2 fehlgeschlagen, Fehler {System.Runtime.InteropServices.Marshal.GetLastWin32Error()}");
                return null;
            }
            return new VirtualMidiPort(port);
        }
        catch (Exception e) when (e is DllNotFoundException or EntryPointNotFoundException or BadImageFormatException)
        {
            Diagnose.Log($"teVirtualMIDI nicht nutzbar: {e.Message}");
            return null;
        }
    }

    public void Send(MidiMessage message)
    {
        if (_port == IntPtr.Zero) return;
        var bytes = message.Bytes;
        TeVirtualMidi.virtualMIDISendData(_port, bytes, (uint)bytes.Length);
    }

    public void Dispose()
    {
        if (_port == IntPtr.Zero) return;
        TeVirtualMidi.virtualMIDIClosePort(_port);
        _port = IntPtr.Zero;
    }
}

/// <summary>
/// Der MIDI-Ausgang zur DAW. Automatisch: ein eigener virtueller Port
/// "MIDIKeys" (wie die CoreMIDI-Quelle unter macOS). Alternativ ein
/// vorhandenes Ausgabegerät, z. B. ein loopMIDI-Port.
///
/// Das Ziel wird nur gewechselt, wenn der Nutzer es in den Einstellungen
/// ändert — nie während Töne klingen (vorher räumt die Engine per Panic auf).
/// </summary>
internal sealed class ExternalOutput : IMidiSender, IDisposable
{
    private IMidiSender? _target;
    private IDisposable? _disposable;

    public string? Status { get; private set; }
    public bool Connected => _target != null;

    /// <summary>GS-Synth und der eigene Port tauchen nie in der Liste der Ausgänge auf.</summary>
    public static bool IsSelectable(string name) =>
        !string.IsNullOrWhiteSpace(name)
        && !name.Contains("GS Wavetable", StringComparison.OrdinalIgnoreCase)
        && !string.Equals(name, VirtualMidiPort.PortName, StringComparison.OrdinalIgnoreCase);

    public static List<string> SelectableDevices() =>
        WinMm.OutputNames().Where(IsSelectable).Distinct().ToList();

    public void Apply(string? deviceName, Sprache sprache)
    {
        Close();
        if (deviceName is null)
        {
            var port = VirtualMidiPort.Create();
            if (port != null)
            {
                _target = port; _disposable = port;
                Status = sprache.Text("Sendet an den MIDI-Eingang „MIDIKeys“", "Sends to the MIDI input “MIDIKeys”");
            }
            else
            {
                Status = sprache.Text("Kein MIDI-Ausgang – loopMIDI installieren", "No MIDI output – install loopMIDI");
            }
        }
        else
        {
            var output = WinMmOutput.Open(n => n == deviceName);
            if (output != null)
            {
                _target = output; _disposable = output;
                Status = sprache.Text($"Sendet an „{deviceName}“", $"Sends to “{deviceName}”");
            }
            else
            {
                Status = sprache.Text($"„{deviceName}“ nicht verfügbar", $"“{deviceName}” not available");
            }
        }
        Diagnose.Log($"MIDI-Ausgang: {Status}");
    }

    public void Send(MidiMessage message)
    {
        _target?.Send(message);
        Diagnose.LogMessage("extern", message, _target != null);
    }

    private void Close()
    {
        _disposable?.Dispose();
        _disposable = null;
        _target = null;
    }

    public void Dispose() => Close();
}

/// <summary>
/// Spielt Noten lokal hörbar über den eingebauten "Microsoft GS Wavetable
/// Synth" (Programm 0 = Acoustic Grand Piano) — keine eigenen Samples nötig.
/// Das Gerät ist nur geöffnet, solange gespielt werden kann.
/// </summary>
internal sealed class LocalPiano : IMidiSender, IDisposable
{
    private WinMmOutput? _synth;
    private bool _sollLaufen;

    private static bool IsSynth(string name) => name.Contains("GS Wavetable", StringComparison.OrdinalIgnoreCase);

    public static bool Available => WinMm.OutputNames().Any(IsSynth);

    public void SetAktiv(bool aktiv)
    {
        _sollLaufen = aktiv;
        if (aktiv) EnsureOpen();
        else
        {
            _synth?.Dispose();
            _synth = null;
        }
    }

    private void EnsureOpen()
    {
        if (_synth != null) return;
        _synth = WinMmOutput.Open(IsSynth);
        _synth?.SendRaw(0x00C0); // Program Change 0: Klavier
        if (_synth == null) Diagnose.Log("GS Wavetable Synth ließ sich nicht öffnen");
    }

    public void Send(MidiMessage message)
    {
        // Sicherheitsnetz: Note-On öffnet bei Bedarf, alles andere weckt nichts.
        if (message.Kind == MidiKind.NoteOn && _sollLaufen) EnsureOpen();
        _synth?.Send(message);
        Diagnose.LogMessage("piano", message, _synth != null);
    }

    public void Dispose()
    {
        _synth?.Dispose();
        _synth = null;
    }
}

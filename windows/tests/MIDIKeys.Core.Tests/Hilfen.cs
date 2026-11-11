using System.Text.Json;

namespace MIDIKeys.Core.Tests;

/// <summary>Merkt sich alles Gesendete — Gegenstück zum SpyTransport der Swift-Tests.</summary>
internal sealed class SpyTransport : IMidiSender
{
    public List<MidiMessage> Sent { get; } = new();
    public void Send(MidiMessage message) => Sent.Add(message);
}

/// <summary>
/// Wie <see cref="MemorySettingsStore"/>, aber jedes Laden/Speichern geht durch
/// JSON. So teilen sich zwei AppStates kein Settings-Objekt, und ein
/// vergessenes Save() fiele in den Neustart-Tests auf.
/// </summary>
internal sealed class RundlaufStore : ISettingsStore
{
    private string _json = JsonSerializer.Serialize(new Settings());
    public int Speicherungen { get; private set; }

    public Settings Load() => JsonSerializer.Deserialize<Settings>(_json)!;

    public void Save(Settings settings)
    {
        _json = JsonSerializer.Serialize(settings);
        Speicherungen++;
    }
}

internal static class Hilfen
{
    /// <summary>Frischer Zustand, fest auf Deutsch — sonst hinge das Ergebnis an der Systemsprache.</summary>
    public static AppState FrischerStart() =>
        new(new MemorySettingsStore(new Settings { Sprache = "de" }));

    public static HashSet<int> Toene(params int[] pitchClasses) => [.. pitchClasses];
}

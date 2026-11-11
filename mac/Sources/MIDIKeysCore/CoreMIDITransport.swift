import CoreMIDI

/// Eine virtuelle MIDI-Quelle namens "MIDIKeys". Jede DAW sieht sie ohne
/// weiteres Setup als MIDI-Eingang — kein IAC-Driver nötig.
public final class CoreMIDITransport: MIDISending {
    private var client = MIDIClientRef()
    private var source = MIDIEndpointRef()

    /// Feste, aus der Bundle-ID abgeleitete MIDI-Unique-ID.
    ///
    /// Ohne das vergibt macOS bei jedem Start eine zufällige ID (verifiziert:
    /// zwei Läufe ergaben -446188286 und 667761548). Manche Programme merken
    /// sich ein MIDI-Gerät über diese ID statt über den Namen — bei einer
    /// wechselnden ID wirkt die Quelle dann bei jedem Neustart "neu" oder
    /// bleibt aus einer gespeicherten Zuordnung verschwunden.
    ///
    /// Einmalig per FNV-1a aus der ursprünglichen Bundle-ID berechnet (oberstes
    /// Bit gekappt) und seitdem fest — so bleibt die Quelle für DAWs dieselbe,
    /// auch wenn sich die Bundle-ID ändert.
    static let persistentUniqueID: MIDIUniqueID = 1_141_659_957

    public init?() {
        guard MIDIClientCreateWithBlock("MIDIKeys" as CFString, &client, nil) == noErr else {
            return nil
        }
        guard MIDISourceCreateWithProtocol(
            client, "MIDIKeys" as CFString, ._1_0, &source
        ) == noErr else {
            MIDIClientDispose(client)
            return nil
        }

        // Best effort: schlägt fehl, wenn die ID schon vergeben ist (etwa an
        // eine andere Quelle aus einer vorherigen, noch nicht beendeten
        // Sitzung). Dann bleibt die vom System automatisch vergebene ID
        // bestehen — kein Grund, die App scheitern zu lassen.
        MIDIObjectSetIntegerProperty(source, kMIDIPropertyUniqueID, CoreMIDITransport.persistentUniqueID)
    }

    deinit {
        MIDIEndpointDispose(source)
        MIDIClientDispose(client)
    }

    public func send(_ message: MIDIMessage) {
        let bytes = message.bytes
        // MIDI-1.0-Nachricht in ein UMP-Wort verpacken:
        // Typ 2, Gruppe 0, Status-Nibble, Kanal, zwei Datenbytes.
        var word = MIDI1UPChannelVoiceMessage(
            0, UInt8(bytes[0] >> 4), UInt8(bytes[0] & 0x0F), bytes[1], bytes[2]
        )
        var list = MIDIEventList()
        var packet = MIDIEventListInit(&list, ._1_0)
        packet = MIDIEventListAdd(
            &list, MemoryLayout<MIDIEventList>.size, packet, 0, 1, &word
        )
        MIDIReceivedEventList(source, &list)
    }
}

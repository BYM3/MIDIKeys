import Foundation

/// Schreibt Diagnosezeilen nach `/tmp/midikeys.log`.
///
/// Eine Menüleisten-App ohne Fenster kann Probleme sonst nirgends melden;
/// stdout ist beim Start per `open` nicht sichtbar.
public enum Diagnose {
    private static let pfad = "/tmp/midikeys.log"
    private static let formatierer: DateFormatter = {
        let f = DateFormatter()
        f.dateFormat = "HH:mm:ss.SSS"
        return f
    }()

    public static func log(_ text: String) {
        let zeile = "[\(formatierer.string(from: Date()))] \(text)\n"
        if let daten = zeile.data(using: .utf8) {
            if let handle = FileHandle(forWritingAtPath: pfad) {
                handle.seekToEndOfFile()
                handle.write(daten)
                try? handle.close()
            } else {
                try? daten.write(to: URL(fileURLWithPath: pfad))
            }
        }
    }
}

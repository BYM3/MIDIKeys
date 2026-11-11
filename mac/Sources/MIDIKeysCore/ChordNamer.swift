/// Erkennt Akkorde und Intervalle aus einer Menge gehaltener
/// Tonhöhenklassen (0–11, oktavunabhängig).
///
/// Quellen, gegen die Formen und Regeln geprüft sind:
/// - Wikipedia „Chord names and symbols (popular music)“ — Akkordformen
/// - Wikipedia „Inversion (music)“, musictheory.net Lektion 47 —
///   Umkehrungen hängen allein vom tiefsten Ton ab
/// - Wikipedia „Root (chord)“, „Added sixth chord“ — Grundton mehrdeutiger
///   Akkorde hängt vom Kontext ab; ohne Kontext: Terzschichtung
/// - de.wikipedia „Intervall (Musik)“ — deutsche Intervallnamen
public enum ChordNamer {
    struct Akkordform {
        let name: String
        let intervalle: Set<Int>
        /// Welche Umkehrung entsteht, wenn dieses Intervall im Bass liegt:
        /// Terz → 1., Quinte → 2., Septime → 3. Andere Akkordtöne (None,
        /// Sexte, sus-Ton …) im Bass sind keine Umkehrung, nur Slash.
        let umkehrung: [Int: Int]
    }

    private static let dur: [Int: Int] = [4: 1, 7: 2]
    private static let moll: [Int: Int] = [3: 1, 7: 2]
    private static let dom7: [Int: Int] = [4: 1, 7: 2, 10: 3]
    private static let moll7: [Int: Int] = [3: 1, 7: 2, 10: 3]

    /// Die Reihenfolge entscheidet bei Gleichstand (siehe `name`):
    /// Terzschichtungen (Drei- und Septakkorde) vor add-Akkorden.
    static let akkordformen: [Akkordform] = [
        // Dreiklänge
        Akkordform(name: "maj", intervalle: [0, 4, 7], umkehrung: dur),
        Akkordform(name: "min", intervalle: [0, 3, 7], umkehrung: moll),
        Akkordform(name: "dim", intervalle: [0, 3, 6], umkehrung: [3: 1, 6: 2]),
        Akkordform(name: "aug", intervalle: [0, 4, 8], umkehrung: [4: 1, 8: 2]),
        Akkordform(name: "sus2", intervalle: [0, 2, 7], umkehrung: [7: 2]),
        Akkordform(name: "sus4", intervalle: [0, 5, 7], umkehrung: [7: 2]),
        // Septakkorde ohne Quinte (typisches Linke-Hand-Voicing)
        Akkordform(name: "7(no5)", intervalle: [0, 4, 10], umkehrung: [4: 1, 10: 3]),
        Akkordform(name: "maj7(no5)", intervalle: [0, 4, 11], umkehrung: [4: 1, 11: 3]),
        Akkordform(name: "m7(no5)", intervalle: [0, 3, 10], umkehrung: [3: 1, 10: 3]),
        // Septakkorde
        Akkordform(name: "7", intervalle: [0, 4, 7, 10], umkehrung: dom7),  // Dominantseptakkord
        Akkordform(name: "maj7", intervalle: [0, 4, 7, 11], umkehrung: [4: 1, 7: 2, 11: 3]),
        Akkordform(name: "m7", intervalle: [0, 3, 7, 10], umkehrung: moll7),
        Akkordform(name: "m7b5", intervalle: [0, 3, 6, 10], umkehrung: [3: 1, 6: 2, 10: 3]),  // halbvermindert
        Akkordform(name: "dim7", intervalle: [0, 3, 6, 9], umkehrung: [3: 1, 6: 2, 9: 3]),
        Akkordform(name: "mMaj7", intervalle: [0, 3, 7, 11], umkehrung: [3: 1, 7: 2, 11: 3]),
        Akkordform(name: "aug7", intervalle: [0, 4, 8, 10], umkehrung: [4: 1, 8: 2, 10: 3]),
        Akkordform(name: "maj7#5", intervalle: [0, 4, 8, 11], umkehrung: [4: 1, 8: 2, 11: 3]),
        Akkordform(name: "7b5", intervalle: [0, 4, 6, 10], umkehrung: [4: 1, 6: 2, 10: 3]),
        Akkordform(name: "7sus4", intervalle: [0, 5, 7, 10], umkehrung: [7: 2, 10: 3]),
        // add-Akkorde (ohne Septime). Bewusst KEINE 6 und m6: Sie bestehen
        // immer aus denselben Tönen wie ein Septakkord in 1. Umkehrung
        // (C6 = Am7/C, Cm6 = Am7b5/C) — beim Lernen soll der Septakkord
        // mit seiner Umkehrung sichtbar sein.
        Akkordform(name: "add9", intervalle: [0, 4, 7, 2], umkehrung: dur),
        Akkordform(name: "madd9", intervalle: [0, 3, 7, 2], umkehrung: moll),
        Akkordform(name: "add11", intervalle: [0, 4, 7, 5], umkehrung: dur),
        // None-Akkorde (None = +2, entspricht +14 mod 12)
        Akkordform(name: "9", intervalle: [0, 4, 7, 10, 2], umkehrung: dom7),
        Akkordform(name: "maj9", intervalle: [0, 4, 7, 11, 2], umkehrung: [4: 1, 7: 2, 11: 3]),
        Akkordform(name: "m9", intervalle: [0, 3, 7, 10, 2], umkehrung: moll7),
        Akkordform(name: "7b9", intervalle: [0, 4, 7, 10, 1], umkehrung: dom7),
        // 7#9 enthält beide Terzen — die kleine ist die #9, nicht die Terz.
        Akkordform(name: "7#9", intervalle: [0, 4, 7, 10, 3], umkehrung: dom7),
        Akkordform(name: "9sus4", intervalle: [0, 5, 7, 10, 2], umkehrung: [7: 2, 10: 3]),
        Akkordform(name: "6/9", intervalle: [0, 4, 7, 9, 2], umkehrung: dur),
        // Undezimen und Tredezime (13 im üblichen Voicing ohne 5 und 11)
        Akkordform(name: "11", intervalle: [0, 4, 7, 10, 2, 5], umkehrung: dom7),
        Akkordform(name: "m11", intervalle: [0, 3, 7, 10, 2, 5], umkehrung: moll7),
        Akkordform(name: "13", intervalle: [0, 4, 10, 2, 9], umkehrung: [4: 1, 10: 3]),
    ]

    /// Nach de.wikipedia „Intervall (Musik)“, Halbtöne 1–11.
    private static let intervallnamen = [
        1: "kleine Sekunde", 2: "große Sekunde", 3: "kleine Terz", 4: "große Terz",
        5: "Quarte", 6: "Tritonus", 7: "Quinte", 8: "kleine Sexte",
        9: "große Sexte", 10: "kleine Septime", 11: "große Septime",
    ]

    /// Nach en.wikipedia „Interval (music)“, Halbtöne 1–11.
    private static let intervalNames = [
        1: "minor second", 2: "major second", 3: "minor third", 4: "major third",
        5: "perfect fourth", 6: "tritone", 7: "perfect fifth", 8: "minor sixth",
        9: "major sixth", 10: "minor seventh", 11: "major seventh",
    ]

    /// Englische Ordnungszahlen der Umkehrungen (nur 1–3 kommen vor).
    private static let ordinals = [1: "1st", 2: "2nd", 3: "3rd"]

    /// Gibt z. B. "C maj", "C maj/E · 1. Umk." oder bei zwei Tönen
    /// "C–E · große Terz" zurück, oder `nil`, wenn nichts erkannt wird.
    ///
    /// `bass`: Tonhöhenklasse des tatsächlich tiefsten gespielten Tons.
    /// Weicht er vom Grundton ab, folgt die Slash-Schreibweise — und, wenn
    /// Terz, Quinte oder Septime unten liegt, die Umkehrung.
    ///
    /// Viele Tonmengen lassen sich auf mehrere Grundtöne beziehen (Csus2 =
    /// Gsus4, C6/9 = D9sus4, dazu die symmetrischen aug, dim7, 7b5).
    /// Laut Wikipedia hängt der Grundton dann vom Kontext ab — der Kontext,
    /// den die App kennt, ist der Bass. Darum gewinnt, in dieser Reihenfolge:
    /// 1. die Lesart, deren Grundton im Bass liegt,
    /// 2. eine Lesart, in der der Bass Terz, Quinte oder Septime ist,
    /// 3. bei Gleichstand die Terzschichtung (Reihenfolge der Tabelle).
    /// Ohne `bass` gewinnt wie bisher der niedrigste Grundton.

    ///
    /// Notennamen wie im übrigen Projekt (siehe `noteName(forMIDI:)`): `H`
    /// im Deutschen, `B` im Englischen. Bewusst nur Kreuztöne (C#, D# …),
    /// keine b-Schreibweise: Welche Schreibweise "richtig" ist, hängt von der
    /// Tonart ab — das würde den Rahmen dieser reinen Tonhöhen-Erkennung sprengen.
    public static func name(
        forPitchClasses pitchClasses: Set<Int>, bass: Int? = nil, sprache: Sprache = .deutsch
    ) -> String? {
        let notennamen = noteNames(sprache)
        if pitchClasses.count == 2 {
            return intervall(pitchClasses, bass: bass, sprache: sprache)
        }
        guard let treffer = finde(pitchClasses, bass: bass) else { return nil }
        let basisname = "\(notennamen[treffer.grundton]) \(treffer.form.name)"
        guard let bass, bass != treffer.grundton else { return basisname }
        let slash = "\(basisname)/\(notennamen[bass])"
        let abstand = (bass - treffer.grundton + 12) % 12
        guard let umkehrung = treffer.form.umkehrung[abstand] else { return slash }
        return sprache == .deutsch
            ? "\(slash) · \(umkehrung). Umk."
            : "\(slash) · \(ordinals[umkehrung] ?? "\(umkehrung)th") inv."
    }

    private static func intervall(_ pitchClasses: Set<Int>, bass: Int?, sprache: Sprache) -> String? {
        let notennamen = noteNames(sprache)
        let namen = sprache == .deutsch ? intervallnamen : intervalNames
        guard let unten = bass ?? pitchClasses.min(),
              let oben = pitchClasses.first(where: { $0 != unten }),
              let name = namen[(oben - unten + 12) % 12] else { return nil }
        return "\(notennamen[unten])–\(notennamen[oben]) · \(name)"
    }

    private static func finde(
        _ pitchClasses: Set<Int>, bass: Int?
    ) -> (grundton: Int, form: Akkordform)? {
        guard (3...6).contains(pitchClasses.count) else { return nil }

        var kandidaten: [(grundton: Int, formIndex: Int)] = []
        for grundton in 0..<12 {
            for (index, form) in akkordformen.enumerated()
            where form.intervalle.count == pitchClasses.count {
                let geformt = Set(form.intervalle.map { (grundton + $0) % 12 })
                if geformt == pitchClasses {
                    kandidaten.append((grundton, index))
                }
            }
        }
        guard let bass else {
            // Schon nach Grundton sortiert gesammelt.
            return kandidaten.first.map { ($0.grundton, akkordformen[$0.formIndex]) }
        }

        func rang(_ k: (grundton: Int, formIndex: Int)) -> Int {
            if k.grundton == bass { return 0 }
            let abstand = (bass - k.grundton + 12) % 12
            return akkordformen[k.formIndex].umkehrung[abstand] != nil ? 1 : 2
        }
        let bester = kandidaten.min { a, b in
            (rang(a), a.formIndex, a.grundton) < (rang(b), b.formIndex, b.grundton)
        }
        return bester.map { ($0.grundton, akkordformen[$0.formIndex]) }
    }
}

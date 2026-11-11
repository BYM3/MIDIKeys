import Testing
@testable import MIDIKeysCore

@Test func durDreiklangInGrundstellung() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7]) == "C maj")
    #expect(ChordNamer.name(forPitchClasses: [2, 6, 9]) == "D maj")
}

@Test func mollDreiklangInGrundstellung() {
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7]) == "C min")
    #expect(ChordNamer.name(forPitchClasses: [9, 0, 4]) == "A min")
}

@Test func verminderterDreiklang() {
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6]) == "C dim")
}

@Test func uebermaessigerDreiklangNimmtDenNiedrigstenGrundtonAlsKonvention() {
    // Musikalisch symmetrisch: {0,4,8} lässt sich gleichberechtigt als
    // C+, E+ oder G#+ lesen. Deterministisch wird der niedrigste
    // Grundton gewählt (bewusste, dokumentierte Vereinfachung).
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 8]) == "C aug")
    #expect(ChordNamer.name(forPitchClasses: [4, 8, 0]) == "C aug")
}

@Test func erkennungIstUnabhaengigVonDerOktaveUndReihenfolge() {
    // Nur Tonhöhenklassen zählen, keine tatsächlichen MIDI-Notennummern.
    #expect(ChordNamer.name(forPitchClasses: [7, 0, 4]) == "C maj")
    #expect(ChordNamer.name(forPitchClasses: [4, 0, 7]) == "C maj")
}

@Test func jedeUmkehrungWirdErkannt() {
    // Grundstellung, erste und zweite Umkehrung liefern dieselben
    // Tonhöhenklassen — Erkennung ist umkehrungsunabhängig.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7]) == "C maj")   // Grundstellung C-E-G
    #expect(ChordNamer.name(forPitchClasses: [4, 7, 0]) == "C maj")   // 1. Umkehrung E-G-C
    #expect(ChordNamer.name(forPitchClasses: [7, 0, 4]) == "C maj")   // 2. Umkehrung G-C-E
}

@Test func zuWenigTonhoehenErgebenKeinenAkkord() {
    // Ab zwei Tönen ist der Powerchord abgedeckt — weniger als das nicht.
    #expect(ChordNamer.name(forPitchClasses: []) == nil)
    #expect(ChordNamer.name(forPitchClasses: [0]) == nil)
}

@Test func zuVieleTonhoehenErgebenKeinenAkkord() {
    // Sieben Töne (C-Dur-Tonleiter) — größer als jede erkannte Form.
    #expect(ChordNamer.name(forPitchClasses: [0, 2, 4, 5, 7, 9, 11]) == nil)
}

@Test func cmaj13OhneUndezimeIstAm11() {
    // C-E-G-H-D-A = A-C-E-G-H-D: dieselben Töne, mit A im Bass ein Am11.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 11, 2, 9], bass: 9) == "A m11")
}

@Test func dreiTonhoehenOhneErkennbaresMusterErgebenKeinenAkkord() {
    #expect(ChordNamer.name(forPitchClasses: [0, 1, 2]) == nil)
}

@Test func notennamenFolgenDerDeutschenKonventionMitH() {
    // H-Moll: H (11), D (2), Fis (6) — Halbtonabstände 3 und 7 ab H.
    #expect(ChordNamer.name(forPitchClasses: [11, 2, 6]) == "H min")
}

// MARK: - Erweiterte Akkorde

@Test func zweiToeneZeigenDasIntervallStattEinesPowerchords() {
    // Ein Powerchord ist nur eine Quinte — als Intervall benannt lernt
    // man mehr (und Terzen, Sexten usw. werden überhaupt erst sichtbar).
    #expect(ChordNamer.name(forPitchClasses: [0, 7], bass: 0) == "C–G · Quinte")
    #expect(ChordNamer.name(forPitchClasses: [7, 2], bass: 2) == "D–G · Quarte")
}

@Test func susAkkorde() {
    #expect(ChordNamer.name(forPitchClasses: [0, 2, 7]) == "C sus2")
    #expect(ChordNamer.name(forPitchClasses: [0, 5, 7]) == "C sus4")
}

@Test func sextakkordeWerdenAlsSeptakkordGelesen() {
    // D6 = H-D-F#-A (Hm7), Dm6 = H-D-F-A (Hm7b5) — der Septakkord gewinnt.
    #expect(ChordNamer.name(forPitchClasses: [2, 6, 9, 11]) == "H m7")
    #expect(ChordNamer.name(forPitchClasses: [2, 5, 9, 11]) == "H m7b5")
}

@Test func septakkorde() {
    #expect(ChordNamer.name(forPitchClasses: [7, 11, 2, 5]) == "G 7")      // Dominantseptakkord
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 11]) == "C maj7")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 10]) == "C m7")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6, 10]) == "C m7b5")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6, 9]) == "C dim7")
}

@Test func nonakkorde() {
    #expect(ChordNamer.name(forPitchClasses: [7, 11, 2, 5, 9]) == "G 9")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 11, 2]) == "C maj9")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 10, 2]) == "C m9")
}

@Test func dimSiebenIstVierfachSymmetrischNiedrigsterGrundtonGewinnt() {
    // {0,3,6,9} ist unter Transposition um kleine Terzen invariant — vier
    // gleichberechtigte Lesarten (C dim7 = Eb dim7 = Gb dim7 = A dim7).
    // Wie beim übermäßigen Dreiklang: niedrigster Grundton gewinnt.
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6, 9]) == "C dim7")
    #expect(ChordNamer.name(forPitchClasses: [3, 6, 9, 0]) == "C dim7")
    #expect(ChordNamer.name(forPitchClasses: [9, 0, 3, 6]) == "C dim7")
}

@Test func einTonAlleinErgibtKeinenAkkord() {
    #expect(ChordNamer.name(forPitchClasses: [0]) == nil)
}

@Test func sechsToeneOhneErkennbaresMusterErgebenKeinenAkkord() {
    // Sechs Töne kennen nur die Undezimen (11, m11).
    #expect(ChordNamer.name(forPitchClasses: [0, 1, 2, 3, 4, 5]) == nil)
}

@Test func sus2UndSus4KollidierenUeberDieQuinte_niedrigsterGrundtonGewinnt() {
    // C-D-G ist gleichzeitig Csus2 (C-D-G) und Gsus4 (G-C-D) — ein
    // sus4-Akkord ist strukturell ein sus2 auf seiner eigenen Quinte.
    // Durch den systematischen Kollisionscheck aller Akkordformen gefunden.
    let toene: Set<Int> = [0, 2, 7]
    #expect(ChordNamer.name(forPitchClasses: toene) == "C sus2")
}

// MARK: - Zweite Erweiterung: add9, madd9, mMaj7, 7sus4, 6/9

@Test func add9Akkorde() {
    // Cadd9 = C-E-G-D, Dur-Dreiklang plus große None, OHNE Septime
    // (Unterschied zu maj9).
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 2]) == "C add9")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 2]) == "C madd9")
}

@Test func mollMitGrosserSeptime() {
    // mMaj7: Moll-Dreiklang mit GROSSER (nicht kleiner) Septime —
    // unterscheidet sich von m7 nur in diesem einen Ton.
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 11]) == "C mMaj7")
}

@Test func siebenSus4() {
    // C7sus4 = C-F-G-Bb: sus4-Dreiklang plus kleine Septime.
    #expect(ChordNamer.name(forPitchClasses: [0, 5, 7, 10]) == "C 7sus4")
}

@Test func sechsNeunAkkord() {
    // C6/9 = C-E-G-A-D: Dur-Dreiklang plus große Sexte plus große None.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 9, 2]) == "C 6/9")
}

@Test func neueAkkordformenKollidierenMitNichtsBestehendem() {
    // Systematisch geprüft: keine der fünf neuen Formen (add9, madd9,
    // mMaj7, 7sus4, 6/9) teilt sich Töne mit irgendeiner der bisherigen
    // 17 Formen, über alle 12 Grundtöne hinweg. Als Stichprobe hier
    // exemplarisch verankert, damit ein künftiger Fund auffällt.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 2]) == "C add9")
    #expect(ChordNamer.name(forPitchClasses: [5, 9, 0, 7]) == "F add9")
}

// MARK: - Umkehrungen / Bassnote (Slash-Schreibweise)

@Test func rootPositionZeigtKeinenSlash() {
    // Bass entspricht dem Grundton -> keine Slash-Schreibweise.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7], bass: 0) == "C maj")
}

@Test func ersteUmkehrungZeigtDieBassnote() {
    // C-Dur mit E im Bass (erste Umkehrung: E-G-C).
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7], bass: 4) == "C maj/E · 1. Umk.")
}

@Test func zweiteUmkehrungZeigtDieBassnote() {
    // C-Dur mit G im Bass (zweite Umkehrung: G-C-E).
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7], bass: 7) == "C maj/G · 2. Umk.")
}

@Test func ohneAngegebeneBassnoteBleibtDasVerhaltenRueckwaertskompatibel() {
    // Kein bass-Argument -> keine Slash-Schreibweise, exakt wie vor
    // dieser Erweiterung. Alle bisherigen Aufrufstellen bleiben gültig.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7]) == "C maj")
}

@Test func slashSchreibweiseFunktioniertAuchBeiSeptakkorden() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10], bass: 4) == "C 7/E · 1. Umk.")
}

@Test func cm7MitEsImBassBleibtCm7InErsterUmkehrung() {
    // C-Eb-G-Bb = Eb-G-Bb-C (Eb6) — der Septakkord gewinnt.
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 10], bass: 3) == "C m7/D# · 1. Umk.")
}

@Test func umgedrehteQuinteIstEineQuarte() {
    // G unten, C oben: vom Bass aus gemessen eine Quarte, keine Quinte.
    #expect(ChordNamer.name(forPitchClasses: [0, 7], bass: 7) == "G–C · Quarte")
}

// MARK: - Intervalle (zwei Töne)

@Test func alleIntervalleInnerhalbDerOktaveHabenIhrenDeutschenNamen() {
    // Namen nach de.wikipedia.org/wiki/Intervall_(Musik), Halbtöne ab C.
    let erwartet = [
        1: "C–C# · kleine Sekunde", 2: "C–D · große Sekunde",
        3: "C–D# · kleine Terz", 4: "C–E · große Terz", 5: "C–F · Quarte",
        6: "C–F# · Tritonus", 7: "C–G · Quinte", 8: "C–G# · kleine Sexte",
        9: "C–A · große Sexte", 10: "C–A# · kleine Septime", 11: "C–H · große Septime",
    ]
    for (halbtoene, name) in erwartet {
        #expect(ChordNamer.name(forPitchClasses: [0, halbtoene], bass: 0) == name)
    }
}

@Test func terzenWerdenAngezeigt() {
    #expect(ChordNamer.name(forPitchClasses: [4, 7], bass: 4) == "E–G · kleine Terz")
    #expect(ChordNamer.name(forPitchClasses: [9, 1], bass: 9) == "A–C# · große Terz")
}

@Test func intervallWirdVomBassAusGemessen() {
    // E unten, C oben = kleine Sexte (Umkehrung der großen Terz).
    #expect(ChordNamer.name(forPitchClasses: [0, 4], bass: 4) == "E–C · kleine Sexte")
}

@Test func intervallOhneBassnoteMisstVomTieferenTonhoehenwert() {
    #expect(ChordNamer.name(forPitchClasses: [4, 0]) == "C–E · große Terz")
}

// MARK: - Mehrdeutige Akkorde: der Bass entscheidet

@Test func c6OderAm7_derSeptakkordGewinnt() {
    // Wikipedia (Added sixth chord): "Identifying the root depends on context."
    // Bewusste Entscheidung fürs Lernen: Septakkord-Umkehrungen sollen
    // sichtbar sein — auch dann, wenn der Sext-Akkord-Grundton unten liegt.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 9], bass: 0) == "A m7/C · 1. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 9], bass: 9) == "A m7")
}

@Test func cm6OderAm7b5_derSeptakkordGewinnt() {
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 9], bass: 0) == "A m7b5/C · 1. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 9], bass: 9) == "A m7b5")
}

@Test func sus2OderSus4_derBassEntscheidet() {
    #expect(ChordNamer.name(forPitchClasses: [0, 2, 7], bass: 0) == "C sus2")
    #expect(ChordNamer.name(forPitchClasses: [0, 2, 7], bass: 7) == "G sus4")
}

@Test func symmetrischeAkkordeWerdenNachDemBassBenannt() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 8], bass: 4) == "E aug")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6, 9], bass: 9) == "A dim7")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 6, 10], bass: 6) == "F# 7b5")
}

@Test func sechsNeunOderNeunSus4_derBassEntscheidet() {
    let toene: Set<Int> = [0, 2, 4, 7, 9]  // C D E G A
    #expect(ChordNamer.name(forPitchClasses: toene, bass: 0) == "C 6/9")
    #expect(ChordNamer.name(forPitchClasses: toene, bass: 2) == "D 9sus4")
}

@Test func ohneGrundtonImBassGewinntDieTerzschichtung() {
    // C-E-G-A mit E im Bass: weder C noch A liegt unten. Wikipedia (Root):
    // Töne als Terzschichtung ordnen — A-C-E-G ist eine, C-E-G-A nicht.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 9], bass: 4) == "A m7/E · 2. Umk.")
}

@Test func jedeFormInGrundstellungWirdGenauSoBenannt() {
    // Systematisch: alle Formen × alle 12 Grundtöne, Grundton im Bass.
    // Fängt jede künftige Kollision ab, die die Bass-Regel nicht löst.
    let namen = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "H"]
    for form in ChordNamer.akkordformen {
        for grundton in 0..<12 {
            let toene = Set(form.intervalle.map { (grundton + $0) % 12 })
            #expect(
                ChordNamer.name(forPitchClasses: toene, bass: grundton)
                    == "\(namen[grundton]) \(form.name)"
            )
        }
    }
}

// MARK: - Umkehrungen

@Test func dritteUmkehrungNurBeiSeptakkorden() {
    // Septime im Bass (Wikipedia: Inversion, figured bass 4/2).
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10], bass: 10) == "C 7/A# · 3. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10], bass: 7) == "C 7/G · 2. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 11], bass: 11) == "C maj7/H · 3. Umk.")
}

@Test func verminderterSeptakkordHatSeineSeptimeAufDerGrossenSexte() {
    // dim7 = C-Eb-Gb-Bbb: die verminderte Septime klingt wie A (9 Halbtöne).
    // Mit A im Bass greift aber die Bass-Regel (A dim7) — darum hier das
    // Gb als Quinte im Bass prüfen.
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 6, 9], bass: 6) == "F# dim7")
}

@Test func nichtAkkordtonImBassIstKeineUmkehrung() {
    // C add9 mit D im Bass: Die None ist kein Terz-/Quint-/Septton.
    #expect(ChordNamer.name(forPitchClasses: [0, 2, 4, 7], bass: 2) == "C add9/D")
}

@Test func umkehrungenOhneQuinteUndBeiSus() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 10], bass: 4) == "C 7(no5)/E · 1. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 10], bass: 10) == "C 7(no5)/A# · 3. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 5, 7, 10], bass: 5) == "C 7sus4/F")
}

// MARK: - Neue Akkordformen (Wikipedia: Chord names and symbols)

@Test func septakkordeOhneQuinte() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 10], bass: 0) == "C 7(no5)")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 11], bass: 0) == "C maj7(no5)")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 10], bass: 0) == "C m7(no5)")
}

@Test func alterierteSeptakkorde() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 8, 10], bass: 0) == "C aug7")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 8, 11], bass: 0) == "C maj7#5")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 6, 10], bass: 0) == "C 7b5")
}

@Test func alterierteUndErweiterteNonakkorde() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10, 1], bass: 0) == "C 7b9")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10, 3], bass: 0) == "C 7#9")
    #expect(ChordNamer.name(forPitchClasses: [0, 5, 7, 10, 2], bass: 0) == "C 9sus4")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 5], bass: 0) == "C add11")
}

@Test func undezimenUndTredezime() {
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10, 2, 5], bass: 0) == "C 11")
    #expect(ChordNamer.name(forPitchClasses: [0, 3, 7, 10, 2, 5], bass: 0) == "C m11")
    // Übliches Voicing ohne Quinte und ohne 11 (Wikipedia: "After the
    // fifth, the most commonly omitted note is the 11th").
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 10, 2, 9], bass: 0) == "C 13")
}

@Test func siebenKreuzNeunHatGrosseTerzAlsUmkehrungston() {
    // 7#9 enthält kleine UND große Terz — die #9 (Eb) ist keine Terz.
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10, 3], bass: 4) == "C 7#9/E · 1. Umk.")
    #expect(ChordNamer.name(forPitchClasses: [0, 4, 7, 10, 3], bass: 3) == "C 7#9/D#")
}

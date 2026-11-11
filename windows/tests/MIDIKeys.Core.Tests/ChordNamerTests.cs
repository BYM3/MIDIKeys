using static MIDIKeys.Core.Tests.Hilfen;

namespace MIDIKeys.Core.Tests;

public class ChordNamerTests
{
    [Fact]
    public void durDreiklangInGrundstellung()
    {
        Assert.Equal("C maj", ChordNamer.Name(Toene(0, 4, 7)));
        Assert.Equal("D maj", ChordNamer.Name(Toene(2, 6, 9)));
    }

    [Fact]
    public void mollDreiklangInGrundstellung()
    {
        Assert.Equal("C min", ChordNamer.Name(Toene(0, 3, 7)));
        Assert.Equal("A min", ChordNamer.Name(Toene(9, 0, 4)));
    }

    [Fact]
    public void verminderterDreiklang()
    {
        Assert.Equal("C dim", ChordNamer.Name(Toene(0, 3, 6)));
    }

    [Fact]
    public void uebermaessigerDreiklangNimmtDenNiedrigstenGrundtonAlsKonvention()
    {
        // Musikalisch symmetrisch: {0,4,8} lässt sich gleichberechtigt als
        // C+, E+ oder G#+ lesen. Deterministisch wird der niedrigste
        // Grundton gewählt (bewusste, dokumentierte Vereinfachung).
        Assert.Equal("C aug", ChordNamer.Name(Toene(0, 4, 8)));
        Assert.Equal("C aug", ChordNamer.Name(Toene(4, 8, 0)));
    }

    [Fact]
    public void erkennungIstUnabhaengigVonDerOktaveUndReihenfolge()
    {
        // Nur Tonhöhenklassen zählen, keine tatsächlichen MIDI-Notennummern.
        Assert.Equal("C maj", ChordNamer.Name(Toene(7, 0, 4)));
        Assert.Equal("C maj", ChordNamer.Name(Toene(4, 0, 7)));
    }

    [Fact]
    public void jedeUmkehrungWirdErkannt()
    {
        // Grundstellung, erste und zweite Umkehrung liefern dieselben
        // Tonhöhenklassen — Erkennung ist umkehrungsunabhängig.
        Assert.Equal("C maj", ChordNamer.Name(Toene(0, 4, 7)));  // Grundstellung C-E-G
        Assert.Equal("C maj", ChordNamer.Name(Toene(4, 7, 0)));  // 1. Umkehrung E-G-C
        Assert.Equal("C maj", ChordNamer.Name(Toene(7, 0, 4)));  // 2. Umkehrung G-C-E
    }

    [Fact]
    public void zuWenigTonhoehenErgebenKeinenAkkord()
    {
        // Ab zwei Tönen ist der Powerchord abgedeckt — weniger als das nicht.
        Assert.Null(ChordNamer.Name(Toene()));
        Assert.Null(ChordNamer.Name(Toene(0)));
    }

    [Fact]
    public void zuVieleTonhoehenErgebenKeinenAkkord()
    {
        // Sieben Töne (C-Dur-Tonleiter) — größer als jede erkannte Form.
        Assert.Null(ChordNamer.Name(Toene(0, 2, 4, 5, 7, 9, 11)));
    }

    [Fact]
    public void cmaj13OhneUndezimeIstAm11()
    {
        // C-E-G-H-D-A = A-C-E-G-H-D: dieselben Töne, mit A im Bass ein Am11.
        Assert.Equal("A m11", ChordNamer.Name(Toene(0, 4, 7, 11, 2, 9), bass: 9));
    }

    [Fact]
    public void dreiTonhoehenOhneErkennbaresMusterErgebenKeinenAkkord()
    {
        Assert.Null(ChordNamer.Name(Toene(0, 1, 2)));
    }

    [Fact]
    public void notennamenFolgenDerDeutschenKonventionMitH()
    {
        // H-Moll: H (11), D (2), Fis (6) — Halbtonabstände 3 und 7 ab H.
        Assert.Equal("H min", ChordNamer.Name(Toene(11, 2, 6)));
    }

    // MARK: - Erweiterte Akkorde

    [Fact]
    public void zweiToeneZeigenDasIntervallStattEinesPowerchords()
    {
        // Ein Powerchord ist nur eine Quinte — als Intervall benannt lernt
        // man mehr (und Terzen, Sexten usw. werden überhaupt erst sichtbar).
        Assert.Equal("C–G · Quinte", ChordNamer.Name(Toene(0, 7), bass: 0));
        Assert.Equal("D–G · Quarte", ChordNamer.Name(Toene(7, 2), bass: 2));
    }

    [Fact]
    public void susAkkorde()
    {
        Assert.Equal("C sus2", ChordNamer.Name(Toene(0, 2, 7)));
        Assert.Equal("C sus4", ChordNamer.Name(Toene(0, 5, 7)));
    }

    [Fact]
    public void sextakkordeWerdenAlsSeptakkordGelesen()
    {
        // D6 = H-D-F#-A (Hm7), Dm6 = H-D-F-A (Hm7b5) — der Septakkord gewinnt.
        Assert.Equal("H m7", ChordNamer.Name(Toene(2, 6, 9, 11)));
        Assert.Equal("H m7b5", ChordNamer.Name(Toene(2, 5, 9, 11)));
    }

    [Fact]
    public void septakkorde()
    {
        Assert.Equal("G 7", ChordNamer.Name(Toene(7, 11, 2, 5)));  // Dominantseptakkord
        Assert.Equal("C maj7", ChordNamer.Name(Toene(0, 4, 7, 11)));
        Assert.Equal("C m7", ChordNamer.Name(Toene(0, 3, 7, 10)));
        Assert.Equal("C m7b5", ChordNamer.Name(Toene(0, 3, 6, 10)));
        Assert.Equal("C dim7", ChordNamer.Name(Toene(0, 3, 6, 9)));
    }

    [Fact]
    public void nonakkorde()
    {
        Assert.Equal("G 9", ChordNamer.Name(Toene(7, 11, 2, 5, 9)));
        Assert.Equal("C maj9", ChordNamer.Name(Toene(0, 4, 7, 11, 2)));
        Assert.Equal("C m9", ChordNamer.Name(Toene(0, 3, 7, 10, 2)));
    }

    [Fact]
    public void dimSiebenIstVierfachSymmetrischNiedrigsterGrundtonGewinnt()
    {
        // {0,3,6,9} ist unter Transposition um kleine Terzen invariant — vier
        // gleichberechtigte Lesarten (C dim7 = Eb dim7 = Gb dim7 = A dim7).
        // Wie beim übermäßigen Dreiklang: niedrigster Grundton gewinnt.
        Assert.Equal("C dim7", ChordNamer.Name(Toene(0, 3, 6, 9)));
        Assert.Equal("C dim7", ChordNamer.Name(Toene(3, 6, 9, 0)));
        Assert.Equal("C dim7", ChordNamer.Name(Toene(9, 0, 3, 6)));
    }

    [Fact]
    public void einTonAlleinErgibtKeinenAkkord()
    {
        Assert.Null(ChordNamer.Name(Toene(0)));
    }

    [Fact]
    public void sechsToeneOhneErkennbaresMusterErgebenKeinenAkkord()
    {
        // Sechs Töne kennen nur die Undezimen (11, m11).
        Assert.Null(ChordNamer.Name(Toene(0, 1, 2, 3, 4, 5)));
    }

    [Fact]
    public void sus2UndSus4KollidierenUeberDieQuinte_niedrigsterGrundtonGewinnt()
    {
        // C-D-G ist gleichzeitig Csus2 (C-D-G) und Gsus4 (G-C-D) — ein
        // sus4-Akkord ist strukturell ein sus2 auf seiner eigenen Quinte.
        // Durch den systematischen Kollisionscheck aller Akkordformen gefunden.
        var toene = Toene(0, 2, 7);
        Assert.Equal("C sus2", ChordNamer.Name(toene));
    }

    // MARK: - Zweite Erweiterung: add9, madd9, mMaj7, 7sus4, 6/9

    [Fact]
    public void add9Akkorde()
    {
        // Cadd9 = C-E-G-D, Dur-Dreiklang plus große None, OHNE Septime
        // (Unterschied zu maj9).
        Assert.Equal("C add9", ChordNamer.Name(Toene(0, 4, 7, 2)));
        Assert.Equal("C madd9", ChordNamer.Name(Toene(0, 3, 7, 2)));
    }

    [Fact]
    public void mollMitGrosserSeptime()
    {
        // mMaj7: Moll-Dreiklang mit GROSSER (nicht kleiner) Septime —
        // unterscheidet sich von m7 nur in diesem einen Ton.
        Assert.Equal("C mMaj7", ChordNamer.Name(Toene(0, 3, 7, 11)));
    }

    [Fact]
    public void siebenSus4()
    {
        // C7sus4 = C-F-G-Bb: sus4-Dreiklang plus kleine Septime.
        Assert.Equal("C 7sus4", ChordNamer.Name(Toene(0, 5, 7, 10)));
    }

    [Fact]
    public void sechsNeunAkkord()
    {
        // C6/9 = C-E-G-A-D: Dur-Dreiklang plus große Sexte plus große None.
        Assert.Equal("C 6/9", ChordNamer.Name(Toene(0, 4, 7, 9, 2)));
    }

    [Fact]
    public void neueAkkordformenKollidierenMitNichtsBestehendem()
    {
        // Systematisch geprüft: keine der fünf neuen Formen (add9, madd9,
        // mMaj7, 7sus4, 6/9) teilt sich Töne mit irgendeiner der bisherigen
        // 17 Formen, über alle 12 Grundtöne hinweg. Als Stichprobe hier
        // exemplarisch verankert, damit ein künftiger Fund auffällt.
        Assert.Equal("C add9", ChordNamer.Name(Toene(0, 4, 7, 2)));
        Assert.Equal("F add9", ChordNamer.Name(Toene(5, 9, 0, 7)));
    }

    // MARK: - Umkehrungen / Bassnote (Slash-Schreibweise)

    [Fact]
    public void rootPositionZeigtKeinenSlash()
    {
        // Bass entspricht dem Grundton -> keine Slash-Schreibweise.
        Assert.Equal("C maj", ChordNamer.Name(Toene(0, 4, 7), bass: 0));
    }

    [Fact]
    public void ersteUmkehrungZeigtDieBassnote()
    {
        // C-Dur mit E im Bass (erste Umkehrung: E-G-C).
        Assert.Equal("C maj/E · 1. Umk.", ChordNamer.Name(Toene(0, 4, 7), bass: 4));
    }

    [Fact]
    public void zweiteUmkehrungZeigtDieBassnote()
    {
        // C-Dur mit G im Bass (zweite Umkehrung: G-C-E).
        Assert.Equal("C maj/G · 2. Umk.", ChordNamer.Name(Toene(0, 4, 7), bass: 7));
    }

    [Fact]
    public void ohneAngegebeneBassnoteBleibtDasVerhaltenRueckwaertskompatibel()
    {
        // Kein bass-Argument -> keine Slash-Schreibweise, exakt wie vor
        // dieser Erweiterung. Alle bisherigen Aufrufstellen bleiben gültig.
        Assert.Equal("C maj", ChordNamer.Name(Toene(0, 4, 7)));
    }

    [Fact]
    public void slashSchreibweiseFunktioniertAuchBeiSeptakkorden()
    {
        Assert.Equal("C 7/E · 1. Umk.", ChordNamer.Name(Toene(0, 4, 7, 10), bass: 4));
    }

    [Fact]
    public void cm7MitEsImBassBleibtCm7InErsterUmkehrung()
    {
        // C-Eb-G-Bb = Eb-G-Bb-C (Eb6) — der Septakkord gewinnt.
        Assert.Equal("C m7/D# · 1. Umk.", ChordNamer.Name(Toene(0, 3, 7, 10), bass: 3));
    }

    [Fact]
    public void umgedrehteQuinteIstEineQuarte()
    {
        // G unten, C oben: vom Bass aus gemessen eine Quarte, keine Quinte.
        Assert.Equal("G–C · Quarte", ChordNamer.Name(Toene(0, 7), bass: 7));
    }

    // MARK: - Intervalle (zwei Töne)

    [Fact]
    public void alleIntervalleInnerhalbDerOktaveHabenIhrenDeutschenNamen()
    {
        // Namen nach de.wikipedia.org/wiki/Intervall_(Musik), Halbtöne ab C.
        var erwartet = new Dictionary<int, string>
        {
            [1] = "C–C# · kleine Sekunde", [2] = "C–D · große Sekunde",
            [3] = "C–D# · kleine Terz", [4] = "C–E · große Terz", [5] = "C–F · Quarte",
            [6] = "C–F# · Tritonus", [7] = "C–G · Quinte", [8] = "C–G# · kleine Sexte",
            [9] = "C–A · große Sexte", [10] = "C–A# · kleine Septime", [11] = "C–H · große Septime",
        };
        foreach (var (halbtoene, name) in erwartet)
            Assert.Equal(name, ChordNamer.Name(Toene(0, halbtoene), bass: 0));
    }

    [Fact]
    public void terzenWerdenAngezeigt()
    {
        Assert.Equal("E–G · kleine Terz", ChordNamer.Name(Toene(4, 7), bass: 4));
        Assert.Equal("A–C# · große Terz", ChordNamer.Name(Toene(9, 1), bass: 9));
    }

    [Fact]
    public void intervallWirdVomBassAusGemessen()
    {
        // E unten, C oben = kleine Sexte (Umkehrung der großen Terz).
        Assert.Equal("E–C · kleine Sexte", ChordNamer.Name(Toene(0, 4), bass: 4));
    }

    [Fact]
    public void intervallOhneBassnoteMisstVomTieferenTonhoehenwert()
    {
        Assert.Equal("C–E · große Terz", ChordNamer.Name(Toene(4, 0)));
    }

    // MARK: - Mehrdeutige Akkorde: der Bass entscheidet

    [Fact]
    public void c6OderAm7_derSeptakkordGewinnt()
    {
        // Wikipedia (Added sixth chord): "Identifying the root depends on context."
        // Bewusste Entscheidung fürs Lernen: Septakkord-Umkehrungen sollen
        // sichtbar sein — auch dann, wenn der Sext-Akkord-Grundton unten liegt.
        Assert.Equal("A m7/C · 1. Umk.", ChordNamer.Name(Toene(0, 4, 7, 9), bass: 0));
        Assert.Equal("A m7", ChordNamer.Name(Toene(0, 4, 7, 9), bass: 9));
    }

    [Fact]
    public void cm6OderAm7b5_derSeptakkordGewinnt()
    {
        Assert.Equal("A m7b5/C · 1. Umk.", ChordNamer.Name(Toene(0, 3, 7, 9), bass: 0));
        Assert.Equal("A m7b5", ChordNamer.Name(Toene(0, 3, 7, 9), bass: 9));
    }

    [Fact]
    public void sus2OderSus4_derBassEntscheidet()
    {
        Assert.Equal("C sus2", ChordNamer.Name(Toene(0, 2, 7), bass: 0));
        Assert.Equal("G sus4", ChordNamer.Name(Toene(0, 2, 7), bass: 7));
    }

    [Fact]
    public void symmetrischeAkkordeWerdenNachDemBassBenannt()
    {
        Assert.Equal("E aug", ChordNamer.Name(Toene(0, 4, 8), bass: 4));
        Assert.Equal("A dim7", ChordNamer.Name(Toene(0, 3, 6, 9), bass: 9));
        Assert.Equal("F# 7b5", ChordNamer.Name(Toene(0, 4, 6, 10), bass: 6));
    }

    [Fact]
    public void sechsNeunOderNeunSus4_derBassEntscheidet()
    {
        var toene = Toene(0, 2, 4, 7, 9);  // C D E G A
        Assert.Equal("C 6/9", ChordNamer.Name(toene, bass: 0));
        Assert.Equal("D 9sus4", ChordNamer.Name(toene, bass: 2));
    }

    [Fact]
    public void ohneGrundtonImBassGewinntDieTerzschichtung()
    {
        // C-E-G-A mit E im Bass: weder C noch A liegt unten. Wikipedia (Root):
        // Töne als Terzschichtung ordnen — A-C-E-G ist eine, C-E-G-A nicht.
        Assert.Equal("A m7/E · 2. Umk.", ChordNamer.Name(Toene(0, 4, 7, 9), bass: 4));
    }

    [Fact]
    public void jedeFormInGrundstellungWirdGenauSoBenannt()
    {
        // Systematisch: alle Formen × alle 12 Grundtöne, Grundton im Bass.
        // Fängt jede künftige Kollision ab, die die Bass-Regel nicht löst.
        string[] namen = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "H"];
        foreach (var form in ChordNamer.Akkordformen)
        {
            for (var grundton = 0; grundton < 12; grundton++)
            {
                var toene = form.Intervalle.Select(i => (grundton + i) % 12).ToHashSet();
                Assert.Equal($"{namen[grundton]} {form.Name}", ChordNamer.Name(toene, bass: grundton));
            }
        }
    }

    // MARK: - Umkehrungen

    [Fact]
    public void dritteUmkehrungNurBeiSeptakkorden()
    {
        // Septime im Bass (Wikipedia: Inversion, figured bass 4/2).
        Assert.Equal("C 7/A# · 3. Umk.", ChordNamer.Name(Toene(0, 4, 7, 10), bass: 10));
        Assert.Equal("C 7/G · 2. Umk.", ChordNamer.Name(Toene(0, 4, 7, 10), bass: 7));
        Assert.Equal("C maj7/H · 3. Umk.", ChordNamer.Name(Toene(0, 4, 7, 11), bass: 11));
    }

    [Fact]
    public void verminderterSeptakkordHatSeineSeptimeAufDerGrossenSexte()
    {
        // dim7 = C-Eb-Gb-Bbb: die verminderte Septime klingt wie A (9 Halbtöne).
        // Mit A im Bass greift aber die Bass-Regel (A dim7) — darum hier das
        // Gb als Quinte im Bass prüfen.
        Assert.Equal("F# dim7", ChordNamer.Name(Toene(0, 3, 6, 9), bass: 6));
    }

    [Fact]
    public void nichtAkkordtonImBassIstKeineUmkehrung()
    {
        // C add9 mit D im Bass: Die None ist kein Terz-/Quint-/Septton.
        Assert.Equal("C add9/D", ChordNamer.Name(Toene(0, 2, 4, 7), bass: 2));
    }

    [Fact]
    public void umkehrungenOhneQuinteUndBeiSus()
    {
        Assert.Equal("C 7(no5)/E · 1. Umk.", ChordNamer.Name(Toene(0, 4, 10), bass: 4));
        Assert.Equal("C 7(no5)/A# · 3. Umk.", ChordNamer.Name(Toene(0, 4, 10), bass: 10));
        Assert.Equal("C 7sus4/F", ChordNamer.Name(Toene(0, 5, 7, 10), bass: 5));
    }

    // MARK: - Neue Akkordformen (Wikipedia: Chord names and symbols)

    [Fact]
    public void septakkordeOhneQuinte()
    {
        Assert.Equal("C 7(no5)", ChordNamer.Name(Toene(0, 4, 10), bass: 0));
        Assert.Equal("C maj7(no5)", ChordNamer.Name(Toene(0, 4, 11), bass: 0));
        Assert.Equal("C m7(no5)", ChordNamer.Name(Toene(0, 3, 10), bass: 0));
    }

    [Fact]
    public void alterierteSeptakkorde()
    {
        Assert.Equal("C aug7", ChordNamer.Name(Toene(0, 4, 8, 10), bass: 0));
        Assert.Equal("C maj7#5", ChordNamer.Name(Toene(0, 4, 8, 11), bass: 0));
        Assert.Equal("C 7b5", ChordNamer.Name(Toene(0, 4, 6, 10), bass: 0));
    }

    [Fact]
    public void alterierteUndErweiterteNonakkorde()
    {
        Assert.Equal("C 7b9", ChordNamer.Name(Toene(0, 4, 7, 10, 1), bass: 0));
        Assert.Equal("C 7#9", ChordNamer.Name(Toene(0, 4, 7, 10, 3), bass: 0));
        Assert.Equal("C 9sus4", ChordNamer.Name(Toene(0, 5, 7, 10, 2), bass: 0));
        Assert.Equal("C add11", ChordNamer.Name(Toene(0, 4, 7, 5), bass: 0));
    }

    [Fact]
    public void undezimenUndTredezime()
    {
        Assert.Equal("C 11", ChordNamer.Name(Toene(0, 4, 7, 10, 2, 5), bass: 0));
        Assert.Equal("C m11", ChordNamer.Name(Toene(0, 3, 7, 10, 2, 5), bass: 0));
        // Übliches Voicing ohne Quinte und ohne 11 (Wikipedia: "After the
        // fifth, the most commonly omitted note is the 11th").
        Assert.Equal("C 13", ChordNamer.Name(Toene(0, 4, 10, 2, 9), bass: 0));
    }

    [Fact]
    public void siebenKreuzNeunHatGrosseTerzAlsUmkehrungston()
    {
        // 7#9 enthält kleine UND große Terz — die #9 (Eb) ist keine Terz.
        Assert.Equal("C 7#9/E · 1. Umk.", ChordNamer.Name(Toene(0, 4, 7, 10, 3), bass: 4));
        Assert.Equal("C 7#9/D#", ChordNamer.Name(Toene(0, 4, 7, 10, 3), bass: 3));
    }
}

import Testing
@testable import MIDIKeysCore

/// Vollständiger Rundlauf: Jede Akkordform auf jedem Grundton mit jedem
/// Akkordton im Bass muss einen Namen liefern, der — zurück in Töne
/// übersetzt — genau die gespielten Töne ergibt und den Bass richtig nennt.
/// In beiden Sprachen — Englisch unterscheidet sich nur im Notennamen
/// (B statt H) und im Wortlaut der Umkehrung.
@Test(arguments: Sprache.allCases)
func jederNameBeschreibtGenauDieGespieltenToene(sprache: Sprache) {
    let noten = noteNames(sprache)
    var geprueft = 0
    for form in ChordNamer.akkordformen {
        for grundton in 0..<12 {
            let toene = Set(form.intervalle.map { (grundton + $0) % 12 })
            for bass in toene {
                guard let name = ChordNamer.name(forPitchClasses: toene, bass: bass, sprache: sprache) else {
                    Issue.record("kein Name für \(noten[grundton]) \(form.name) über \(noten[bass])")
                    continue
                }
                // "C maj7/E · 1. Umk." -> Grundton "C", Form "maj7", Bass "E"
                let haupt = name.components(separatedBy: " · ")[0]
                let teile = haupt.split(separator: " ", maxSplits: 1).map(String.init)
                var formName = teile[1]
                var genannterBass = teile[0]
                // Der Slash-Bass ist der letzte "/Note"-Teil ("6/9" enthält selbst einen Slash).
                if let slash = formName.lastIndex(of: "/"),
                   noten.contains(String(formName[formName.index(after: slash)...])) {
                    genannterBass = String(formName[formName.index(after: slash)...])
                    formName = String(formName[..<slash])
                }
                guard let g = noten.firstIndex(of: teile[0]),
                      let f = ChordNamer.akkordformen.first(where: { $0.name == formName }) else {
                    Issue.record("unlesbarer Name: \(name)")
                    continue
                }
                #expect(Set(f.intervalle.map { (g + $0) % 12 }) == toene, "\(name) passt nicht zu den Tönen")
                #expect(genannterBass == noten[bass], "\(name): falscher Bass, erwartet \(noten[bass])")
                // Liegt der eigene Grundton im Bass, gewinnt eine Lesart ohne Slash.
                if bass == grundton { #expect(!name.contains(" · "), "\(name): Grundstellung mit Umkehrung") }
                geprueft += 1
            }
        }
    }
    #expect(geprueft == ChordNamer.akkordformen.reduce(0) { $0 + $1.intervalle.count } * 12)
}

/// Jedes der 11 Intervalle auf jedem Ton, in beiden Sprachen: Die Töne
/// stehen in der Reihenfolge unten–oben, und der Intervallname passt.
@Test(arguments: Sprache.allCases)
func jedesIntervallWirdRichtigBenannt(sprache: Sprache) {
    let noten = noteNames(sprache)
    let deutsch = ["kleine Sekunde", "große Sekunde", "kleine Terz", "große Terz", "Quarte", "Tritonus",
                   "Quinte", "kleine Sexte", "große Sexte", "kleine Septime", "große Septime"]
    let english = ["minor second", "major second", "minor third", "major third", "perfect fourth", "tritone",
                   "perfect fifth", "minor sixth", "major sixth", "minor seventh", "major seventh"]
    let namen = sprache == .deutsch ? deutsch : english
    for unten in 0..<12 {
        for abstand in 1...11 {
            let oben = (unten + abstand) % 12
            #expect(ChordNamer.name(forPitchClasses: [unten, oben], bass: unten, sprache: sprache)
                == "\(noten[unten])–\(noten[oben]) · \(namen[abstand - 1])")
        }
    }
}

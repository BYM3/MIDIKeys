import MIDIKeysCore
import SwiftUI

/// Zeichnet einen zusammenhängenden Ausschnitt der Klaviatur (per
/// `semitoneRange`) mit Tastenbeschriftung und Notennamen. Für lange
/// Layouts (Adv., 38 Tasten) wird dieselbe View zweimal mit unterschiedlichen
/// Bereichen gestapelt statt einmal in voller Breite gezeichnet — siehe
/// `CheatsheetView`.
///
/// Weiße und schwarze Tasten liegen räumlich korrekt: Die schwarzen sitzen
/// zwischen den weißen, so wie auf der echten Tastatur. `semitoneRange` muss
/// an einer Stelle ohne schwarze Taste enden/beginnen (E–F oder H–C), sonst
/// verliert die letzte schwarze Taste ihre benachbarte weiße Taste und wird
/// falsch positioniert.
struct PianoDiagram: View {
    let keyboard: Keyboard
    /// Gerade gedrückte Keycodes — diese Tasten werden hervorgehoben.
    let pressed: Set<UInt16>
    /// Halbton -> Keycode, vollständige Tabelle (nicht nur der hier
    /// gezeichnete Ausschnitt) — `semitoneRange` filtert direkt hinein.
    /// Ein Dictionary statt eines lückenlosen Arrays, weil eine Tastenbelegung
    /// (Adv.-Layout) nicht jeden Halbton zwingend über eine eigene Position
    /// abdecken muss.
    let noteKeycodes: [Int: UInt16]
    let semitoneRange: ClosedRange<Int>
    /// Löst einen Keycode in seine Bedeutung auf (Adv.-Layout hat pro
    /// Halbton bis zu zwei Tasten — `noteKeycodes` kennt nur die
    /// kanonische; fürs Hervorheben müssen aber BEIDE erkannt werden,
    /// sonst leuchtet die Kachel bei der zweiten Taste nicht auf, obwohl
    /// die Note korrekt klingt).
    let keyAction: (UInt16) -> KeyAction?
    /// Für die Notennamen: H im Deutschen, B im Englischen.
    let sprache: Sprache

    private var weisseHalbtoene: [Int] {
        semitoneRange.filter { noteKeycodes[$0] != nil && !KeyMap.isBlackKey(semitone: $0) }
    }
    private var schwarzeHalbtoene: [Int] {
        semitoneRange.filter { noteKeycodes[$0] != nil && KeyMap.isBlackKey(semitone: $0) }
    }

    private let weisseBreite: CGFloat = 26
    private let weisseHoehe: CGFloat = 74
    private let schwarzeBreite: CGFloat = 17
    private let schwarzeHoehe: CGFloat = 44

    var body: some View {
        ZStack(alignment: .topLeading) {
            HStack(spacing: 2) {
                ForEach(weisseHalbtoene, id: \.self) { halbton in
                    taste(halbton: halbton, breite: weisseBreite, hoehe: weisseHoehe, schwarz: false)
                }
            }
            ForEach(schwarzeHalbtoene, id: \.self) { halbton in
                taste(halbton: halbton, breite: schwarzeBreite, hoehe: schwarzeHoehe, schwarz: true)
                    .offset(x: versatz(fuer: halbton))
            }
        }
        .frame(height: weisseHoehe)
    }

    /// Eine schwarze Taste sitzt an der Fuge rechts der weißen Taste unter ihr.
    private func versatz(fuer halbton: Int) -> CGFloat {
        let weisseLinks = weisseHalbtoene.filter { $0 < halbton }.count
        let fuge = CGFloat(weisseLinks) * (weisseBreite + 2)
        return fuge - schwarzeBreite / 2 - 1
    }

    private func taste(halbton: Int, breite: CGFloat, hoehe: CGFloat, schwarz: Bool) -> some View {
        let keycode = noteKeycodes[halbton]!
        let note = Int(keyboard.note(forSemitone: halbton))
        let gedrueckt = pressed.contains { gehaltenerKeycode in
            if case .note(let s) = keyAction(gehaltenerKeycode) { return s == halbton }
            return false
        }

        let fuellung: AnyShapeStyle = gedrueckt
            ? AnyShapeStyle(Color.accentColor)
            : (schwarz ? AnyShapeStyle(.thickMaterial) : AnyShapeStyle(.quaternary))

        return VStack(spacing: 2) {
            Spacer(minLength: 0)
            Text(KeyLabelResolver.label(for: keycode))
                .font(.system(size: 13, weight: .semibold, design: .rounded))
            Text(noteName(forMIDI: note, sprache: sprache))
                .font(.system(size: 8))
                .opacity(gedrueckt ? 0.85 : 1)
        }
        .foregroundStyle(gedrueckt ? AnyShapeStyle(.white) : AnyShapeStyle(.primary))
        .padding(.bottom, 5)
        .frame(width: breite, height: hoehe)
        .background(
            RoundedRectangle(cornerRadius: 4)
                .fill(fuellung)
                .overlay(RoundedRectangle(cornerRadius: 4).stroke(.separator, lineWidth: 0.5))
        )
        .animation(.easeOut(duration: 0.08), value: gedrueckt)
    }
}

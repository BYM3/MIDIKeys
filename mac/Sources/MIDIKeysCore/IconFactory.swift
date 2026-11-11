import AppKit

/// Die beiden Menüleisten-Symbole, programmatisch gezeichnet.
///
/// Als Template-Images: macOS färbt sie selbst passend zu Hell-/Dunkelmodus
/// und getönter Menüleiste ein. Deshalb wird nur eine Silhouette gezeichnet,
/// keine Farben.
public enum IconFactory {
    public static let size = NSSize(width: 18, height: 18)

    public static func icon(active: Bool) -> NSImage {
        let image = NSImage(size: size, flipped: false) { rect in
            let strichbreite: CGFloat = 1.3
            let kappe = rect.insetBy(dx: 1.5, dy: 1.0)

            // Die Tastenkappe als Umriss — gemeinsam für beide Zustände.
            let umriss = NSBezierPath(roundedRect: kappe, xRadius: 3.5, yRadius: 3.5)
            umriss.lineWidth = strichbreite
            NSColor.black.setStroke()
            umriss.stroke()

            if active {
                // Die Klaviatur braucht jede verfügbare Breite: bei 18 pt bleiben
                // sonst keine 9 px für fünf Tasten.
                zeichneKlaviatur(in: kappe.insetBy(dx: 2.2, dy: 3.4))
            } else {
                zeichneBuchstabeA(in: kappe.insetBy(dx: 3.0, dy: 3.0))
            }
            return true
        }
        image.isTemplate = true
        return image
    }

    /// Zwei schwarze Tasten hängen von oben herab; darunter trennen zwei
    /// dünne Stege die drei weißen Tasten. Stege und schwarze Tasten dürfen
    /// sich nicht überlappen, sonst verschmelzen sie zu zwei dicken Balken.
    private static func zeichneKlaviatur(in rect: NSRect) {
        NSColor.black.setFill()

        let weisseBreite = rect.width / 3
        let schwarzHoehe = rect.height * 0.52
        let schwarzBreite = weisseBreite * 0.66
        let obergrenze = rect.maxY - schwarzHoehe

        let trennerBreite: CGFloat = 0.8
        for i in 1..<3 {
            let x = rect.minX + weisseBreite * CGFloat(i) - trennerBreite / 2
            NSRect(
                x: x, y: rect.minY,
                width: trennerBreite, height: obergrenze - rect.minY
            ).fill()
        }

        for i in 1..<3 {
            let mitte = rect.minX + weisseBreite * CGFloat(i)
            NSRect(
                x: mitte - schwarzBreite / 2, y: obergrenze,
                width: schwarzBreite, height: schwarzHoehe
            ).fill()
        }
    }

    private static func zeichneBuchstabeA(in rect: NSRect) {
        let schrift = NSFont.systemFont(ofSize: rect.height * 0.95, weight: .semibold)
        let attribute: [NSAttributedString.Key: Any] = [
            .font: schrift,
            .foregroundColor: NSColor.black,
        ]
        let text = NSAttributedString(string: "A", attributes: attribute)
        let groesse = text.size()
        text.draw(at: NSPoint(
            x: rect.midX - groesse.width / 2,
            y: rect.midY - groesse.height / 2
        ))
    }
}

import AppKit
import MIDIKeysCore

// Entwicklerhilfe: Icons als PNG rendern und beenden.
if CommandLine.arguments.contains("--render-icons") {
    for (name, aktiv) in [("inaktiv", false), ("aktiv", true)] {
        for (variante, hintergrund, vordergrund) in [
            ("hell", NSColor.white, NSColor.black),
            ("dunkel", NSColor.black, NSColor.white),
        ] {
            let skala: CGFloat = 8
            let kante = Int(18 * skala)
            let rep = NSBitmapImageRep(
                bitmapDataPlanes: nil, pixelsWide: kante, pixelsHigh: kante,
                bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
                isPlanar: false, colorSpaceName: .deviceRGB,
                bytesPerRow: 0, bitsPerPixel: 0
            )!
            NSGraphicsContext.saveGraphicsState()
            NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
            hintergrund.setFill()
            NSRect(x: 0, y: 0, width: kante, height: kante).fill()
            let icon = IconFactory.icon(active: aktiv)
            icon.isTemplate = false
            let getoent = NSImage(size: icon.size, flipped: false) { rect in
                icon.draw(in: rect)
                vordergrund.set()
                rect.fill(using: .sourceAtop)
                return true
            }
            getoent.draw(in: NSRect(x: 0, y: 0, width: kante, height: kante))
            NSGraphicsContext.restoreGraphicsState()
            let pfad = "/tmp/midikeys-\(name)-\(variante).png"
            try? rep.representation(using: .png, properties: [:])?
                .write(to: URL(fileURLWithPath: pfad))
            print(pfad)
        }
    }

    // Kontaktbogen: beide Zustände, beide Themes, vergrößert und in Originalgröße.
    let zelle = 160, rand = 14
    let bogenB = zelle * 2 + rand * 3
    let bogenH = zelle + rand * 2 + 46
    let bogen = NSBitmapImageRep(
        bitmapDataPlanes: nil, pixelsWide: bogenB, pixelsHigh: bogenH,
        bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
        isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
    )!
    NSGraphicsContext.saveGraphicsState()
    NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: bogen)
    NSColor(white: 0.5, alpha: 1).setFill()
    NSRect(x: 0, y: 0, width: bogenB, height: bogenH).fill()

    func male(_ aktiv: Bool, _ hell: Bool, _ frame: NSRect) {
        (hell ? NSColor.white : NSColor(white: 0.13, alpha: 1)).setFill()
        frame.fill()
        let icon = IconFactory.icon(active: aktiv)
        icon.isTemplate = false
        let vg = hell ? NSColor.black : NSColor.white
        let getoent = NSImage(size: icon.size, flipped: false) { r in
            icon.draw(in: r); vg.set(); r.fill(using: .sourceAtop); return true
        }
        // groß
        let g = min(frame.width, frame.height) - 30
        getoent.draw(in: NSRect(x: frame.midX - g/2, y: frame.midY - g/2 + 8, width: g, height: g))
        // Originalgröße 18pt
        getoent.draw(in: NSRect(x: frame.midX - 9, y: frame.minY + 6, width: 18, height: 18))
    }

    male(false, true,  NSRect(x: rand, y: rand + 46, width: zelle, height: zelle))
    male(true,  true,  NSRect(x: rand*2 + zelle, y: rand + 46, width: zelle, height: zelle))
    male(false, false, NSRect(x: rand, y: rand, width: zelle, height: 46))
    male(true,  false, NSRect(x: rand*2 + zelle, y: rand, width: zelle, height: 46))
    NSGraphicsContext.restoreGraphicsState()
    try? bogen.representation(using: .png, properties: [:])?
        .write(to: URL(fileURLWithPath: "/tmp/midikeys-icons.png"))
    print("/tmp/midikeys-icons.png")
    exit(0)
}


let app = NSApplication.shared
let delegate = AppDelegate()
app.delegate = delegate
// Kein Dock-Icon, kein Menü — die App lebt nur in der Menüleiste.
app.setActivationPolicy(.accessory)
app.run()

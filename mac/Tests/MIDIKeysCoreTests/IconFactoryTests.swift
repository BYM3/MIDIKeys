import AppKit
import Testing
@testable import MIDIKeysCore

@Test @MainActor func beideIconsHabenMenueleistenGroesse() {
    for aktiv in [true, false] {
        #expect(IconFactory.icon(active: aktiv).size == IconFactory.size)
    }
}

@Test @MainActor func beideIconsSindTemplates() {
    // Nur so passen sie sich Hell-/Dunkelmodus und getönter Menüleiste an.
    for aktiv in [true, false] {
        #expect(IconFactory.icon(active: aktiv).isTemplate == true)
    }
}

@Test @MainActor func iconsSindNichtLeer() {
    // Ein leeres Bild würde als unsichtbares Menüleisten-Symbol durchgehen.
    for aktiv in [true, false] {
        let icon = IconFactory.icon(active: aktiv)
        let rep = NSBitmapImageRep(
            bitmapDataPlanes: nil, pixelsWide: 36, pixelsHigh: 36,
            bitsPerSample: 8, samplesPerPixel: 4, hasAlpha: true,
            isPlanar: false, colorSpaceName: .deviceRGB, bytesPerRow: 0, bitsPerPixel: 0
        )!
        NSGraphicsContext.saveGraphicsState()
        NSGraphicsContext.current = NSGraphicsContext(bitmapImageRep: rep)
        icon.draw(in: NSRect(x: 0, y: 0, width: 36, height: 36))
        NSGraphicsContext.restoreGraphicsState()

        var sichtbarePixel = 0
        for x in 0..<36 {
            for y in 0..<36 where (rep.colorAt(x: x, y: y)?.alphaComponent ?? 0) > 0.1 {
                sichtbarePixel += 1
            }
        }
        #expect(sichtbarePixel > 50)
    }
}

@Test @MainActor func dieBeidenIconsUnterscheidenSich() {
    let inaktiv = IconFactory.icon(active: false).tiffRepresentation
    let aktiv = IconFactory.icon(active: true).tiffRepresentation
    #expect(inaktiv != aktiv)
}

import Testing
@testable import MIDIKeysCore

private let ctrl = CGEventFlagsRaw.control
private let opt = CGEventFlagsRaw.option
private let cmd = CGEventFlagsRaw.command
private let standardHotkey = Hotkey.default  // ⌥-

private func decide(
    _ keycode: UInt16, flags: UInt64 = 0, down: Bool = true,
    repeated: Bool = false, active: Bool
) -> Decision {
    TapPolicy.decide(
        keycode: keycode, flags: flags, isKeyDown: down,
        isRepeat: repeated, isActive: active, hotkey: standardHotkey
    )
}

// MARK: - Der Not-Aus

@Test func cmdKombinationenGehenImmerDurch() {
    // Das ist die Lebensversicherung: Cmd+Opt+Esc muss den Force-Quit-Dialog
    // erreichen, auch wenn die App gerade die ganze Tastatur schluckt.
    #expect(decide(0x35, flags: cmd | opt, active: true) == .passThrough)  // Escape
    #expect(decide(0x30, flags: cmd, active: true) == .passThrough)        // Cmd+Tab
    #expect(decide(0x0C, flags: cmd, active: true) == .passThrough)        // Cmd+Q
    // Auch eine gemappte Notentaste darf mit Cmd nicht geschluckt werden.
    #expect(decide(0x00, flags: cmd, active: true) == .passThrough)        // Cmd+A
}

@Test func hotkeySchlaegtCmdRegelUmSelbstMitCmdZuFunktionieren() {
    // Ein Shortcut, der Cmd enthält, muss trotzdem als Toggle erkannt werden.
    let cmdHotkey = Hotkey(keycode: 0x2E, modifiers: cmd)
    let entscheidung = TapPolicy.decide(
        keycode: 0x2E, flags: cmd, isKeyDown: true,
        isRepeat: false, isActive: true, hotkey: cmdHotkey
    )
    #expect(entscheidung == .toggle)
}

// MARK: - Inaktiver Modus

@Test func imInaktivenModusGehtAllesDurch() {
    #expect(decide(0x00, active: false) == .passThrough)   // a
    #expect(decide(0x30, active: false) == .passThrough)   // Tab
    #expect(decide(0x06, active: false) == .passThrough)   // y
}

@Test func derHotkeyWirktInBeidenModi() {
    #expect(decide(0x2C, flags: opt, active: false) == .toggle)
    #expect(decide(0x2C, flags: opt, active: true) == .toggle)
}

@Test func dasLoslassenDesHotkeysWirdGeschlucktOhneZweitesUmschalten() {
    // Sonst würde einmal Drücken zweimal umschalten.
    #expect(decide(0x2C, flags: opt, down: false, active: true) == .swallow)
    #expect(decide(0x2C, flags: opt, down: false, active: false) == .swallow)
}

// MARK: - Aktiver Modus

@Test func notentastenErzeugenNoten() {
    #expect(decide(0x00, active: true) == .noteOn(semitone: 0))
    #expect(decide(0x00, down: false, active: true) == .noteOff(semitone: 0))
    #expect(decide(0x2A, active: true) == .noteOn(semitone: 19))
}

@Test func steuertastenWirkenNurBeimDruecken() {
    #expect(decide(0x06, active: true) == .octave(-1))
    #expect(decide(0x07, active: true) == .octave(1))
    #expect(decide(0x06, down: false, active: true) == .swallow)
    #expect(decide(0x07, down: false, active: true) == .swallow)
}

@Test func tabSchaltetSustain() {
    #expect(decide(0x30, active: true) == .sustain(true))
    #expect(decide(0x30, down: false, active: true) == .sustain(false))
}

@Test func nichtGemappteTastenWerdenGeschluckt() {
    // Der Nutzer hat "ganze Tastatur schlucken" gewählt.
    #expect(decide(0x24, active: true) == .swallow)  // Return
    #expect(decide(0x35, active: true) == .swallow)  // Escape ohne Cmd
}

@Test func pfeiltastenGehenImAktivenModusDurch() {
    // Damit ein Programm im Hintergrund (z. B. die DAW) weiter bedienbar
    // bleibt, während die App spielt.
    for pfeil: UInt16 in [0x7B, 0x7C, 0x7D, 0x7E] {  // ← → ↓ ↑
        #expect(decide(pfeil, active: true) == .passThrough)
        #expect(decide(pfeil, down: false, active: true) == .passThrough)
        // Gehalten muss es weiterscrollen — Autorepeat also auch durchlassen.
        #expect(decide(pfeil, repeated: true, active: true) == .passThrough)
        // Auch mit Shift/Option (Markieren, wortweise springen).
        #expect(decide(pfeil, flags: CGEventFlagsRaw.shift | opt, active: true) == .passThrough)
    }
}

@Test func leertasteGehtImAktivenModusDurch() {
    // Play/Stop der DAW im Hintergrund, ohne den Modus zu verlassen.
    #expect(decide(0x31, active: true) == .passThrough)
    #expect(decide(0x31, down: false, active: true) == .passThrough)
    #expect(decide(0x31, repeated: true, active: true) == .passThrough)
    #expect(decide(0x31, flags: CGEventFlagsRaw.shift | opt, active: true) == .passThrough)
}

@Test func autorepeatErzeugtKeinZweitesNoteOn() {
    // Eine gehaltene Taste würde sonst zehn Note-Ons pro Sekunde feuern.
    #expect(decide(0x00, repeated: true, active: true) == .swallow)
    #expect(decide(0x30, repeated: true, active: true) == .swallow)
    #expect(decide(0x06, repeated: true, active: true) == .swallow)
}

// MARK: - Adv.-Layout (via keyAction-Parameter)

@Test func advLayoutErkenntSeineEigenenNotentasten() {
    // '<' ist im Adv.-Layout C3, im Grundlayout dagegen gar nicht gemappt.
    let entscheidung = TapPolicy.decide(
        keycode: 0x32, flags: 0, isKeyDown: true, isRepeat: false,
        isActive: true, hotkey: Hotkey.default, keyAction: AdvancedKeyMap.action(for:)
    )
    #expect(entscheidung == .noteOn(semitone: 0))
}

@Test func advLayoutHatEigeneOktavtasten() {
    let ab = TapPolicy.decide(
        keycode: AdvancedKeyMap.octaveDownKeycode, flags: 0, isKeyDown: true, isRepeat: false,
        isActive: true, hotkey: Hotkey.default, keyAction: AdvancedKeyMap.action(for:)
    )
    #expect(ab == .octave(-1))
    let auf = TapPolicy.decide(
        keycode: AdvancedKeyMap.octaveUpKeycode, flags: 0, isKeyDown: true, isRepeat: false,
        isActive: true, hotkey: Hotkey.default, keyAction: AdvancedKeyMap.action(for:)
    )
    #expect(auf == .octave(1))
}

@Test func ohneKeyActionParameterBleibtDasGrundlayoutAktiv() {
    // Rückwärtskompatibel: 'a' bedeutet ohne den neuen Parameter weiterhin C3.
    let entscheidung = TapPolicy.decide(
        keycode: 0x00, flags: 0, isKeyDown: true, isRepeat: false,
        isActive: true, hotkey: Hotkey.default
    )
    #expect(entscheidung == .noteOn(semitone: 0))
}

@Test func minusOhneOptionSpieltImAdvLayoutWeiterF4() {
    // Der Standard-Hotkey ⌥- teilt sich die Taste mit F4 — nur mit ⌥ schaltet sie.
    let entscheidung = TapPolicy.decide(
        keycode: 0x2C, flags: 0, isKeyDown: true, isRepeat: false,
        isActive: true, hotkey: Hotkey.default, keyAction: AdvancedKeyMap.action(for:)
    )
    #expect(entscheidung == .noteOn(semitone: 17))
}

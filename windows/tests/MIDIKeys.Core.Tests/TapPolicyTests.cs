using static MIDIKeys.Core.ScanCodes;

namespace MIDIKeys.Core.Tests;

public class TapPolicyTests
{
    private const Mods ctrl = Mods.Ctrl;
    private const Mods alt = Mods.Alt;   // Mac: ⌥
    private const Mods win = Mods.Win;   // Mac: ⌘
    private static readonly Hotkey standardHotkey = Hotkey.Default;  // Alt+-

    private static Decision Decide(
        ushort scancode, Mods mods = Mods.None, bool down = true, bool repeated = false, bool active = false) =>
        TapPolicy.Decide(scancode, mods, down, repeated, active, standardHotkey);

    // MARK: - Der Not-Aus

    [Fact]
    public void winKombinationenGehenImmerDurch()
    {
        // Mac: Cmd-Kombinationen. Windows: Win spielt die Rolle von ⌘ —
        // Win+Alt+Esc, Win+Tab, Win+Q und auch Win+Notentaste gehen durch.
        Assert.Equal(Decision.PassThrough, Decide(Escape, win | alt, active: true));
        Assert.Equal(Decision.PassThrough, Decide(Tab, win, active: true));
        Assert.Equal(Decision.PassThrough, Decide(Q, win, active: true));
        // Auch eine gemappte Notentaste darf mit Win nicht geschluckt werden.
        Assert.Equal(Decision.PassThrough, Decide(A, win, active: true));
    }

    [Fact]
    public void strgKombinationenGehenImmerDurch()
    {
        // Windows: Strg+Umschalt+Esc (Task-Manager) ist die Lebensversicherung;
        // Strg+C/V/Z und Strg+Notentaste bleiben ebenfalls nutzbar.
        Assert.Equal(Decision.PassThrough, Decide(Escape, ctrl | Mods.Shift, active: true));
        Assert.Equal(Decision.PassThrough, Decide(C, ctrl, active: true));
        Assert.Equal(Decision.PassThrough, Decide(A, ctrl, active: true));
        Assert.Equal(Decision.PassThrough, Decide(A, ctrl, down: false, active: true));
        Assert.Equal(Decision.PassThrough, Decide(A, ctrl | alt, active: true));  // auch AltGr (= Strg+Alt)
    }

    [Fact]
    public void altSystemkombinationenGehenImmerDurch()
    {
        // Windows: Alt+Tab, Alt+F4 und Alt+Esc — Fenster wechseln und schließen bleibt möglich.
        foreach (var taste in new[] { Tab, F4, Escape })
        {
            Assert.Equal(Decision.PassThrough, Decide(taste, alt, active: true));
            Assert.Equal(Decision.PassThrough, Decide(taste, alt, down: false, active: true));
            Assert.Equal(Decision.PassThrough, Decide(taste, alt | Mods.Shift, active: true));  // Alt+Umschalt+Tab
        }
        // Andere Alt-Kombinationen werden im aktiven Modus normal behandelt.
        Assert.Equal(Decision.NoteOn(0), Decide(A, alt, active: true));
    }

    [Fact]
    public void modifierTastenGehenImmerDurch()
    {
        // Windows: Der Hook sieht die Modifier als eigene Tasten (auf dem Mac
        // erreichen sie den Tap gar nicht als keyDown).
        foreach (var taste in new[] { LeftShift, RightShift, LeftCtrl, RightCtrl, LeftAlt, RightAlt, LeftWin, RightWin, CapsLock })
        {
            Assert.Equal(Decision.PassThrough, Decide(taste, active: true));
            Assert.Equal(Decision.PassThrough, Decide(taste, down: false, active: true));
            Assert.Equal(Decision.PassThrough, Decide(taste, repeated: true, active: true));
            Assert.Equal(Decision.PassThrough, Decide(taste, alt, active: true));
        }
    }

    [Fact]
    public void hotkeySchlaegtWinRegelUmSelbstMitWinZuFunktionieren()
    {
        // Ein Shortcut, der Win enthält, muss trotzdem als Toggle erkannt werden.
        var winHotkey = new Hotkey(M, win);
        var entscheidung = TapPolicy.Decide(M, win, isKeyDown: true, isRepeat: false, isActive: true, hotkey: winHotkey);
        Assert.Equal(Decision.Toggle, entscheidung);
    }

    [Fact]
    public void hotkeySchlaegtStrgRegelUmSelbstMitStrgZuFunktionieren()
    {
        var strgHotkey = new Hotkey(M, ctrl | alt);
        var entscheidung = TapPolicy.Decide(M, ctrl | alt, isKeyDown: true, isRepeat: false, isActive: true, hotkey: strgHotkey);
        Assert.Equal(Decision.Toggle, entscheidung);
    }

    // MARK: - Inaktiver Modus

    [Fact]
    public void imInaktivenModusGehtAllesDurch()
    {
        Assert.Equal(Decision.PassThrough, Decide(A, active: false));    // a
        Assert.Equal(Decision.PassThrough, Decide(Tab, active: false));  // Tab
        Assert.Equal(Decision.PassThrough, Decide(Z, active: false));    // y
    }

    [Fact]
    public void derHotkeyWirktInBeidenModi()
    {
        Assert.Equal(Decision.Toggle, Decide(Slash, alt, active: false));
        Assert.Equal(Decision.Toggle, Decide(Slash, alt, active: true));
    }

    [Fact]
    public void dasLoslassenDesHotkeysWirdGeschlucktOhneZweitesUmschalten()
    {
        // Sonst würde einmal Drücken zweimal umschalten.
        Assert.Equal(Decision.Swallow, Decide(Slash, alt, down: false, active: true));
        Assert.Equal(Decision.Swallow, Decide(Slash, alt, down: false, active: false));
    }

    // MARK: - Aktiver Modus

    [Fact]
    public void notentastenErzeugenNoten()
    {
        Assert.Equal(Decision.NoteOn(0), Decide(A, active: true));
        Assert.Equal(Decision.NoteOff(0), Decide(A, down: false, active: true));
        Assert.Equal(Decision.NoteOn(19), Decide(Backslash, active: true));
    }

    [Fact]
    public void steuertastenWirkenNurBeimDruecken()
    {
        Assert.Equal(Decision.Octave(-1), Decide(Z, active: true));
        Assert.Equal(Decision.Octave(1), Decide(X, active: true));
        Assert.Equal(Decision.Swallow, Decide(Z, down: false, active: true));
        Assert.Equal(Decision.Swallow, Decide(X, down: false, active: true));
    }

    [Fact]
    public void tabSchaltetSustain()
    {
        Assert.Equal(Decision.Sustain(true), Decide(Tab, active: true));
        Assert.Equal(Decision.Sustain(false), Decide(Tab, down: false, active: true));
    }

    [Fact]
    public void nichtGemappteTastenWerdenGeschluckt()
    {
        // Der Nutzer hat "ganze Tastatur schlucken" gewählt.
        Assert.Equal(Decision.Swallow, Decide(Enter, active: true));   // Return
        Assert.Equal(Decision.Swallow, Decide(Escape, active: true));  // Escape ohne Win/Strg/Alt
    }

    [Fact]
    public void pfeiltastenGehenImAktivenModusDurch()
    {
        // Damit ein Programm im Hintergrund (z. B. die DAW) weiter bedienbar
        // bleibt, während die App spielt.
        foreach (var pfeil in new[] { Left, Right, Down, Up })  // ← → ↓ ↑
        {
            Assert.Equal(Decision.PassThrough, Decide(pfeil, active: true));
            Assert.Equal(Decision.PassThrough, Decide(pfeil, down: false, active: true));
            // Gehalten muss es weiterscrollen — Autorepeat also auch durchlassen.
            Assert.Equal(Decision.PassThrough, Decide(pfeil, repeated: true, active: true));
            // Auch mit Umschalt/Alt (Markieren, wortweise springen).
            Assert.Equal(Decision.PassThrough, Decide(pfeil, Mods.Shift | alt, active: true));
        }
    }

    [Fact]
    public void leertasteGehtImAktivenModusDurch()
    {
        // Play/Stop der DAW im Hintergrund, ohne den Modus zu verlassen.
        Assert.Equal(Decision.PassThrough, Decide(Space, active: true));
        Assert.Equal(Decision.PassThrough, Decide(Space, down: false, active: true));
        Assert.Equal(Decision.PassThrough, Decide(Space, repeated: true, active: true));
        Assert.Equal(Decision.PassThrough, Decide(Space, Mods.Shift | alt, active: true));
    }

    [Fact]
    public void autorepeatErzeugtKeinZweitesNoteOn()
    {
        // Eine gehaltene Taste würde sonst zehn Note-Ons pro Sekunde feuern.
        Assert.Equal(Decision.Swallow, Decide(A, repeated: true, active: true));
        Assert.Equal(Decision.Swallow, Decide(Tab, repeated: true, active: true));
        Assert.Equal(Decision.Swallow, Decide(Z, repeated: true, active: true));
    }

    // MARK: - Adv.-Layout (via keyAction-Parameter)

    [Fact]
    public void advLayoutErkenntSeineEigenenNotentasten()
    {
        // '<' ist im Adv.-Layout C3, im Grundlayout dagegen gar nicht gemappt.
        var entscheidung = TapPolicy.Decide(
            IsoBackslash, Mods.None, isKeyDown: true, isRepeat: false,
            isActive: true, hotkey: Hotkey.Default, keyAction: AdvancedKeyMap.Action);
        Assert.Equal(Decision.NoteOn(0), entscheidung);
    }

    [Fact]
    public void advLayoutHatEigeneOktavtasten()
    {
        var ab = TapPolicy.Decide(
            AdvancedKeyMap.OctaveDownKey, Mods.None, isKeyDown: true, isRepeat: false,
            isActive: true, hotkey: Hotkey.Default, keyAction: AdvancedKeyMap.Action);
        Assert.Equal(Decision.Octave(-1), ab);
        var auf = TapPolicy.Decide(
            AdvancedKeyMap.OctaveUpKey, Mods.None, isKeyDown: true, isRepeat: false,
            isActive: true, hotkey: Hotkey.Default, keyAction: AdvancedKeyMap.Action);
        Assert.Equal(Decision.Octave(1), auf);
    }

    [Fact]
    public void ohneKeyActionParameterBleibtDasGrundlayoutAktiv()
    {
        // Rückwärtskompatibel: 'a' bedeutet ohne den neuen Parameter weiterhin C3.
        var entscheidung = TapPolicy.Decide(
            A, Mods.None, isKeyDown: true, isRepeat: false, isActive: true, hotkey: Hotkey.Default);
        Assert.Equal(Decision.NoteOn(0), entscheidung);
    }

    [Fact]
    public void minusOhneAltSpieltImAdvLayoutWeiterF4()
    {
        // Der Standard-Hotkey Alt+- teilt sich die Taste mit F4 — nur mit Alt schaltet sie.
        var entscheidung = TapPolicy.Decide(
            Slash, Mods.None, isKeyDown: true, isRepeat: false,
            isActive: true, hotkey: Hotkey.Default, keyAction: AdvancedKeyMap.Action);
        Assert.Equal(Decision.NoteOn(17), entscheidung);
    }
}

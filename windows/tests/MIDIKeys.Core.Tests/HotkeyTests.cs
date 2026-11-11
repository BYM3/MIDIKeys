using System.Text.Json;

namespace MIDIKeys.Core.Tests;

public class HotkeyTests
{
    private const Mods ctrl = Mods.Ctrl;
    private const Mods alt = Mods.Alt;   // Mac: ⌥
    private const Mods shift = Mods.Shift;
    private const Mods win = Mods.Win;   // Mac: ⌘

    [Fact]
    public void standardShortcutIstAltMinus()
    {
        // Mac: ⌥- (kVK_ANSI_Slash). Windows: Alt + dieselbe physische Taste.
        Assert.Equal(ScanCodes.Slash, Hotkey.Default.Key);  // = - auf deutscher Tastatur
        Assert.Equal(alt, Hotkey.Default.Modifiers);
    }

    [Fact]
    public void trifftNurBeiExaktGleichenModifiern()
    {
        var hotkey = new Hotkey(ScanCodes.M, ctrl | alt);
        Assert.True(hotkey.Matches(ScanCodes.M, ctrl | alt));
        Assert.False(hotkey.Matches(ScanCodes.M, ctrl));
        Assert.False(hotkey.Matches(ScanCodes.M, ctrl | alt | shift));
        Assert.False(hotkey.Matches(ScanCodes.A, ctrl | alt));
    }

    [Fact]
    public void anzeigenameFolgtDerWindowsReihenfolge()
    {
        // Mac: Apple-Konvention ⌃⌥⇧⌘. Windows: Strg+Win+Alt+Umschalt+Taste.
        var hotkey = new Hotkey(ScanCodes.M, win | shift | alt | ctrl);
        Assert.Equal("Strg+Win+Alt+Umschalt+", hotkey.ModifierText(Sprache.Deutsch));
        Assert.Equal("Ctrl+Win+Alt+Shift+", hotkey.ModifierText(Sprache.English));
        Assert.Equal("Strg+Win+Alt+Umschalt+M", hotkey.DisplayName("M", Sprache.Deutsch));
        Assert.Equal("Alt+-", Hotkey.Default.DisplayName("-", Sprache.Deutsch));
        Assert.Equal("", new Hotkey(ScanCodes.M, Mods.None).ModifierText(Sprache.Deutsch));
    }

    [Fact]
    public void einShortcutBrauchtMindestensEinenModifier()
    {
        Assert.False(Hotkey.IsValid(Mods.None));
        Assert.False(Hotkey.IsValid(shift));  // Shift allein reicht nicht
        Assert.True(Hotkey.IsValid(ctrl));
        Assert.True(Hotkey.IsValid(win));
        Assert.True(Hotkey.IsValid(alt | shift));
    }

    [Fact]
    public void ueberstehtDenRundlaufDurchDieKodierung()
    {
        var original = new Hotkey(ScanCodes.Space, win | shift);
        var json = JsonSerializer.Serialize(original);
        var zurueck = JsonSerializer.Deserialize<Hotkey>(json);
        Assert.Equal(original, zurueck);
    }
}

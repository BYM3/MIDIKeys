namespace MIDIKeys.Core.Tests;

/// <summary>
/// Mac-Original: <c>HotkeyRecording.hotkey(fromKeycode:modifierFlagsRaw:)</c>.
/// Unter Windows <see cref="Hotkey.FromRecording"/>. Der NSEvent/CGEvent-Bitvergleich
/// und das Abschneiden fremder Flags (CapsLock …) entfallen: Der Hook führt
/// nur die vier Modifier in <see cref="Mods"/>.
/// </summary>
public class HotkeyRecordingTests
{
    [Fact]
    public void gueltigeKombinationWirdAufgezeichnet()
    {
        var hotkey = Hotkey.FromRecording(ScanCodes.C, Mods.Ctrl | Mods.Alt);
        Assert.Equal(new Hotkey(ScanCodes.C, Mods.Ctrl | Mods.Alt), hotkey);
    }

    [Fact]
    public void kombinationOhneModifierWirdAbgelehnt()
    {
        Assert.Null(Hotkey.FromRecording(ScanCodes.C, Mods.None));
        Assert.Null(Hotkey.FromRecording(ScanCodes.C, Mods.Shift));
    }

    [Fact]
    public void winKombinationWirdAufgezeichnet()
    {
        // Win spielt die Rolle von ⌘.
        Assert.Equal(new Hotkey(ScanCodes.C, Mods.Win), Hotkey.FromRecording(ScanCodes.C, Mods.Win));
    }
}

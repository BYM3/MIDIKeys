namespace MIDIKeys.Core;

/// <summary>
/// Physische Tasten als PS/2-Scancodes (Set 1), wie sie der Low-Level-Hook in
/// <c>KBDLLHOOKSTRUCT.scanCode</c> liefert. Erweiterte Tasten (Pfeile, Entf,
/// rechte Strg/Alt …) tragen zusätzlich <see cref="Extended"/>.
///
/// Das Gegenstück zu den macOS-Virtual-Keycodes: Scancodes beschreiben die
/// Position auf der Tastatur, nicht das Zeichen. Die Klaviatur-Geometrie
/// bleibt damit unabhängig vom aktiven Layout (QWERTZ, QWERTY, ungarisch …).
/// Die Namen folgen der US-Beschriftung; der Kommentar nennt die deutsche.
/// </summary>
public static class ScanCodes
{
    public const ushort Extended = 0xE000;

    public const ushort Escape = 0x01;
    public const ushort D1 = 0x02, D2 = 0x03, D3 = 0x04, D4 = 0x05, D5 = 0x06;
    public const ushort D6 = 0x07, D7 = 0x08, D8 = 0x09, D9 = 0x0A, D0 = 0x0B;
    public const ushort Minus = 0x0C;          // DE: ß
    public const ushort EqualsKey = 0x0D;        // DE: ´
    public const ushort Backspace = 0x0E;
    public const ushort Tab = 0x0F;
    public const ushort Q = 0x10, W = 0x11, E = 0x12, R = 0x13, T = 0x14;
    public const ushort Y = 0x15;              // DE: z
    public const ushort U = 0x16, I = 0x17, O = 0x18, P = 0x19;
    public const ushort LeftBracket = 0x1A;    // DE: ü
    public const ushort RightBracket = 0x1B;   // DE: +
    public const ushort Enter = 0x1C;
    public const ushort LeftCtrl = 0x1D;
    public const ushort A = 0x1E, S = 0x1F, D = 0x20, F = 0x21, G = 0x22;
    public const ushort H = 0x23, J = 0x24, K = 0x25, L = 0x26;
    public const ushort Semicolon = 0x27;      // DE: ö
    public const ushort Quote = 0x28;          // DE: ä
    public const ushort Grave = 0x29;          // DE: ^
    public const ushort LeftShift = 0x2A;
    public const ushort Backslash = 0x2B;      // DE: # (ISO-Taste links neben Enter)
    public const ushort Z = 0x2C;              // DE: y
    public const ushort X = 0x2D, C = 0x2E, V = 0x2F, B = 0x30, N = 0x31, M = 0x32;
    public const ushort Comma = 0x33, Period = 0x34;
    public const ushort Slash = 0x35;          // DE: -
    public const ushort RightShift = 0x36;
    public const ushort LeftAlt = 0x38;
    public const ushort Space = 0x39;
    public const ushort CapsLock = 0x3A;
    public const ushort F4 = 0x3E;
    public const ushort IsoBackslash = 0x56;   // DE: < (nur ISO-Tastaturen)

    public const ushort NumpadEnter = Extended | 0x1C;
    public const ushort RightCtrl = Extended | 0x1D;
    public const ushort RightAlt = Extended | 0x38;  // DE: AltGr
    public const ushort Up = Extended | 0x48;
    public const ushort Left = Extended | 0x4B;
    public const ushort Right = Extended | 0x4D;
    public const ushort Down = Extended | 0x50;
    public const ushort Delete = Extended | 0x53;
    public const ushort LeftWin = Extended | 0x5B;
    public const ushort RightWin = Extended | 0x5C;
}

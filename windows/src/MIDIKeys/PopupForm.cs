using System.Drawing.Drawing2D;
using System.Drawing.Text;
using MIDIKeys.Core;

namespace MIDIKeys;

/// <summary>
/// Das Cheatsheet — Gegenstück zum Popover der Mac-App. Ein randloses
/// Flyout über dem Tray, komplett selbst gezeichnet, damit es wie das
/// Original aussieht: Akkordanzeige, Schalter, Klaviatur, Oktav-/Sustain-
/// Tasten, Piano/Adv., Shortcut-Recorder, Einstellungen.
/// </summary>
internal sealed class PopupForm : Form
{
    private readonly AppState _state;
    private readonly InputController _controller;
    private readonly KeyboardHook _hook;
    private readonly Action<string?> _applyOutput;
    private readonly Action _quit;

    private readonly List<(RectangleF Rect, Action Action)> _hits = new();
    private readonly System.Windows.Forms.Timer _chordTimer = new() { Interval = 100 };
    private string? _chordShown;
    private string? _chordPending;
    private bool _settings;
    private bool _recording;
    private string? _recordError;
    private List<string> _devices = new();
    private DateTime _hiddenAt = DateTime.MinValue;
    private PointF _hover = new(-1, -1);

    private const float W = 380;
    private const float Pad = 16;

    public PopupForm(AppState state, InputController controller, KeyboardHook hook,
        Action<string?> applyOutput, Action quit)
    {
        _state = state;
        _controller = controller;
        _hook = hook;
        _applyOutput = applyOutput;
        _quit = quit;

        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        AutoScaleMode = AutoScaleMode.None;
        DoubleBuffered = true;
        KeyPreview = true;
        Text = "MIDIKeys";
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer, true);

        _chordTimer.Tick += (_, _) =>
        {
            _chordTimer.Stop();
            _chordShown = _chordPending;
            Invalidate();
        };
        _state.Changed += OnStateChanged;
    }

    protected override CreateParams CreateParams
    {
        get
        {
            var cp = base.CreateParams;
            cp.ClassStyle |= 0x00020000; // CS_DROPSHADOW
            cp.ExStyle |= 0x00000080;    // WS_EX_TOOLWINDOW: nicht im Alt+Tab
            return cp;
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        var round = Native.DWMWCP_ROUND;
        Native.DwmSetWindowAttribute(Handle, Native.DWMWA_WINDOW_CORNER_PREFERENCE, ref round, sizeof(int));
    }

    private float S => DeviceDpi / 96f;
    private string T(string de, string en) => _state.Sprache.Text(de, en);

    // ---------------------------------------------------------------- Zeigen

    public void ToggleNear(Point cursor)
    {
        if (Visible) { HidePopup(); return; }
        // Ein Klick aufs Tray-Symbol schließt das Flyout erst per Deactivate —
        // ohne diese Sperre öffnete derselbe Klick es sofort wieder.
        if ((DateTime.Now - _hiddenAt).TotalMilliseconds < 250) return;
        ShowNear(cursor);
    }

    public void ShowNear(Point cursor)
    {
        _settings = false;
        _chordShown = _state.ChordName;
        Relayout();
        var area = Screen.FromPoint(cursor).WorkingArea;
        var gap = (int)(12 * S);
        var x = Math.Clamp(cursor.X - Width / 2, area.Left + gap, area.Right - Width - gap);
        var y = cursor.Y < area.Top + area.Height / 2 ? area.Top + gap : area.Bottom - Height - gap;
        Location = new Point(x, y);
        Show();
        Activate();
        Native.SetForegroundWindow(Handle);
    }

    private void HidePopup()
    {
        StopRecording();
        Hide();
        _hiddenAt = DateTime.Now;
    }

    protected override void OnDeactivate(EventArgs e)
    {
        base.OnDeactivate(e);
        // MIDIKEYS_PIN_POPUP=1 hält das Flyout offen — für Bildschirmfotos und Tests.
        if (Visible && Environment.GetEnvironmentVariable("MIDIKEYS_PIN_POPUP") != "1") HidePopup();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.KeyCode == Keys.Escape && !_state.IsActive && !_recording) HidePopup();
    }

    protected override void OnDpiChanged(DpiChangedEventArgs e)
    {
        base.OnDpiChanged(e);
        Relayout();
    }

    private void OnStateChanged()
    {
        if (!Visible) return;
        var chord = _state.ChordName;
        if (chord != _chordPending)
        {
            _chordPending = chord;
            _chordTimer.Stop();
            if (chord != _chordShown) _chordTimer.Start();
        }
        // Läuft oft im Hook-Callback: nur vormerken, gezeichnet wird danach.
        if (_relayoutPending) return;
        _relayoutPending = true;
        BeginInvoke(() => { _relayoutPending = false; Relayout(); });
    }

    private bool _relayoutPending;

    // ------------------------------------------------------- Hotkey-Aufnahme

    private void StartRecording()
    {
        _recording = true;
        _recordError = null;
        _hook.Recorder = (key, mods) =>
        {
            if (key == ScanCodes.Escape && mods == Mods.None) { StopRecording(); return; }
            if (Hotkey.FromRecording(key, mods) is { } neu)
            {
                StopRecording();
                _state.Hotkey = neu;
            }
            else
            {
                _recordError = T("Mindestens Strg, Alt oder Win verwenden.", "Use at least Ctrl, Alt or Win.");
                BeginInvoke(Relayout);
            }
        };
        Relayout();
    }

    private void StopRecording()
    {
        if (!_recording) return;
        _recording = false;
        _hook.Recorder = null;
        if (IsHandleCreated) BeginInvoke(Relayout);
    }

    // ---------------------------------------------------------------- Farben

    private sealed record Palette(
        Color Bg, Color Text, Color Secondary, Color Fill, Color FillStrong, Color Border,
        Color WhiteKey, Color BlackKey, Color BlackKeyText, Color Accent);

    private static Palette CurrentPalette()
    {
        var accent = Theme.Accent;
        return Theme.AppsDark
            ? new Palette(Color.FromArgb(0x24, 0x24, 0x24), Color.FromArgb(0xF2, 0xF2, 0xF2), Color.FromArgb(0x9E, 0x9E, 0x9E),
                Color.FromArgb(0x33, 0x33, 0x33), Color.FromArgb(0x40, 0x40, 0x40), Color.FromArgb(0x3C, 0x3C, 0x3C),
                Color.FromArgb(0x3A, 0x3A, 0x3A), Color.FromArgb(0x14, 0x14, 0x14), Color.FromArgb(0xE6, 0xE6, 0xE6), accent)
            : new Palette(Color.FromArgb(0xF6, 0xF6, 0xF6), Color.FromArgb(0x1A, 0x1A, 0x1A), Color.FromArgb(0x6B, 0x6B, 0x6B),
                Color.FromArgb(0xE6, 0xE6, 0xE6), Color.FromArgb(0xD8, 0xD8, 0xD8), Color.FromArgb(0xD0, 0xD0, 0xD0),
                Color.FromArgb(0xFF, 0xFF, 0xFF), Color.FromArgb(0x2A, 0x2A, 0x2A), Color.FromArgb(0xF2, 0xF2, 0xF2), accent);
    }

    // ---------------------------------------------------------------- Zeichnen

    /// <summary>
    /// Ein Durchgang für Layout und Zeichnen: Mit <c>g == null</c> wird nur
    /// gemessen (Höhe), sonst gezeichnet und die Klickflächen gesammelt.
    /// </summary>
    private float Draw(Graphics? g)
    {
        _hits.Clear();
        return _settings ? DrawSettings(g) : DrawMain(g);
    }

    private void Relayout()
    {
        if (IsDisposed) return;
        var h = Draw(null);
        var size = new Size((int)Math.Ceiling(W * S), (int)Math.Ceiling(h * S));
        if (Size != size)
        {
            var bottom = Bottom;
            var anchoredBottom = Visible && Top > Screen.FromControl(this).WorkingArea.Top + 40;
            Size = size;
            if (anchoredBottom) Top = bottom - Height;
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var p = CurrentPalette();
        g.Clear(p.Bg);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        g.ScaleTransform(S, S);
        using (var pen = new Pen(p.Border, 1 / S)) g.DrawRectangle(pen, 0, 0, W - 1 / S, Height / S - 1 / S);
        Draw(g);
    }

    private Font F(float px, FontStyle style = FontStyle.Regular, string family = "Segoe UI") =>
        new(family, px, style, GraphicsUnit.Pixel);

    private static SizeF Measure(string text, Font font)
    {
        using var bmp = new Bitmap(1, 1);
        using var g = Graphics.FromImage(bmp);
        g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
        return g.MeasureString(text, font, PointF.Empty, StringFormat.GenericTypographic);
    }

    private static void DrawText(Graphics? g, string text, Font font, Color color, float x, float y,
        StringAlignment align = StringAlignment.Near, float width = 0)
    {
        if (g == null) return;
        using var brush = new SolidBrush(color);
        using var sf = new StringFormat(StringFormat.GenericTypographic) { Alignment = align, FormatFlags = StringFormatFlags.NoWrap };
        if (width > 0) g.DrawString(text, font, brush, new RectangleF(x, y, width, font.Size * 1.6f), sf);
        else g.DrawString(text, font, brush, x, y, sf);
    }

    private static void Fill(Graphics? g, RectangleF r, float radius, Color color)
    {
        if (g == null) return;
        using var path = IconFactory.RoundedRect(r, radius);
        using var brush = new SolidBrush(color);
        g.FillPath(brush, path);
    }

    private static void Stroke(Graphics? g, RectangleF r, float radius, Color color, float width = 1)
    {
        if (g == null) return;
        using var path = IconFactory.RoundedRect(r, radius);
        using var pen = new Pen(color, width);
        g.DrawPath(pen, path);
    }

    private void Divider(Graphics? g, Palette p, ref float y)
    {
        y += 10;
        if (g != null) { using var pen = new Pen(p.Border, 1); g.DrawLine(pen, Pad, y, W - Pad, y); }
        y += 11;
    }

    private bool Hovered(RectangleF r) => r.Contains(_hover);

    private float DrawMain(Graphics? g)
    {
        var p = CurrentPalette();
        var y = Pad;

        // Kopf: Akkordname (oder "MIDIKeys") und der Ein/Aus-Schalter.
        using (var font = F(17, FontStyle.Bold, "Segoe UI Semibold"))
        {
            var titel = _chordShown ?? "MIDIKeys";
            var maxW = W - Pad * 2 - 60;
            var size = Measure(titel, font);
            using var passend = size.Width > maxW ? F(17 * Math.Max(0.75f, maxW / size.Width), FontStyle.Bold, "Segoe UI Semibold") : null;
            DrawText(g, titel, passend ?? font, p.Text, Pad, y);
        }
        var sw = new RectangleF(W - Pad - 44, y + 1, 44, 22);
        var enabled = _state.TapError == null;
        if (g != null)
        {
            var on = _state.IsActive;
            Fill(g, sw, 11, on ? p.Accent : p.Bg);
            if (!on) Stroke(g, sw, 11, p.Secondary, 1.2f);
            var knob = on ? new RectangleF(sw.Right - 18, sw.Top + 4, 14, 14) : new RectangleF(sw.Left + 5, sw.Top + 5, 12, 12);
            Fill(g, knob, 7, on ? Color.White : p.Secondary);
        }
        if (enabled) _hits.Add((sw, () => _controller.SetActive(!_state.IsActive)));
        y += 28;

        if (_state.TapError is { } fehler)
        {
            y += 8;
            using var bold = F(12, FontStyle.Bold);
            using var small = F(11.5f);
            var box = new RectangleF(Pad, y, W - Pad * 2, 58);
            Fill(g, box, 6, p.Fill);
            DrawText(g, T("Tastatur wird nicht gelesen", "Keyboard is not being read"), bold, p.Text, box.X + 8, box.Y + 7);
            if (g != null)
            {
                using var brush = new SolidBrush(p.Secondary);
                g.DrawString(fehler, small, brush, new RectangleF(box.X + 8, box.Y + 25, box.Width - 16, 32));
            }
            y = box.Bottom;
        }

        Divider(g, p, ref y);

        // Klaviatur
        if (_state.AdvancedLayoutEnabled)
        {
            y = DrawPiano(g, p, y, 17, 36, AdvancedKeyMap.NoteKeys, AdvancedKeyMap.Action);
            y += 6;
            y = DrawPiano(g, p, y, 0, 16, AdvancedKeyMap.NoteKeys, AdvancedKeyMap.Action);
        }
        else
        {
            y = DrawPiano(g, p, y, 0, KeyMap.MaxSemitone, KeyMap.NoteKeys, KeyMap.Action);
        }
        y += 12;

        // Steuerung: Oktave ab/auf, Sustain, Lage
        var ab = _state.AdvancedLayoutEnabled ? AdvancedKeyMap.OctaveDownKey : KeyMap.OctaveDownKey;
        var auf = _state.AdvancedLayoutEnabled ? AdvancedKeyMap.OctaveUpKey : KeyMap.OctaveUpKey;
        var x = Pad;
        x = Beschriftung(g, p, x, y, KeyLabelResolver.Label(ab), T("Oktave ab", "Octave down"), ab) + 14;
        x = Beschriftung(g, p, x, y, KeyLabelResolver.Label(auf), T("Oktave auf", "Octave up"), auf) + 14;
        Beschriftung(g, p, x, y, "⇥", "Sustain", KeyMap.SustainKey);
        using (var bold = F(12.5f, FontStyle.Bold))
        {
            var lage = _state.Keyboard.RangeDescription(_state.MaxSemitone, _state.Sprache);
            DrawText(g, lage, bold, p.Text, W - Pad - 120, y + 2, StringAlignment.Far, 120);
        }
        y += 22;

        Divider(g, p, ref y);

        // Piano / Adv. links, Shortcut-Recorder rechts
        x = Pad;
        x = Pill(g, p, x, y + 3, "Piano", _state.PianoModeEnabled, _state.PianoModeAvailable,
            () => _state.PianoModeEnabled = !_state.PianoModeEnabled) + 6;
        Pill(g, p, x, y + 3, "Adv.", _state.AdvancedLayoutEnabled, true,
            () => _state.AdvancedLayoutEnabled = !_state.AdvancedLayoutEnabled);

        using (var mono = F(12.5f, FontStyle.Regular, "Cascadia Mono"))
        using (var small = F(11.5f))
        {
            var label = _recording
                ? T("Taste drücken …", "Press keys …")
                : _state.Hotkey.DisplayName(KeyLabelResolver.Label(_state.Hotkey.Key).ToUpperInvariant(), _state.Sprache);
            var bw = Math.Max(100, Measure(label, mono).Width + 22);
            var btn = new RectangleF(W - Pad - bw, y, bw, 28);
            Fill(g, btn, 5, _recording ? p.Accent : (Hovered(btn) ? p.FillStrong : p.Fill));
            DrawText(g, label, mono, _recording ? Color.White : p.Text, btn.X, btn.Y + 6, StringAlignment.Center, btn.Width);
            _hits.Add((btn, () => { if (_recording) StopRecording(); else StartRecording(); }));
            var lbl = T("Umschalten", "Toggle");
            DrawText(g, lbl, small, p.Secondary, btn.X - 8 - Measure(lbl, small).Width, y + 7);
        }
        y += 28;
        if (_recordError != null)
        {
            using var small = F(11.5f);
            y += 4;
            DrawText(g, _recordError, small, p.Secondary, Pad, y);
            y += 16;
        }

        // Fuß: Ziel, Zahnrad, Beenden
        y += 12;
        using (var tiny = F(11))
        {
            var status = _state.PianoModeEnabled
                ? T("Spielt lokal (GS Wavetable Synth)", "Plays locally (GS Wavetable Synth)")
                : _state.OutputStatus;
            DrawText(g, Ellipsize(status, tiny, W - Pad * 2 - 130), tiny, p.Secondary, Pad, y + 6);
        }
        using (var small = F(12))
        {
            var quitText = T("Beenden", "Quit");
            var qw = Measure(quitText, small).Width + 20;
            var quit = new RectangleF(W - Pad - qw, y, qw, 26);
            Fill(g, quit, 5, Hovered(quit) ? p.FillStrong : p.Fill);
            DrawText(g, quitText, small, p.Text, quit.X, quit.Y + 5, StringAlignment.Center, quit.Width);
            _hits.Add((quit, _quit));

            var gear = new RectangleF(quit.X - 6 - 26, y, 26, 26);
            Fill(g, gear, 5, Hovered(gear) ? p.FillStrong : p.Fill);
            using var sym = F(14, FontStyle.Regular, "Segoe Fluent Icons");
            DrawText(g, "", sym, p.Text, gear.X, gear.Y + 6, StringAlignment.Center, gear.Width);
            _hits.Add((gear, () => { _settings = true; _devices = ExternalOutput.SelectableDevices(); Relayout(); }));
        }
        y += 26;
        return y + Pad;
    }

    private static string Ellipsize(string text, Font font, float max)
    {
        if (Measure(text, font).Width <= max) return text;
        while (text.Length > 1 && Measure(text + "…", font).Width > max) text = text[..^1];
        return text + "…";
    }

    /// <summary>
    /// Ein zusammenhängender Ausschnitt der Klaviatur. Schwarze Tasten sitzen
    /// an der Fuge rechts der weißen Taste unter ihnen.
    /// </summary>
    private float DrawPiano(Graphics? g, Palette p, float y, int from, int to,
        IReadOnlyDictionary<int, ushort> noteKeys, Func<ushort, KeyAction?> keyAction)
    {
        const float ww = 26, wh = 74, bw = 17, bh = 44, gap = 2;
        var range = Enumerable.Range(from, to - from + 1).Where(noteKeys.ContainsKey).ToList();
        var whites = range.Where(s => !KeyMap.IsBlackKey(s)).ToList();
        var blacks = range.Where(KeyMap.IsBlackKey).ToList();
        var total = whites.Count * (ww + gap) - gap;
        var x0 = (W - total) / 2;

        if (g != null)
        {
            using var keyFont = F(13, FontStyle.Bold);
            using var noteFont = F(8.5f);
            for (var i = 0; i < whites.Count; i++)
                Taste(g, p, whites[i], new RectangleF(x0 + i * (ww + gap), y, ww, wh), false, noteKeys, keyAction, keyFont, noteFont);
            foreach (var s in blacks)
            {
                var links = whites.Count(w => w < s);
                var fuge = x0 + links * (ww + gap) - gap / 2;
                Taste(g, p, s, new RectangleF(fuge - bw / 2, y, bw, bh), true, noteKeys, keyAction, keyFont, noteFont);
            }
        }
        return y + wh;
    }

    private void Taste(Graphics g, Palette p, int semitone, RectangleF r, bool schwarz,
        IReadOnlyDictionary<int, ushort> noteKeys, Func<ushort, KeyAction?> keyAction, Font keyFont, Font noteFont)
    {
        // Beide Tasten einer Dopplung (Adv.) lassen dieselbe Kachel leuchten.
        var gedrueckt = _state.PressedKeys.Any(k => keyAction(k) is { Kind: KeyActionKind.Note } a && a.Semitone == semitone);
        var fill = gedrueckt ? p.Accent : schwarz ? p.BlackKey : p.WhiteKey;
        var fg = gedrueckt ? Color.White : schwarz ? p.BlackKeyText : p.Text;
        Fill(g, r, 4, fill);
        Stroke(g, r, 4, p.Border, 0.75f);
        var label = KeyLabelResolver.Label(noteKeys[semitone]);
        var note = Notes.Name(_state.Keyboard.Note(semitone), _state.Sprache);
        DrawText(g, label, keyFont, fg, r.X, r.Bottom - 33, StringAlignment.Center, r.Width);
        DrawText(g, note, noteFont, Color.FromArgb(gedrueckt ? 220 : 255, fg), r.X, r.Bottom - 15, StringAlignment.Center, r.Width);
    }

    private float Beschriftung(Graphics? g, Palette p, float x, float y, string taste, string text, ushort key)
    {
        var gedrueckt = _state.PressedKeys.Contains(key);
        using var bold = F(11.5f, FontStyle.Bold);
        using var small = F(11.5f);
        var tw = Math.Max(18, Measure(taste, bold).Width + 10);
        var cap = new RectangleF(x, y + 1, tw, 19);
        Fill(g, cap, 3, gedrueckt ? p.Accent : p.Fill);
        DrawText(g, taste, bold, gedrueckt ? Color.White : p.Text, cap.X, cap.Y + 2, StringAlignment.Center, cap.Width);
        DrawText(g, text, small, p.Secondary, cap.Right + 5, y + 3);
        return cap.Right + 5 + Measure(text, small).Width;
    }

    private float Pill(Graphics? g, Palette p, float x, float y, string titel, bool aktiv, bool verfuegbar, Action action)
    {
        using var font = F(12, FontStyle.Bold);
        var r = new RectangleF(x, y, Measure(titel, font).Width + 16, 22);
        var alpha = verfuegbar ? 255 : 100;
        Fill(g, r, 5, Color.FromArgb(alpha, aktiv ? p.Accent : (Hovered(r) && verfuegbar ? p.FillStrong : p.Fill)));
        DrawText(g, titel, font, Color.FromArgb(alpha, aktiv ? Color.White : p.Text), r.X, r.Y + 4, StringAlignment.Center, r.Width);
        if (verfuegbar) _hits.Add((r, action));
        return r.Right;
    }

    // ---------------------------------------------------------- Einstellungen

    private float DrawSettings(Graphics? g)
    {
        var p = CurrentPalette();
        var y = Pad;
        using var font = F(13);
        using var bold = F(15, FontStyle.Bold, "Segoe UI Semibold");
        using var small = F(11.5f);
        using var sectionFont = F(11.5f, FontStyle.Bold);

        var back = new RectangleF(Pad - 4, y - 2, 90, 26);
        DrawText(g, "‹ " + T("Zurück", "Back"), font, Hovered(back) ? p.Text : p.Secondary, Pad, y + 2);
        _hits.Add((back, () => { _settings = false; Relayout(); }));
        DrawText(g, T("Einstellungen", "Settings"), bold, p.Text, 0, y, StringAlignment.Center, W);
        y += 24;
        Divider(g, p, ref y);

        y = Checkbox(g, p, y, T("Beim Anmelden öffnen", "Launch at login"), LoginItem.IsEnabled,
            () => { LoginItem.SetEnabled(!LoginItem.IsEnabled); _state.LoginItemRegistered = true; Relayout(); });
        y = Checkbox(g, p, y, T("Tasten c und r freigeben (z. B. für Ableton)", "Let the c and r keys through (e.g. for Ableton)"),
            _state.CUndRFrei, () => _state.CUndRFrei = !_state.CUndRFrei);
        DrawText(g, T("Gilt nur im Grundlayout mit MIDI-Ausgang – nicht mit Adv. oder Piano.",
                  "Only in the basic layout with MIDI output – not with Adv. or Piano."), small, p.Secondary, Pad + 26, y - 4);
        y += 18;

        // Sprache als Segmented Control
        y += 6;
        DrawText(g, T("Sprache", "Language"), font, p.Text, Pad, y + 4);
        var seg = new RectangleF(W - Pad - 170, y, 170, 28);
        Fill(g, seg, 6, p.Fill);
        var sprachen = new[] { Sprache.Deutsch, Sprache.English };
        for (var i = 0; i < 2; i++)
        {
            var r = new RectangleF(seg.X + 2 + i * 83, seg.Y + 2, 83, 24);
            var sel = _state.Sprache == sprachen[i];
            if (sel) Fill(g, r, 5, p.Accent);
            DrawText(g, sprachen[i].Name(), font, sel ? Color.White : p.Text, r.X, r.Y + 3, StringAlignment.Center, r.Width);
            var s = sprachen[i];
            _hits.Add((r, () => { _state.Sprache = s; _applyOutput(_state.OutputDevice); }));
        }
        y += 28;

        Divider(g, p, ref y);
        DrawText(g, T("MIDI-AUSGANG", "MIDI OUTPUT"), sectionFont, p.Secondary, Pad, y);
        y += 20;

        var treiber = VirtualMidiPort.DriverAvailable;
        y = Radio(g, p, y, T("Automatisch – eigener Eingang „MIDIKeys“", "Automatic – own input “MIDIKeys”"),
            _state.OutputDevice == null, () => _applyOutput(null));
        if (!treiber)
        {
            using var linkFont = F(11.5f, FontStyle.Underline);
            DrawText(g, T("Braucht den loopMIDI-Treiber:", "Needs the loopMIDI driver:"), small, p.Secondary, Pad + 26, y - 4);
            var linkText = "tobias-erichsen.de/loopMIDI";
            var lx = Pad + 26 + Measure(T("Braucht den loopMIDI-Treiber:", "Needs the loopMIDI driver:"), small).Width + 4;
            var link = new RectangleF(lx, y - 5, Measure(linkText, linkFont).Width, 16);
            DrawText(g, linkText, linkFont, p.Accent, lx, y - 4);
            _hits.Add((link, () => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(
                "https://www.tobias-erichsen.de/software/loopmidi.html") { UseShellExecute = true })));
            y += 16;
        }
        foreach (var name in _devices)
        {
            var n = name;
            y = Radio(g, p, y, name, _state.OutputDevice == name, () => _applyOutput(n));
        }
        if (_state.OutputDevice != null && !_devices.Contains(_state.OutputDevice))
            y = Radio(g, p, y, _state.OutputDevice + T(" (nicht verbunden)", " (not connected)"), true, () => _applyOutput(_state.OutputDevice));

        y += 6;
        DrawText(g, Ellipsize(_state.OutputStatus, small, W - Pad * 2), small, p.Secondary, Pad, y);
        y += 16;
        return y + Pad;
    }

    private float Checkbox(Graphics? g, Palette p, float y, string text, bool checkedState, Action action)
    {
        using var font = F(13);
        var box = new RectangleF(Pad, y + 2, 16, 16);
        if (checkedState)
        {
            Fill(g, box, 3, p.Accent);
            if (g != null)
            {
                using var pen = new Pen(Color.White, 1.8f);
                g.DrawLines(pen, [new PointF(box.X + 3.5f, box.Y + 8), new PointF(box.X + 6.5f, box.Y + 11.5f), new PointF(box.X + 12.5f, box.Y + 4.5f)]);
            }
        }
        else Stroke(g, box, 3, p.Secondary, 1.2f);
        DrawText(g, text, font, p.Text, Pad + 26, y + 1);
        _hits.Add((new RectangleF(Pad - 4, y - 2, W - Pad * 2 + 8, 26), action));
        return y + 28;
    }

    private float Radio(Graphics? g, Palette p, float y, string text, bool selected, Action action)
    {
        using var font = F(13);
        var row = new RectangleF(Pad - 6, y - 2, W - Pad * 2 + 12, 26);
        if (Hovered(row)) Fill(g, row, 5, p.Fill);
        var c = new RectangleF(Pad, y + 3, 16, 16);
        if (g != null)
        {
            using var pen = new Pen(selected ? p.Accent : p.Secondary, 1.3f);
            g.DrawEllipse(pen, c);
            if (selected) { using var b = new SolidBrush(p.Accent); g.FillEllipse(b, RectangleF.Inflate(c, -4, -4)); }
        }
        DrawText(g, Ellipsize(text, font, W - Pad * 2 - 30), font, p.Text, Pad + 26, y + 2);
        _hits.Add((row, action));
        return y + 28;
    }

    // ---------------------------------------------------------------- Maus

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        var pt = new PointF(e.X / S, e.Y / S);
        var vorher = _hits.FindIndex(h => h.Rect.Contains(_hover));
        var nachher = _hits.FindIndex(h => h.Rect.Contains(pt));
        _hover = pt;
        Cursor = nachher >= 0 ? Cursors.Hand : Cursors.Default;
        if (vorher != nachher) Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        _hover = new PointF(-1, -1);
        Invalidate();
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button != MouseButtons.Left) return;
        var pt = new PointF(e.X / S, e.Y / S);
        // Rückwärts: Später Gezeichnetes liegt oben.
        for (var i = _hits.Count - 1; i >= 0; i--)
        {
            if (!_hits[i].Rect.Contains(pt)) continue;
            _hits[i].Action();
            Relayout();
            return;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _state.Changed -= OnStateChanged;
            _chordTimer.Dispose();
        }
        base.Dispose(disposing);
    }
}

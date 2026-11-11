using MIDIKeys.Core;

namespace MIDIKeys;

/// <summary>
/// Das Gegenstück zum AppDelegate: Tray-Symbol statt Menüleiste, Popup statt
/// Popover. Verdrahtet Zustand, Hook, Controller und MIDI-Ziele.
/// </summary>
internal sealed class TrayApp : ApplicationContext
{
    private readonly AppState _state;
    private readonly ExternalOutput _external = new();
    private readonly LocalPiano _piano = new();
    private readonly TransportRouter _router;
    private readonly InputController _controller;
    private readonly KeyboardHook _hook;
    private readonly NotifyIcon _tray;
    private readonly PopupForm _popup;
    private readonly Control _invoker = new();
    private bool _lastActive;
    private bool _lastPianoNeeded;
    private bool _updatePending;

    public TrayApp(bool showPopup)
    {
        Diagnose.Log($"--- Start, PID {Environment.ProcessId} ---");
        Diagnose.Log($"Pfad: {Environment.ProcessPath}");
        _invoker.CreateControl();

        var settingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MIDIKeys", "settings.json");
        _state = new AppState(new JsonSettingsStore(settingsPath));
        LoginItem.RegisterOnFirstLaunch(_state);

        _external.Apply(_state.OutputDevice, _state.Sprache);
        _state.OutputStatus = _external.Status ?? "";

        _state.PianoModeAvailable = LocalPiano.Available;
        if (!_state.PianoModeAvailable) Diagnose.Log("Klavier-Ton nicht verfügbar: kein GS Wavetable Synth");

        _router = new TransportRouter(_external, _piano) { PianoModeEnabled = _state.PianoModeEnabled };
        _controller = new InputController(_state, new MidiEngine(_router));
        _hook = new KeyboardHook(_controller.Handle);

        _popup = new PopupForm(_state, _controller, _hook, ApplyOutput, () => Quit());
        _tray = new NotifyIcon
        {
            Visible = true,
            Text = "MIDIKeys",
            ContextMenuStrip = BuildMenu(),
        };
        _tray.MouseClick += (_, e) => { if (e.Button == MouseButtons.Left) _popup.ToggleNear(Cursor.Position); };
        UpdateTrayIcon();

        StartHook();
        _state.Changed += OnStateChanged;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += (_, _) => UpdateTrayIcon();

        if (showPopup) _invoker.BeginInvoke(() => _popup.ShowNear(Cursor.Position));
    }

    private void StartHook()
    {
        if (_hook.Start())
        {
            Diagnose.Log("Hook angelegt");
            _state.TapError = null;
        }
        else
        {
            _state.TapError = _state.Sprache.Text(
                "Der Tastatur-Hook ließ sich nicht anlegen. Ein Neustart der App hilft meist.",
                "The keyboard hook could not be installed. Restarting the app usually helps.");
        }
    }

    /// <summary>
    /// Läuft oft mitten im Hook-Callback. Darum nur vormerken; die eigentliche
    /// Arbeit (Gerät öffnen, Tray-Symbol tauschen) kommt danach per BeginInvoke.
    /// </summary>
    private void OnStateChanged()
    {
        _router.PianoModeEnabled = _state.PianoModeEnabled;
        if (_updatePending) return;
        _updatePending = true;
        _invoker.BeginInvoke(ApplyStateEffects);
    }

    private void ApplyStateEffects()
    {
        _updatePending = false;
        if (_lastActive != _state.IsActive)
        {
            _lastActive = _state.IsActive;
            UpdateTrayIcon();
        }
        // Der GS-Synth ist nur offen, solange tatsächlich Klavier gespielt werden kann.
        var pianoNeeded = _state.IsActive && _state.PianoModeEnabled;
        if (pianoNeeded != _lastPianoNeeded)
        {
            _lastPianoNeeded = pianoNeeded;
            _piano.SetAktiv(pianoNeeded);
        }
    }

    private void ApplyOutput(string? device)
    {
        _controller.SetActive(false); // nie mit klingenden Tönen das Ziel wechseln
        _state.OutputDevice = device;
        _external.Apply(device, _state.Sprache);
        _state.OutputStatus = _external.Status ?? "";
    }

    private void UpdateTrayIcon()
    {
        var size = SystemInformation.SmallIconSize.Width;
        var old = _tray.Icon;
        _tray.Icon = IconFactory.TrayIcon(_state.IsActive, Math.Max(16, size));
        old?.Dispose();
        var t = _state.Sprache;
        _tray.Text = _state.IsActive ? t.Text("MIDIKeys – MIDI-Modus an", "MIDIKeys – MIDI mode on") : "MIDIKeys";
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        var toggle = new ToolStripMenuItem();
        var open = new ToolStripMenuItem();
        var quit = new ToolStripMenuItem();
        toggle.Click += (_, _) => _controller.SetActive(!_state.IsActive);
        open.Click += (_, _) => _popup.ShowNear(Cursor.Position);
        quit.Click += (_, _) => Quit();
        menu.Items.AddRange([toggle, open, new ToolStripSeparator(), quit]);
        menu.Opening += (_, _) =>
        {
            var t = _state.Sprache;
            toggle.Text = t.Text("MIDI-Modus", "MIDI mode");
            toggle.Checked = _state.IsActive;
            toggle.ShortcutKeyDisplayString = _state.Hotkey.DisplayName(
                KeyLabelResolver.Label(_state.Hotkey.Key).ToUpperInvariant(), t);
            open.Text = t.Text("Cheatsheet öffnen", "Open cheatsheet");
            quit.Text = t.Text("Beenden", "Quit");
        };
        return menu;
    }

    public void Quit()
    {
        // Nie mit klingenden Tönen aussteigen.
        _controller.SetActive(false);
        _hook.Dispose();
        _piano.Dispose();
        _external.Dispose();
        _tray.Visible = false;
        _tray.Dispose();
        _popup.Dispose();
        Diagnose.Log("Beendet");
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hook.Dispose();
            _piano.Dispose();
            _external.Dispose();
            _tray.Dispose();
            _invoker.Dispose();
        }
        base.Dispose(disposing);
    }

}

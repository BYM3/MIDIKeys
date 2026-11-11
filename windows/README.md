# MIDIKeys für Windows

Verwandelt die PC-Tastatur in ein MIDI-Keyboard. Sitzt im Infobereich der
Taskleiste (Tray), kein Fenster, kein Taskleisten-Eintrag. Funktionsgleicher
Port der [macOS-App](../mac/README.md): gleiche Klaviatur, gleiche
Tastenbelegung (nach physischer Position), Akkorderkennung, Piano/Adv.,
einstellbarer Shortcut, Deutsch/Englisch.

## Installieren

Braucht **Windows 10/11 (x64)** und die **.NET 10 Desktop Runtime**
(oder einen Build mit `-SelfContained`, der sie mitbringt).

    cd windows
    .\build.ps1 -Run

Das Skript führt die Tests aus, baut, installiert nach
`%LOCALAPPDATA%\Programs\MIDIKeys` und legt eine Startmenü-Verknüpfung an.
Zum Bauen wird das .NET 10 SDK benötigt (`winget install Microsoft.DotNet.SDK.10`).

### MIDI in der DAW: loopMIDI

macOS kann eine virtuelle MIDI-Quelle direkt anlegen, Windows nicht. Ist
[loopMIDI](https://www.tobias-erichsen.de/software/loopmidi.html) installiert,
nutzt MIDIKeys dessen Treiber (teVirtualMIDI) und legt **selbst** einen
MIDI-Eingang namens **MIDIKeys** an. In loopMIDI muss dafür kein Port
angelegt werden, und loopMIDI muss nicht laufen. In der DAW erscheint
dieser Eingang dann wie unter macOS.

Ohne loopMIDI lässt sich in den Einstellungen (Zahnrad) jeder vorhandene
MIDI-Ausgang wählen, z. B. ein selbst angelegter loopMIDI-Port. Der
**Piano**-Modus braucht nichts davon: Er spielt über den eingebauten
*Microsoft GS Wavetable Synth*.

## Bedienung

Die Tasten sind über ihre **Position** definiert, nicht über das Zeichen.
Die Tabelle nennt die Beschriftung einer deutschen Tastatur; das
Cheatsheet zeigt immer die Zeichen des aktiven Layouts.

| Taste | Funktion |
|---|---|
| `a s d f g h j k l ö ä #` | weiße Tasten (C D E F G A H C D E F G) |
| `w e t z u o p +` | schwarze Tasten |
| `y` / `x` | Oktave ab / auf |
| `Tab` | Sustain-Pedal |
| `Alt -` (einstellbar) | MIDI-Modus an/aus |
| `Esc` `Esc` (innerhalb 0,5 s) | Notausstieg: MIDI-Modus aus |

Im aktiven Modus schluckt die App die Tastatur. **Kombinationen mit `Strg`
oder der Windows-Taste gehen immer durch**, also `Strg+Umschalt+Esc`
(Task-Manager), `Strg+C` oder `Win+L`. Das ist das Gegenstück zur ⌘-Regel
auf dem Mac. Ebenso gehen **`Alt+Tab`, `Alt+F4` und `Alt+Esc`** durch, dazu
die **Pfeiltasten** und die **Leertaste**. Mit MIDI-Ausgang (ohne Piano)
gehen auch **Enter** und die **Löschtasten** durch. `c` und `r` lassen sich
in den Einstellungen freigeben (Grundlayout, z. B. für Ableton).
`Strg+Alt+Entf` lässt sich ohnehin nie abfangen.

Ein Klick auf das Tray-Symbol öffnet das Cheatsheet, ein Rechtsklick ein
Menü mit *MIDI-Modus*, *Cheatsheet öffnen* und *Beenden*.

Beim ersten Start trägt sich die App einmalig unter *Autostart* ein
(`HKCU\…\Run`). Abschalten lässt sich das im Task-Manager unter
*Autostart-Apps* oder in den Einstellungen der App. Die App legt den
Eintrag danach nicht wieder an.

### Einschränkungen gegenüber macOS

- **Eine Freigabe ist nicht nötig.** Windows erlaubt globale
  Tastatur-Hooks ohne Bedienungshilfen-Dialog.
- **Tasten in Fenstern mit Administratorrechten sieht die App nicht**
  (UIPI), solange sie selbst nicht als Administrator läuft.
- **`<` (Adv.-Layout, C3) gibt es nur auf ISO-Tastaturen.** Auf
  US-ANSI-Tastaturen fehlt diese Taste.
- **Der Piano-Modus hat etwas Latenz.** Der GS Wavetable Synth reagiert
  hörbar langsamer als Apples Sampler. Für ernsthaftes Spielen den
  MIDI-Ausgang in eine DAW nutzen.

## Entwicklung

    dotnet test tests\MIDIKeys.Core.Tests            # Unit-Tests
    dotnet run --project src\MIDIKeys -- --show      # starten, Cheatsheet gleich offen
    dotnet run --project src\MIDIKeys -- --render-icons .\out   # Tray-Symbole als PNG

| Projekt | Inhalt |
|---|---|
| `src/MIDIKeys.Core` | Plattformfreie Logik, 1:1 aus `MIDIKeysCore` portiert: Keymaps, TapPolicy, InputController, MIDI-Engine, Router, Akkorderkennung, Zustand |
| `src/MIDIKeys` | WinForms-Tray-App: Low-Level-Tastatur-Hook, winmm/teVirtualMIDI, GS-Synth, Cheatsheet |
| `tests/MIDIKeys.Core.Tests` | xUnit-Port der Swift-Tests plus Windows-spezifische Regeln |

Die Klaviatur ist über **Scancodes** (Set 1) definiert, das Gegenstück
zu den macOS-Keycodes. Damit bleibt die Geometrie unabhängig vom Layout.

Diagnose-Log: `%TEMP%\midikeys.log`. Mit `MIDIKEYS_LOG_MIDI=1` wird jede
gesendete MIDI-Nachricht protokolliert, nützlich ohne DAW. Mit
`MIDIKEYS_PIN_POPUP=1` bleibt das Cheatsheet offen (für Bildschirmfotos).
Einstellungen liegen in `%APPDATA%\MIDIKeys\settings.json`.

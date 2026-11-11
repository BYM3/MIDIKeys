# MIDIKeys

Verwandelt die Mac-Tastatur in ein MIDI-Keyboard. Sitzt in der Menüleiste,
kein Fenster, kein Dock-Icon.

## Installieren

Braucht **macOS 14 oder neuer** (Apple Silicon oder Intel).

1. Unter **Releases** die Datei `MIDIKeys.zip` herunterladen und entpacken.
2. `MIDIKeys.app` in den Ordner **Programme** ziehen und öffnen.
3. macOS blockiert den ersten Start, weil die App nicht von Apple beglaubigt
   ist. Dann **Systemeinstellungen → Datenschutz & Sicherheit** öffnen, nach
   unten scrollen und bei „MIDIKeys“ auf **Dennoch öffnen** klicken.
   (Bis macOS 14 genügt auch Rechtsklick auf die App → *Öffnen*.)
4. Die Freigabe unter **Systemeinstellungen → Datenschutz & Sicherheit →
   Bedienungshilfen** erteilen. Ohne sie kann die App keine Tasten lesen.

Danach sitzt das Klavier-Symbol in der Menüleiste.

## Selbst bauen

    ./build.sh
    open ~/Applications/MIDIKeys.app

Braucht die Xcode Command Line Tools (Swift 6). Das Skript installiert die
App nach `~/Applications`.

## Bedienung

| Taste | Funktion |
|---|---|
| `a s d f g h j k l ö ä #` | weiße Tasten (C D E F G A H C D E F G) |
| `w e t z u o p +` | schwarze Tasten |
| `y` / `x` | Oktave ab / auf |
| `Tab` | Sustain-Pedal |
| `⌥-` (einstellbar) | MIDI-Modus an/aus |
| `Esc` `Esc` (innerhalb 0,5 s) | Notausstieg: MIDI-Modus aus |

Im aktiven Modus schluckt die App die Tastatur. **Kombinationen mit `⌘`
gehen immer durch** — `⌘⌥Esc` (Sofort beenden) funktioniert also jederzeit.
Ebenso die **Pfeiltasten** und die **Leertaste** (auch mit Shift/Option),
damit ein Programm im Hintergrund bedienbar bleibt — z. B. Play/Stop in der DAW.
Mit MIDI-Ausgang (also ohne Piano) gehen außerdem **Return/Enter** und die
**Löschtasten** `⌫`/`⌦` durch. Im Grundlayout (ohne Adv.) lassen sich in den
Einstellungen zusätzlich die Tasten **`c`** und **`r`** freigeben (z. B. für Ableton).
Friert die App ein, schaltet macOS den Event-Tap nach etwa einer Sekunde
selbst ab und die Tastatur kommt von allein zurück.

In der DAW erscheint der Eingang als **MIDIKeys**.

Beim ersten Start trägt sich die App einmalig als **Anmeldeobjekt** ein und
öffnet sich danach bei jeder Anmeldung. Abschalten lässt sich das in den
Systemeinstellungen unter *Allgemein › Anmeldeobjekte* oder über das
Zahnrad neben „Beenden“ (*Beim Anmelden öffnen*) — die App legt den Eintrag
danach nicht wieder an.

Die Oberfläche gibt es auf **Deutsch** und **Englisch**, umschaltbar über das
Zahnrad. Ohne eigene Wahl folgt sie der Systemsprache. Auf Englisch heißen
Notennamen B statt H, Umkehrungen „1st inv.“ statt „1. Umk.“.

## Entwicklung

    swift test                           # Unit-Tests
    swift run MIDIKeys --render-icons    # Menüleisten-Icons als PNG

Die Klaviatur ist über physische Keycodes definiert, nicht über Zeichen —
die Geometrie bleibt damit layoutunabhängig. Nur die Beschriftung im
Cheatsheet wird zur Laufzeit aus dem aktiven Tastaturlayout aufgelöst.

### Signatur und die Bedienungshilfen-Freigabe

`build.sh` sucht automatisch ein Code-Signing-Zertifikat im Schlüsselbund.
Das ist wichtig, weil macOS die Freigabe an das Erkennungsmerkmal der
Signatur bindet:

| Signatur | Erkennungsmerkmal | Folge |
|---|---|---|
| ad-hoc (`-`) | der cdhash | Jede Code-Änderung erzeugt einen neuen cdhash — die Freigabe muss nach **jedem** Rebuild neu erteilt werden |
| Zertifikat | Bundle-ID + Zertifikat | Bleibt über Rebuilds stabil — **einmal** freigeben genügt |

Eine bestimmte Identität erzwingen:

    MIDIKEYS_SIGN_IDENTITY="Name des Zertifikats" ./build.sh

Wird nichts gefunden, signiert das Skript ad-hoc und warnt entsprechend.

Wechselt die Signaturidentität, sieht macOS eine andere App. Dann einmal
zurücksetzen und neu freigeben:

    tccutil reset Accessibility com.bym3.midikeys

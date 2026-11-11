#!/usr/bin/env bash
# Baut MIDIKeys und schnürt daraus ein startklares .app-Bundle.
set -euo pipefail

cd "$(dirname "$0")"

APP_NAME="MIDIKeys"
# Lokale, nicht eingecheckte Überschreibungen (z. B. eine eigene Bundle-ID,
# damit eine bestehende Bedienungshilfen-Freigabe gültig bleibt).
if [[ -f build.local ]]; then source build.local; fi
BUNDLE_ID="${MIDIKEYS_BUNDLE_ID:-com.bym3.midikeys}"
VERSION="1.0"
# Die App lebt an einem stabilen Ort, NICHT im Build-Ordner: macOS bindet
# die Bedienungshilfen-Freigabe an das Bundle. Ein Pfad, der bei jedem Build
# geloescht wird, entwertet die Freigabe jedes Mal.
INSTALL_DIR="${MIDIKEYS_INSTALL_DIR:-$HOME/Applications}"
APP="${INSTALL_DIR}/${APP_NAME}.app"

# Die Signaturidentität entscheidet, ob die Bedienungshilfen-Freigabe
# Rebuilds übersteht:
#   - ad-hoc  -> Erkennungsmerkmal ist der cdhash. Jede Code-Änderung
#                erzeugt einen neuen cdhash, und macOS fragt erneut.
#   - Zertifikat -> Erkennungsmerkmal ist Bundle-ID + Zertifikat.
#                Bleibt über Rebuilds stabil.
# Darum wird automatisch eine vorhandene Identität gesucht.
if [[ -n "${MIDIKEYS_SIGN_IDENTITY:-}" ]]; then
    SIGN_IDENTITY="${MIDIKEYS_SIGN_IDENTITY}"
else
    SIGN_IDENTITY="$(security find-identity -v -p codesigning 2>/dev/null \
        | sed -n 's/.*"\(.*\)"$/\1/p' | head -1)"
    SIGN_IDENTITY="${SIGN_IDENTITY:--}"
fi

# MIDIKEYS_UNIVERSAL=1 baut für Apple Silicon und Intel zugleich (für
# Releases); Standard ist nur die Architektur dieses Macs.
ARCH_FLAGS=()
if [[ -n "${MIDIKEYS_UNIVERSAL:-}" ]]; then ARCH_FLAGS=(--arch arm64 --arch x86_64); fi

echo "==> Baue Release"
swift build -c release --product "${APP_NAME}" ${ARCH_FLAGS[@]+"${ARCH_FLAGS[@]}"}
BIN="$(swift build -c release --product "${APP_NAME}" ${ARCH_FLAGS[@]+"${ARCH_FLAGS[@]}"} --show-bin-path)/${APP_NAME}"

# Das Bundle wird in-place aktualisiert. Ein "rm -rf" wuerde die
# Bundle-Identitaet zerstoeren und die Freigabe mit ihr.
if [[ -d "${APP}" ]]; then
    echo "==> Aktualisiere bestehendes Bundle (Freigabe bleibt erhalten)"
else
    echo "==> Lege Bundle neu an: ${APP}"
    mkdir -p "${APP}/Contents/MacOS" "${APP}/Contents/Resources"
fi
mkdir -p "${INSTALL_DIR}"
mkdir -p "${APP}/Contents/Resources"
cp "${BIN}" "${APP}/Contents/MacOS/${APP_NAME}"
cp "Resources/AppIcon.icns" "${APP}/Contents/Resources/AppIcon.icns"

cat > "${APP}/Contents/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleName</key><string>${APP_NAME}</string>
    <key>CFBundleDisplayName</key><string>${APP_NAME}</string>
    <key>CFBundleIdentifier</key><string>${BUNDLE_ID}</string>
    <key>CFBundleVersion</key><string>${VERSION}</string>
    <key>CFBundleShortVersionString</key><string>${VERSION}</string>
    <key>CFBundlePackageType</key><string>APPL</string>
    <key>CFBundleExecutable</key><string>${APP_NAME}</string>
    <key>CFBundleIconFile</key><string>AppIcon</string>
    <key>LSMinimumSystemVersion</key><string>14.0</string>
    <!-- Kein Dock-Icon, kein App-Umschalter-Eintrag. -->
    <key>LSUIElement</key><true/>
</dict>
</plist>
PLIST

printf 'APPL????' > "${APP}/Contents/PkgInfo"

echo "==> Signiere (${SIGN_IDENTITY})"
codesign --force --sign "${SIGN_IDENTITY}" --timestamp=none "${APP}"
codesign --verify --verbose "${APP}"

# Sichtbar machen, woran macOS die App wiedererkennt.
REQ="$(codesign -d --requirements - "${APP}" 2>&1 | grep designated || true)"
echo "==> Erkennungsmerkmal: ${REQ#*designated => }"
if [[ "${REQ}" == *cdhash* ]]; then
    echo
    echo "WARNUNG: ad-hoc signiert — die Bedienungshilfen-Freigabe gilt nur"
    echo "         fuer genau diesen Build. Nach jeder Code-Aenderung muss"
    echo "         der Haken neu gesetzt werden."
    echo "         Abhilfe: mit einem Zertifikat signieren, z. B."
    echo "         MIDIKEYS_SIGN_IDENTITY=\"Name des Zertifikats\" ./build.sh"
fi

# Finder cached bislang moeglicherweise das generische Icon fuer diesen Pfad.
# Ein sanfter Anstoss (kein "killall Finder", das wuerde offene Fenster
# schliessen) reicht meist, damit das neue Icon sofort erscheint.
touch "${APP}"
osascript -e 'tell application "Finder" to update (POSIX file "'"${APP}"'" as alias)' \
    >/dev/null 2>&1 || true

echo
echo "Fertig: ${APP}"
echo "Starten mit:  open ${APP}"
echo "Zeigt Finder noch das alte Icon: einmal 'killall Finder' oder abmelden/anmelden."

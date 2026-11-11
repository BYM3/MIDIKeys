APP ICON - Klavier-Keycap
===========================

Quelle: brand/logo.svg (Vektor, 1024 x 1024, transparent)
Alle PNGs sind direkt aus dieser Geometrie in der jeweiligen Zielgroesse gerendert.

FARBEN
  #0B0B0C   Rahmen
  #F5F5F6   Tastenflaeche
  #141416   Motiv-Linien und Tasten
  #F7F7F7   Hintergrund (deckende Exporte)

iOS / iPadOS  ->  ios/
  AppIcon-1024.png   Master fuer den App Store und Xcode (quadratisch, deckend,
                     ohne eigene runde Ecken - iOS maskiert selbst)
  AppIcon-180 ... 20 fuer manuelle Asset-Kataloge

ANDROID  ->  android/
  adaptive-foreground-432.png   Vordergrund-Layer, transparent
  adaptive-background-432.png   Hintergrund-Layer, deckend #F7F7F7
                                Motiv liegt komplett in der 66 %-Sicherheitszone
  playstore-512.png             Play-Store-Eintrag
  legacy/ic_launcher-192 ... 48  fuer aeltere Launcher

WEB / PWA  ->  web/
  favicon-16.png, favicon-32.png
  apple-touch-icon-180.png
  pwa-192.png, pwa-512.png
  maskable-512.png              mit zusaetzlichem Rand fuer Browser-Masken

MARKE  ->  brand/
  logo.svg                      Vektor-Quelle
  logo-1024/512/256/128.png     transparent, fuer alles Weitere

HINWEIS
  Unter etwa 40 px verliert das Klavier-Motiv naturgemaess Details.
  Fuer Favicon-Groessen ist eine vereinfachte Variante empfehlenswert.

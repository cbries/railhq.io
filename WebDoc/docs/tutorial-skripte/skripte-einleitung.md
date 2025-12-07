---
sidebar_position: 1
---

# Einleitung

## Was sind `railhq.io` - Skripte?

Mit railhq.io – Skripten kannst du beliebige Aktionen auf deiner Modellbahnanlage automatisch ausführen lassen. Skripte werden in JavaScript geschrieben und ermöglichen dir eine flexible Steuerung – von einfachen Befehlen bis hin zu komplexer Logik.

Ein typisches Skript kann z. B. Weichen stellen, Fahrstraßen aktivieren, Wartezeiten einbauen oder Signale setzen – alles gesteuert direkt im Browser.

Skripte machen dein digitales Stellwerk noch intelligenter – ganz ohne komplizierte Konfiguration. Du entscheidest, wann was passiert.

### Vorteile

- Keine lokale Installation nötig
  - Alles läuft im Browser – kein Upload, keine Plugins.
  - Die erforderlichen Systembibliotheken werden entweder direkt von `railhq.io` ausgeliefert oder sind schon ein Teil von Eurer Browserinstallation.

- Direkt ausführbar
  - Skripte lassen sich per Button im Gleisplan starten oder automatisch bei bestimmten Ereignissen.
  - Die jeweils gestarteten Skripte lassen sich jederzeit beenden.
  - Die jeweiligen Skripte laufen autark voneinander ab, d.h. es gibt keine Kopplung zwischen ein oder mehreren Skripten.
  - Nur ein Skript gleichzeitig pro Button: Ein Klick auf den Button startet das Skript nur, wenn es noch nicht läuft.

- Modular und sicher
  - Alle Skripte laufen isoliert in einer geschützten Umgebung. Die Steuerung erfolgt über die zentrale hq-API.
  - Fehlerbehandlung integriert: Fehler im Skript werden im UI sichtbar angezeigt.
  - Skripte als Komponenten: Skripte können direkt auf Kacheln (Tiles) gelegt oder aus externen Events gestartet werden.

- Live-Entwicklung mit Intellisense
  - Dank Monaco-Editor stehen dir Autovervollständigung, Typinformationen und Fehlermeldungen zur Verfügung.
  

### Features im Detail

- Skript-Start per Button direkt auf dem Gleisplan
- Mehrfachstart verhindern – ein Skript kann nur einmal gleichzeitig laufen
- optionaler Start von Skripten beim Laden des Gleisplan, so dass Standardansichten direkt geladen undangezeigt werden
- Globale Handhabung der unabhängigen Skript-Tasks – zentrale Verwaltung von Status, Start/Stopp und laufenden Skripten
- REST API & Events – Skripte können auch externe Schnittstellen nutzen, ebenso kann die Steuerung durch eigene Implementierungen mit der Verwendung unserer REST API erweitert werden
- Das Skripting erlaubt das direkt Einbinden von Webcams oder Mjpeg-Streams.

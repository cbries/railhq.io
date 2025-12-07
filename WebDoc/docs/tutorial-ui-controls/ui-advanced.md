---
sidebar_position: 10
---

# 🧠 Erweitertes Wissen

Die `hqUi`-Architektur von `railhq.io` ist ein durchdachtes, flexibles UI-System, das speziell für den Einsatz im Modelleisenbahn-Kontext entwickelt wurde. Hier sind einige zentrale und erwähnenswerte Punkte:

## 🧩 Modular & Erweiterbar

- Komponentenbasiert: Neue UI-Controls (z. B. Buttons, Slider, Anzeige-Tiles) lassen sich leicht durch eigene JavaScript-Klassen erweitern und mit `hqUi.register(config)` hinzufügen.

- Einfache API: Alle Komponenten folgen einem klaren Lebenszyklus (`createElement()`, `update()`, `destroy()`), was eine einfache Anbindung und Pflege ermöglicht.

## 🧠 Script-Integration & Dynamik

- Alle Komponenten können dynamisch über das Scripting-System gesteuert werden.

- Skripte können:
  - Komponenten nachträglich erzeugen oder löschen
  - Zustände oder Werte ändern
  - auf Ereignisse im UI reagieren (z. B. Klicks, Slider-Änderungen)

## 🎨 Layout durch Koordinaten

- Jede Komponente erhält über `x` und `y` eine feste Position im Grid des Gleisplans.

- Die Größe kann über `width`, `height` und `ratio` flexibel angepasst werden.

- Positionen sind immer tilebasiert (Schritte à 32px).

## 🖱️ Interaktive Elemente

Draggable Controls: Mit `draggable: true` können Komponenten vom Benutzer verschoben werden.

Live UI-Verhalten: Z. B. wird ein `toggle` direkt visuell aktualisiert, ein `slider` feuert beim Bewegen ein Event.

## 🔌 Spezialkomponenten (Beispiele)

- `button` – Löst eine benutzerdefinierte Funktion oder ein Skript aus.

- `slider` – Steuerung von Werten, z. B. Helligkeit, Geschwindigkeit.

- `mjpeg` / `snapshot` – Zeigt periodisch aktualisierte Bilder (Snapshot) oder MJPEG-Streams.

- `clock` – Bahnhofsähnliche Uhranzeige.

- `tile` – Basisanzeige für Status-/Sensordaten.

##  Styling & Icons

- Font Awesome 5.15.4 (Free) ist eingebunden.

- Icons können über `icon: "fas fa-plug"` gesetzt werden.

- Layout und Farben können per CSS erweitert werden (eigene Klassen wie `.data-tile`, `.button-tile`, etc.).

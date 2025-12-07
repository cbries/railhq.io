---
sidebar_position: 1
---

# 🔧 Icons

Für die Darstellung von Icons innerhalb der UI-Tiles nutzt `railhq.io` die frei verfügbare Font Awesome Bibliothek in der Version 5.15.4 (Free-Version). Damit steht ein umfangreicher, aber begrenzter Satz an Icons zur Verfügung, der in allen Controls verwendet werden kann, die ein icon-Feld unterstützen (z. B. tile, button, toggle, slider).

## Beispiel: Icon setzen

```javascript
hqUi.register({
  id: "stromanschluss",
  type: "tile",
  label: "Spannung",
  icon: "fas fa-plug", // Font Awesome Icon-Klasse
  unit: "V",
  value: "14.2",
  x: 3,
  y: 2
});
```

:::note

Die Angabe erfolgt als CSS-Klasse. Üblich ist dabei "fas fa-ICONNAME" für Solid Icons. Andere Varianten wie "far" (Regular) oder "fab" (Brands) sind in der Free-Version nur eingeschränkt verfügbar.

:::

## Nützliche Ressourcen

- Übersicht über alle verfügbaren Icons:

 👉 https://fontawesome.com/v5.15/icons?d=gallery&m=free

- Die Icons können bei Bedarf auch direkt über eigene CSS-Klassen oder über innerHTML individuell erweitert werden.

## Beliebte Font Awesome Icons (Version 5.15.4 Free)

Hier sind einige häufig genutzte Icons, die du direkt mit `"fas fa-..."` in deinem icon-Feld verwenden kannst:

| Vorschau | Klassenname               | Bedeutung               |
| -------- | ------------------------- | ----------------------- |
| 🔌       | `fas fa-plug`             | Strom, Energieanschluss |
| 💡       | `fas fa-lightbulb`        | Licht, Idee             |
| 📷       | `fas fa-camera`           | Kamera, Foto            |
| 🎥       | `fas fa-video`            | Video, Live-Stream      |
| ▶️       | `fas fa-play`             | Start, Ausführen        |
| ⏹️       | `fas fa-stop`             | Stop, Anhalten          |
| ⏲️       | `fas fa-clock`            | Uhrzeit, Zeitsteuerung  |
| 🔧       | `fas fa-wrench`           | Wartung, Konfiguration  |
| 🖲️       | `fas fa-toggle-on`        | Schalter (an)           |
| 🌡️       | `fas fa-thermometer-half` | Temperaturanzeige       |

:::note

Du kannst die Vorschau dieser Icons auch direkt in deinem Browser testen, indem du ein einfaches HTML-Element mit der Klasse renderst:
<i class="fas fa-camera"></i>

:::


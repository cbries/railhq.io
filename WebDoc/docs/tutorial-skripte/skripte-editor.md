---
sidebar_position: 5
---

# 💻 Skripteditor

## 💻 Skripteditor in `railhq.io`

Der integrierte Skripteditor von `railhq.io` basiert auf dem leistungsstarken Monaco Editor — derselben Code-Engine, die auch in Visual Studio Code zum Einsatz kommt. Damit bietet er eine professionelle und vertraute Entwicklungsumgebung direkt im Browser.

![Ansicht des Skripteditor](img/001.png)

## ✨ Features im Überblick

| Feature                        | Beschreibung                                                                             |
| ------------------------------ | ---------------------------------------------------------------------------------------- |
| 🧠 **IntelliSense**            | Automatische Codevervollständigung für alle `hq.*`-APIs inkl. Dokumentation              |
| 🎨 **Syntax-Highlighting**     | JavaScript-Highlighting auf Basis der Monaco-Engine                                      |
| 🛡 **Fehlerprüfung**           | Echtzeitprüfung auf Syntaxfehler oder fehlende Befehle                                   |
| 📖 **Dokumentation im Editor** | Hover-Tooltips mit Funktionsbeschreibungen und Parametertypen                            |
| 🏁 **Codeausführung**          | Skripte können manuell gestartet werden oder automatisch bei bestimmten Events           |
| 📦 **Script Snippets**         | Vorgefertigte Beispiele und Befehlsbausteine als Startpunkt                              |
| 📂 **Persistenz**              | Skripte werden im Workspace gespeichert und können geladen, kopiert oder gelöscht werden |

## ⚙️ Unterstützung für eigene APIs

Der Editor ist speziell auf `railhq.io` zugeschnitten und kennt:

- Alle zentralen API-Methoden, z. B.:
  - `hqSensors.on(..)`
  - `hqLocomotive.stop(..)`
  - `hqLocomotive.setDirectionAndSpeed(..)`
  - `hqAccessory.switch(..)`
  - `hqMqttClient.publish(..)`

- Globale Objekte wie hqSensors, hqLocomotive, hqAccessory, hqUi, usw.

- Kontextabhängige Vorschläge und Tooltips je nach Schreibposition

🧠 Beispiel: IntelliSense in Aktion

Beim Tippen von `hqSensors.` schlägt der Editor automatisch alle verfügbaren Methoden vor, inkl. Beschreibung und Parametertypen.

![Intellisense für hqSensors](img/002-intellisense.gif)

Ein Beispielskript wie die Handhabung mit `hqSensors.on(..)` aussieht folgt im nachfolgenden Beispiel. In den Zeilen `5` und `12` werden jeweils ein Trigger konfiguriert, der dann reagiert wenn entweder der Sensor aktiv wird (also `true`) oder wieder inaktiv wird (also `false`). Was dann passieren soll, wird über einen sog. Callback implementiert. Bei der Implementierung könnt Ihr Euch richtig austoben und alles machen was Euch einfällt.

```javascript showLineNumbers
const data = {
    driver: "z21", fbPort: 2, fbPinRight: 2, fbPinLeft: 1, locAddr: 9, locSpeed: 45
};

hqSensors.on(data.driver, data.fbPort, data.fbPinRight, true, async (ev) => {
    log(`RECHTS ${ev.driver}::${ev.port}::${ev.pin} ist AN`);
    await hqLocomotive.stop(data.driver, data.locAddr);
    await hqHelper.sleep1Sec();
    await hqLocomotive.setDirectionAndSpeed(data.driver, data.locAddr, false, data.locSpeed);
});

hqSensors.on(data.driver, data.fbPort, data.fbPinLeft, true, async (ev) => {
    log(`LINKS ${ev.driver}::${ev.port}::${ev.pin} ist AN`);
    await hqLocomotive.stop(data.driver, data.locAddr);
    await hqHelper.sleep1Sec();
    await hqLocomotive.setDirectionAndSpeed(data.driver, data.locAddr, true, data.locSpeed);
});
```

## 📌 Fazit

Der Skripteditor von `railhq.io` ist ein zentrales Werkzeug zur Automatisierung und Steuerung komplexer Modellbahnszenarien. Dank der Integration des Monaco Editors steht ein vertrautes und leistungsfähiges Tool zur Verfügung, das keine Wünsche offenlässt – von der Codehilfe bis zur direkten Integration in das Gleisplan-System.

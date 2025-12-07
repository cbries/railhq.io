---
sidebar_position: 1
---

# Auffahrblöcke (Staging)

## Was sind Auffahrblöcke?

Auffahrblöcke (auch „Aufstellblöcke“ oder „Einfahrblöcke“ genannt) sind Abschnitte in einem Schattenbahnhof oder Rangierbereich, in denen Züge nacheinander hintereinander einfahren und warten können. Die Idee ist, dass mehrere Züge platzsparend auf einem Gleisstrang stehen können, ohne sich gegenseitig zu behindern. Dabei ist jeder Auffahrblock ein eigenständiger Abschnitt mit eigenem Rückmelder (z. B. S88-Kontakt).

## Typische Merkmale:

- **Feste Richtung:** Züge fahren immer nur in eine Richtung durch die Kette der Blöcke.

- **Sequenzielles Einfahren:** Ein Zug darf nur in den nächsten Block einfahren, wenn dieser frei ist und ggf. der vorletzte bereits belegt ist (um die Reihenfolge korrekt zu halten).

- **Blockbildung:** Es gibt mehrere hintereinander liegende kurze Blöcke auf demselben physikalischen Gleis.

- **Ausfahrlogik:** Der Zug ganz vorne darf zuerst wieder ausfahren – oft über eine Kettenlogik oder ein Skript gesteuert.

- **Rückmeldung:** Jeder Block hat eine eigene Rückmeldung, um Anwesenheit eines Zuges zu erkennen (z. B. über Stromfühler, Gleisbesetztmelder oder Reedkontakte).

## Beispiel:

Ein Schattenbahnhofsgleis mit 3 Auffahrblöcken:

```
[Block A1] --> [Block A2] --> [Block A3]
      ↑            ↑             ↑
   Melder 1     Melder 2      Melder 3
```

- Züge fahren von links nach rechts ein.

- Sobald Block A1 belegt ist, wird der nächste Zug in A2 aufgefahren usw.

- Die Ausfahrt erfolgt dann typischerweise in der Reihenfolge: A3 → A2 → A1 (je nachdem, wie man es steuert).



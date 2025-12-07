// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSample.driveBackAndForth",
    description: "Lässt eine Lokomotive wiederholt vorwärts und rückwärts fahren, jeweils für 5 Sekunden"
};

const driverName = "z21";
const dccAddr = 9;
const speed = 35;

//
// Fährt eine Lokomotive für 5 Sekunden 2 mal hin & her.
//
await driveBackAndForth(driverName, dccAddr, speed, 5, 2, {
  onStart: function(direction, index) {
    console.log("Startet " + direction + " in Durchgang " + (index + 1));
  },
  onStop: function(direction, index) {
    console.log("Beendet " + direction + " in Durchgang " + (index + 1));
  }
});

/**
 * Lässt eine Lokomotive wiederholt vor- und rückwärts fahren.
 * 
 * @param {string} driver - Name des Steuergeräts
 * @param {number} locAddr - Adresse der Lok
 * @param {number} speed - Geschwindigkeit (0-100)
 * @param {number} durationSec - Fahrdauer pro Richtung (Sekunden)
 * @param {number} repeat - Anzahl der Wiederholungen
 * @param {object} callbacks - Optional, mit onStart(direction, index) und onStop(direction, index)
 */
async function driveBackAndForth(driver, locAddr, speed, durationSec = 5, repeat = 2, callbacks = {}) {
  const { onStart, onStop } = callbacks;

  for(let i = 0; i < repeat; ++i) {
    log("Durchgang " + (i + 1) + " von " + repeat + ": Lok fährt " + durationSec + " Sekunden vorwärts");
    if (onStart) onStart('forward', i);
    try {
      await hqLocomotive.setDirectionAndSpeed(driver, locAddr, true, speed);
      await hqHelper.sleep(durationSec * 1000);
      hqLocomotive.stop(driver, locAddr);
    } catch(e) {
      log("Fehler beim Vorwärtsfahren: " + e.message);
      return;
    }
    if (onStop) onStop('forward', i);

    await hqHelper.sleep(durationSec * 1000);

    log("Durchgang " + (i + 1) + " von " + repeat + ": Lok fährt " + durationSec + " Sekunden rückwärts");
    if (onStart) onStart('backward', i);
    try {
      await hqLocomotive.setDirectionAndSpeed(driver, locAddr, false, speed);
      await hqHelper.sleep(durationSec * 1000);
      hqLocomotive.stop(driver, locAddr);
    } catch(e) {
      log("Fehler beim Rückwärtsfahren: " + e.message);
      return;
    }
    if (onStop) onStop('backward', i);

    await hqHelper.sleep(durationSec * 1000);
  }
}

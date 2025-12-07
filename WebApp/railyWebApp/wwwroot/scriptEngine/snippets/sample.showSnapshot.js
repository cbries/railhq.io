// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSample.showSnapshot",
    description: "Zeigt regelmäßig aktualisierte Snapshot-Kameras als Kachel im Gleisplan."
};

hqUi.injectDefaultStyles();

hqUi.register({
    id: "snapshot1",
    type: "snapshot",
    label: 'Bielefeld',
    url: 'https://www.bicos.de/webcam/webcam1.jpg',
    x: 1,
    y: 1,
    ratio: 4 / 3,
    width: 9,           // in Tiles (32px * 10)
    intervalMsec: 4000      // alle 4 Sekunden neues Bild
});

hqHelper.finalize = async () => {
    log("Benutzerdefinierter Cleanup läuft...");
    hqUi.finalize();
};

// Halte das Skript am Leben, bis "Stop" gedrückt wurde
while (!api.shouldStop?.()) {
    await new Promise(resolve => setTimeout(resolve, 500));
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSnippet.keepAliveLoop",
    description: "Wartet in einer Schleife, bis das Programm sagen soll: Jetzt beenden!"
};

// Halte das Skript am Leben, bis "Stop" gedrückt wurde
while (!api.shouldStop?.()) {
    await new Promise(resolve => setTimeout(resolve, 500));
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSnippet.finalize",
    description: "Führt Aufräumarbeiten aus, bevor das Programm beendet wird"
};

hqHelper.finalize = async () => {
    log("Benutzerdefinierter Cleanup läuft...");
};

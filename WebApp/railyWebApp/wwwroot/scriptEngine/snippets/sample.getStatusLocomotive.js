// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

const snippetMetainformation = {
    label: "hqSample.getLocomotiveStatus",
    description: "Fragt den aktuellen Namen und Status einer Lokomotive ab und zeigt das Ergebnis an."
};

const driverName = "ecos";
const dccAddr = 3;

hqLocomotive.getStatus(driverName, dccAddr).then(status => {
  log(status.driverName + "::" + status.address + " => " + status.name);
});

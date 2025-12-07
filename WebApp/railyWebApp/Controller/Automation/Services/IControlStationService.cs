// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Threading.Tasks;
using railyWebApp.Controller.Automation.Dto.Entity;

namespace railyWebApp.Controller.Automation.Services
{
    public interface IControlStationService
    {
        // === Verwaltung ===

        /// <summary>
        /// Gibt eine Liste aller verfügbaren Zentralen zurück (z.B. "ecos", "z21", "demo").
        /// </summary>
        Task<List<string>> GetAvailableStationsAsync();

        /// <summary>
        /// Gibt Details zur aktuellen Zentrale zurück (z.B. Modell, Firmware, Verbindung).
        /// </summary>
        Task<ControlStationInfo> GetInfoAsync(string driverName);

        // === Steuerung ===

        /// <summary>
        /// Schaltet die Fahrspannung ein oder aus.
        /// </summary>
        Task<bool> SetPowerAsync(string driverName, bool on);

        // === Status ===

        /// <summary>
        /// Gibt zurück, ob die Fahrspannung aktuell an oder aus ist.
        /// </summary>
        Task<bool?> GetPowerStatusAsync(string driverName);
    }
}

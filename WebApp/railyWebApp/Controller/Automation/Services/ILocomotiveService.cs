// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using railyWebApp.Controller.Automation.Dto.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace railyWebApp.Controller.Automation.Services
{
    public interface ILocomotiveService
    {
        // === Verwaltung ===

        Task<List<Locomotive>> GetAllLocomotivesAsync();
        Task<Locomotive> GetLocomotiveAsync(string driverName, int address);

        // === Steuerung ===

        Task<bool> SetSpeedAsync(string driverName, int address, int speed);
        Task<bool> SetDirectionAsync(string driverName, int address, bool forward);
        Task<bool> StopLocomotiveAsync(string driverName, int address);
        Task<bool> ToggleFunctionAsync(string driverName, int address, int functionNumber, bool state);

        // === Status / Verlauf ===

        Task<LocomotiveState> GetStatusAsync(string driverName, int address);
    }

}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class ControlStationInfo
    {
        [JsonProperty("driverName")] public string DriverName { get; set; } = string.Empty;
        [JsonProperty("model")] public string Model { get; set; } = string.Empty;
        [JsonProperty("firmware")] public string FirmwareVersion { get; set; } = string.Empty;
        [JsonProperty("hardware")] public string HardwareVersion { get; set; } = string.Empty;
        [JsonProperty("isRunning")] public bool IsRunning { get; set; }
    }
}

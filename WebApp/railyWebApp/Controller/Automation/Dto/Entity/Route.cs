// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class Route
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("isEnabled")] public bool IsEnabled { get; set; }
        [JsonProperty("isLocked")] public bool IsLocked { get; set; }
        [JsonProperty("isOccupied")] public bool IsOccupied { get; set; }
        [JsonProperty("isOccupiedReason")] public string IsOccupiedReason { get; set; }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class LocomotiveState
    {
        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("address")] public int Address { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("decoderType")] public string DecoderType { get; set; }
        [JsonProperty("isDriving")] public bool IsDriving { get; set; }
        [JsonProperty("isFunctionAvailable")] public bool IsFunctionAvailable { get; set; }
        [JsonProperty("speed")] public int Speed { get; set; }
        [JsonProperty("speedSteps")] public int SpeedSteps { get; set; }
        [JsonProperty("direction")] public int Direction { get; set; }
        [JsonProperty("functions")] public Dictionary<int, bool> Functions { get; set; } = new();
        [JsonProperty("blockId")] public string BlockId { get; set; }
        [JsonProperty("automationState")] public string AutomationState { get; set; }
        [JsonProperty("tags")] public List<string> Tags { get; set; }
    }


}

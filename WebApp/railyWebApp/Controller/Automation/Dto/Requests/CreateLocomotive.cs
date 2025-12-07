// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Requests
{
    public class CreateLocomotive
    {
        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("address")] public int Address { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("maxSpeed")] public int MaxSpeed { get; set; }
        [JsonProperty("functionLabels")] public Dictionary<int, string> FunctionLabels { get; set; }
    }
}

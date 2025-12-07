// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity;

public class BlockSensor
{
    [JsonProperty("name")] public string Name { get; set; }
    [JsonProperty("provider")] public string Provider { get; set; }
    [JsonProperty("address")] public string Address { get; set; }
}
// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class Locomotive
    {
        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("address")] public int Address { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("decoderType")] public string DecoderType { get; set; }
    }
}

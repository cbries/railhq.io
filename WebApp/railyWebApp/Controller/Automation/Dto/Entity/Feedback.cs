// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class Feedback
    {
        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("module")] public int Module { get; set; }
        [JsonProperty("binaryState")] public string BinaryState { get; set; }
    }
}

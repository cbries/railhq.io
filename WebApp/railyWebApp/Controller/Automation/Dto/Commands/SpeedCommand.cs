// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Commands
{
    public class SpeedCommand
    {
        [JsonProperty("speed")] public int Speed { get; set; } // z. B. 0–100
    }
}

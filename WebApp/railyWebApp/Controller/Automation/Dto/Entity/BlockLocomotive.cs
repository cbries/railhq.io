// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity;

public class BlockLocomotive
{
    [JsonProperty("driverName")] public string DriverName { get; set; }
    [JsonProperty("objectId")] public int ObjectId { get; set; }
    [JsonProperty("enterSide")] public string EnterSide { get; set; }
}
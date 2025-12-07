// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity
{
    public class Block
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("plus")] public BlockDirectional Plus { get; set; }
        [JsonProperty("minus")] public BlockDirectional Minus { get; set; }
    }
}

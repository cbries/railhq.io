// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace libZ21.EntitiesPredefined
{
    public class FunctionEntry
    {
        [JsonProperty("idx")] public int Index { get; set; } = -1;
        [JsonProperty("fncIdx")] public int FunctionIndex { get; set; } = -1;
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        [JsonProperty("isUsed")] public bool IsUsed { get; set; } = false;
        [JsonProperty("icon")] public string Icon { get; set; } = string.Empty;
    }

    public class PredefinedEntityLocomotive : IPredefinedEntity
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("address")] public int Address { get; set; } = -1;
        [JsonProperty("protocol")] public string Protocol { get; set; } = string.Empty;
        [JsonProperty("functions")] public List<FunctionEntry> Functions { get; set; } = new();

        [JsonIgnore] public EntityFileType EntityType => EntityFileType.Locomotive;
    }
}

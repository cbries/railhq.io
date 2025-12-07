// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libZ21.EntitiesPredefined
{
    public class PredefinedEntityAccessory : IPredefinedEntity
    {
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("address")] public int Address { get; set; } = -1;
        [JsonProperty("protocol")] public string Protocol { get; set; } = string.Empty;
        [JsonProperty("acctype")] public string AccType { get; set; } = string.Empty;

        [JsonIgnore] public EntityFileType EntityType => EntityFileType.Accessory;
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libShared.Entities
{
    public enum EntityType
    {
        None = 1,
        Accessory = 2,
        Locomotive = 3,
        Ecos2 = 4,
        S88 = 5,
        Z21Station
    }

    public interface IEntity : IEntityBase
    {
        [JsonProperty("objectId")]  int ObjectId { get; }
        [JsonProperty("type")]  EntityType Type { get; }
        [JsonProperty("displayName")] string DisplayName { get; set; }
        [JsonProperty("isEnabled")] bool IsEnabled { get; set; }

        [JsonProperty("name0")] string Name0 { get; set; }
        [JsonProperty("name1")] string Name1 { get; set; }
        [JsonProperty("name2")] string Name2 { get; set; }
    }
}

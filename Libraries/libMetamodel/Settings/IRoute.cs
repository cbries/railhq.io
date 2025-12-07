// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace libMetamodel.Settings
{
    public interface IRoute : IStateBase
    {
        [JsonProperty("name")] string Name { get; set; }
        [JsonProperty("uid")] string Uid { get; set; }
        [JsonProperty("isOccupied")] bool IsOccupied { get; set; }
        [JsonProperty("isOccupiedReason")] string IsOccupiedReason { get; set; }
    }
}

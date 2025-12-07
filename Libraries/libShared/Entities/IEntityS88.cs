// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libShared.Entities
{
    public interface IEntityS88 : IEntityBase
    {
        [JsonProperty("port")] int Port { get; }
        [JsonProperty("maxPorts")] int MaxPorts { get; }
        [JsonProperty("pins")] int Pins { get; }
        [JsonProperty("hexState")] string HexState { get; }
        [JsonProperty("binaryState")] string BinaryState { get; }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libShared.MessageProtocol
{
    public class MessageSpeedstep : MessageBase
    {
        [JsonProperty("speed")] public int Speed { get; set; }
        [JsonProperty("maxSpeedSteps")] public string MaxSpeedSteps { get; set; }
        [JsonProperty("direction")] public int Direction { get; set; }
    }
}

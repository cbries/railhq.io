// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libShared.MessageProtocol
{
    public class MessagePower : MessageBase
    {
        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("action")] public string Action { get; set; }
    }
}

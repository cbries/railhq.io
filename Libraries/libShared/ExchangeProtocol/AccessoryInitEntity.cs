// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace libShared.ExchangeProtocol
{
    public class AccessoryInitEntity
    {
        [JsonProperty("addr")] public string Address { get; set; } = string.Empty;
        [JsonProperty("addrExt")] public List<string> AddrExt { get; set; } = new();
        [JsonProperty("objectId")] public int ObjectId { get; set; } = -1;
        [JsonProperty("driverName")] public string DriverName { get; set; } = string.Empty;
    }
}

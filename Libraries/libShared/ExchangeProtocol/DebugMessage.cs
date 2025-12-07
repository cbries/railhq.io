// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace libShared.ExchangeProtocol
{
    public class DebugMessage
    {
        [JsonProperty("command")] public string Command { get; set; } = "debugMessages";
        [JsonProperty("datetime")] public DateTime DateTime { get; set; } = DateTime.Now;
        [JsonProperty("priority")] public string Priority { get; set; } = "Info";
        [JsonProperty("messages")] public List<string> Messages { get; set; } = new();
    }
}

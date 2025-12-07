// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;

namespace libMetamodel.Settings
{
    public interface IDebugging
    {
        [JsonProperty("runtime")] bool Runtime { get; set; }
        [JsonProperty("accessories")] bool Accessories { get; set; }
        [JsonProperty("locomotives")] bool Locomotives { get; set; }
        [JsonProperty("routes")] bool Routes { get; set; }
        [JsonProperty("exceptions")] bool Exceptions { get; set; }
    }
}

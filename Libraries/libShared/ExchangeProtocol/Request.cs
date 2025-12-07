// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace libShared.ExchangeProtocol
{
    public class Request
    {
        /// <summary>
        /// Protocol Version
        /// </summary>
        [JsonProperty("version")] public string Version { get; set; } = "0.0";
        /// <summary>
        /// e.g. "ecos"
        /// </summary>
        [JsonProperty("extensionName")] public string ExtensionName { get; set; } = string.Empty;
        /// <summary>
        /// Time of creation, not when it is sent.
        /// </summary>
        [JsonProperty("timestamp")] public DateTime Timestamp { get; set; } = DateTime.Now;
        
        [JsonProperty("data")] public Data Data { get; set; } = new();
    }
}

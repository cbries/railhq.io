// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libShared.ExchangeProtocol;

public class Data
{
    [JsonProperty("type")] public string Type { get; set; } = "exchange";
    [JsonProperty("payload")] public object Payload { get; set; }
}
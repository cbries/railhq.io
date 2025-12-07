// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.Net;

namespace railyEsuEcos.Cfg
{
    internal class Cfg : ICfg
    {
        [JsonProperty("ecos")] public CfgTargetEcos CfgTargetEcos { get; set; } = new();
    }

    internal class CfgTargetEcos : ICfgTargetEcos
    {
        [JsonProperty("isEnabled")] public bool IsEnabled { get; set; } = false;
        [JsonIgnore] public IPAddress TargetIp { get; set; }

        [JsonProperty("ip")]
        public string Ip
        {
            get => TargetIp.ToString();
            set => TargetIp = IPAddress.Parse(value);
        }

        [JsonProperty("port")] public ushort TargetPort { get; set; } = 15471;

        [JsonProperty("delaySecondsReconnect")] public int DelaySecondsReconnect { get; set; } = 5;
    }

}

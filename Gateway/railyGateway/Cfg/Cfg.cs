// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;

namespace railyGateway.Cfg
{
    public class Cfg : ICfg
    {
        [JsonProperty("connection")] public IConnection Connection { get; set; } = new Connection();
        [JsonProperty("localHost")] public IHost LocalHost { get; set; } = new LocalHost();
    }

    public class Connection : IConnection
    {
        [JsonProperty("isEnabled")] public bool IsEnabled { get; set; } = true;
        [JsonProperty("timeoutSeconds")] public int TimeoutSeconds { get; set; } = 10;
        [JsonProperty("host")] public string Host { get; set; } = "railhq.io";
        [JsonProperty("port")] public int Port { get; set; } = 5001;
        [JsonProperty("username")] public string Username { get; set; }
        [JsonProperty("password")] public string Password { get; set; }
    }

    public class LocalHost : IHost
    {
        public int ListenPort { get; set; } = 8081;
        public string ListenDevice { get; set; } = "auto";
    }
}

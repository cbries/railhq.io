// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;

namespace railyWebApp.Cfg
{
    public interface IHost
    {
        [JsonProperty("isTls")] bool IsTls { get; }
        [JsonProperty("listenPort")] int ListenPort { get; }
        [JsonProperty("listenDevice")] string ListenDevice { get; }
        [JsonProperty("remoteHost")] string RemoteHost { get; }
    }
}

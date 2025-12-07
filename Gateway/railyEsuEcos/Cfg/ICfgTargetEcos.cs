// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Net;

namespace railyEsuEcos.Cfg
{
    internal interface ICfgTargetEcos
    {
        bool IsEnabled { get; set; }
        IPAddress TargetIp { get; set; }
        string Ip { get; set; }
        ushort TargetPort { get; set; }
        int DelaySecondsReconnect { get; set; }
    }
}

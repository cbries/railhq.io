// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json.Linq;

namespace railyGateway.Cfg
{
    internal interface ICfg
    {
        IConnection Connection { get; set; }
        IHost LocalHost { get; set; }
    }
}

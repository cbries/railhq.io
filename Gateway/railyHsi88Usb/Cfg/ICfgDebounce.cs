// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyHsi88Usb.Cfg
{
    internal interface ICfgDebounce
    {
        uint CheckInterval { get; }
        uint On { get; }
        uint Off { get; }
    }
}

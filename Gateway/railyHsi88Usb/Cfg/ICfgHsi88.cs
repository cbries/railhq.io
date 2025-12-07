// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyHsi88Usb.Cfg
{
    internal interface ICfgHsi88
    {
        bool IsEnabled { get; }
        ICfgSimulation CfgSimulation { get; }
        ushort NumberLeft { get; }
        ushort NumberMiddle { get; }
        ushort NumberRight { get; }
        int NumberMax { get; }
        string DevicePath { get; }
        ICfgDebounce CfgDebounce { get; }
    }
}

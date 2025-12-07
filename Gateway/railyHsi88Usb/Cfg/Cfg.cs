// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;

namespace railyHsi88Usb.Cfg
{
    internal class Cfg : ICfg
    {
        [JsonProperty("hsi")] public ICfgHsi88 CfgHsi88 { get; set; } = new CfgHsi88();
    }

    internal class CfgHsi88 : ICfgHsi88
    {
        [JsonProperty("isEnabled")] public bool IsEnabled { get; set; } = false;
        [JsonProperty("simulation")] public ICfgSimulation CfgSimulation { get; set; } = new CfgSimulation();
        [JsonProperty("left")] public ushort NumberLeft { get; set; } = 0;
        [JsonProperty("middle")] public ushort NumberMiddle { get; set; } = 0;
        [JsonProperty("right")] public ushort NumberRight { get; set; } = 0;
        [JsonIgnore] public int NumberMax => NumberLeft + NumberMiddle + NumberRight;
        [JsonProperty("devicePath")] public string DevicePath { get; set; } = @"\\.\HsiUsb1";
        [JsonProperty("debounce")] public ICfgDebounce CfgDebounce { get; set; } = new CfgDebounce();
    }

    internal class CfgDebounce : ICfgDebounce
    {
        [JsonProperty("checkIntervalMs")] public uint CheckInterval { get; set; }
        [JsonProperty("onMs")] public uint On { get; set; }
        [JsonProperty("offMs")] public uint Off { get; set; }
    }

    internal class CfgSimulation : ICfgSimulation
    {
        [JsonProperty("isEnabled")] public bool IsEnabled { get; set; } = false;
        [JsonProperty("simulationMode")] public string SimulationMode { get; set; } = "random";
        [JsonProperty("betweenTicksMs")] public int BetweenTicksMs { get; set; } = 1000;
    }
}

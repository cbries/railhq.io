// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace libShared.Entities
{
    public interface IFuncDescItem
    {
        [JsonProperty("functionIndex")]  int FunctionIndex { get; set; }
        [JsonProperty("functionType")] int FunctionType { get; set; }
        [JsonProperty("moment")] bool Moment { get; set; }
        [JsonProperty("state")]  bool State { get; set; }

        [JsonProperty("fncIdx")] int FncIdx { get; set; }
        [JsonProperty("name")] string Name { get; set; }
        [JsonProperty("description")] string Description { get; set; }
        [JsonProperty("isUsed")] bool IsUsed { get; set; }
        [JsonProperty("icon")] string Icon { get; set; }
    }

    public enum LocomotiveDirection
    {
        [JsonProperty("forward")] Forward = 0,
        [JsonProperty("backward")] Backward = 1
    }

    public interface ILocomotive : IEntity
    {
        [JsonProperty("protocol")] string Protocol { get; }
        [JsonProperty("address")]  string Address { get; }
        [JsonProperty("direction")]  LocomotiveDirection Direction { get; }
        [JsonProperty("maxSpeed")]  int MaxSpeed { get; }
        [JsonProperty("speedstep")]  int Speedstep { get; }
        [JsonProperty("functions")]  List<IFuncDescItem> Functions { get; }
    }

    public static class LocomotiveUtilities
    {
        public static int GetStepsForSpeedsteps(string protocol)
        {
            if (string.IsNullOrEmpty(protocol)) return 128;
            if (protocol.Equals("MM14", StringComparison.OrdinalIgnoreCase)) return 1;
            if (protocol.Equals("MM27", StringComparison.OrdinalIgnoreCase)) return 2;
            if (protocol.Equals("MM128", StringComparison.OrdinalIgnoreCase)) return 5;
            if (protocol.Equals("DCC14", StringComparison.OrdinalIgnoreCase)) return 1;
            if (protocol.Equals("DCC28", StringComparison.OrdinalIgnoreCase)) return 2;
            if (protocol.Equals("DCC128", StringComparison.OrdinalIgnoreCase)) return 5;
            if (protocol.Equals("MFX", StringComparison.OrdinalIgnoreCase)) return 5;
            if (protocol.Equals("MMFKT", StringComparison.OrdinalIgnoreCase)) return 1;
            return 1;
        }

        public static int GetNumberOfSpeedsteps(string protocol)
        {
            if (string.IsNullOrEmpty(protocol)) return 128;
            if (protocol.Equals("MM14", StringComparison.OrdinalIgnoreCase)) return 14;
            if (protocol.Equals("MM27", StringComparison.OrdinalIgnoreCase)) return 27;
            if (protocol.Equals("MM128", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("DCC14", StringComparison.OrdinalIgnoreCase)) return 14;
            if (protocol.Equals("DCC28", StringComparison.OrdinalIgnoreCase)) return 28;
            if (protocol.Equals("DCC128", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("MFX", StringComparison.OrdinalIgnoreCase)) return 128;
            if (protocol.Equals("MMFKT", StringComparison.OrdinalIgnoreCase)) return 14;
            return 128;
        }
    }
}

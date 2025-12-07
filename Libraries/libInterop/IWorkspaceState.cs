// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using Newtonsoft.Json;

namespace libInterop
{
    public interface IWorkspaceState
    {
        [JsonProperty("automaticEnabled")] bool AutomaticEnabled { get; }
        [JsonProperty("simulationEnabled")] bool SimulationEnabled { get; }
        [JsonProperty("runningRoutes")] int RunningRoutes { get; }
        [JsonProperty("started")] DateTime Started { get; }
        [JsonProperty("stopped")] DateTime Stopped { get; }
    }
}

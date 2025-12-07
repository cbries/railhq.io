// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using libUserspace.Info.PODs;
using Newtonsoft.Json;

namespace libUserspace.Info;

public class WorkspaceInfo
{
    [JsonProperty("wsName")] public string Name { get; set; } = string.Empty;
    [JsonProperty("items")] public Planfield Items { get; set; } = new();
    [JsonProperty("settings")] public PODs.Settings Settings { get; set; } = new();
    [JsonProperty("routes")] public List<PODs.RouteStats> Routes { get; set; } = new();
}
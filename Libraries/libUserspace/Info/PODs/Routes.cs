// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libUserspace.Info.PODs
{
    public class RouteStats
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("noTracks")] public int NoTracks { get; set; }
        [JsonProperty("noSwitches")] public int NoSwitches { get; set; }
        [JsonProperty("noSensors")] public int NoSensors { get; set; }
        [JsonProperty("noSignals")] public int NoSignals { get; set; }
        [JsonProperty("noBlocks")] public int NoBlocks { get; set; }
    }

    public class Routes
    {
        [JsonProperty("routeStatistics")]
        public List<RouteStats> RouteStatistics { get; set; } = new();
        
        public static Routes Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var instance = new Routes();
            if (instance.Load(json)) return instance;
            return null;
        }

        private bool Load(string json)
        {
            try
            {
                var arr = JArray.Parse(json);

                foreach (var it in arr)
                {
                    var routeStats = new RouteStats();

                    routeStats.Name = it.GetString("name");
                    var tracks = it["tracks"] as JArray;
                    routeStats.NoTracks = tracks?.Count ?? -1;

                    var switches = it["switches"] as JArray;
                    routeStats.NoSwitches = switches?.Count ?? 1;

                    var sensors = it["sensors"] as JArray;
                    routeStats.NoSensors = sensors?.Count ?? -1;

                    var signals = it["signals"] as JArray;
                    routeStats.NoSignals = signals?.Count ?? -1;

                    var blocks = it["blocks"] as JArray;
                    routeStats.NoBlocks = blocks?.Count ?? -1;

                    RouteStatistics.Add(routeStats);
                }

                return true;
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}

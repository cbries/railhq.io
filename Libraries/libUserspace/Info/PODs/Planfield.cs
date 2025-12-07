// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using libTrackplan.Plan;
using libUtilities;
using Newtonsoft.Json;

namespace libUserspace.Info.PODs
{
    public class Planfield
    {
        [JsonProperty("itemTypes")] public Dictionary<int, int> ItemTypes { get; set; } = new();

        [JsonProperty("noSwitches")] public int NoSwitches { get; set; } = 0;
        [JsonProperty("noSignals")] public int NoSignals { get; set; } = 0;
        [JsonProperty("noBlocks")] public int NoBlocks { get; set; } = 0;
        [JsonProperty("noSensors")] public int NoSensors { get; set; } = 0;

        public static Planfield Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var instance = new Planfield();
            if (instance.Load(json))
                return instance;
            return null;
        }

        public Dictionary<int, int> GetItemTypeCounts(List<int> items)
        {
            return items.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count());
        }

        private bool Load(string json)
        {
            try
            {
                var noSwitches = 0;
                var noSignals = 0;
                var noBlocks = 0;
                var noSensors = 0;

                var items = new List<int>();

                if (string.IsNullOrEmpty(json)) return false;
                var obj = JObject.Parse(json);
                foreach (var it in obj)
                {
                    var itemObject = it.Value as JObject;
                    var editor = itemObject?["editor"] as JObject;
                    if (editor == null) continue;
                    var themeId = editor.GetInt("themeId", -1);
                    if (themeId == -1) continue;

                    var type = PlanGlobals.GetThemeType(themeId);
                    switch (type)
                    {
                        case PlanGlobals.ThemeIdType.Block:
                            noBlocks++;
                            break;

                        case PlanGlobals.ThemeIdType.Signal:
                            noSignals++;
                            break;

                        case PlanGlobals.ThemeIdType.Switch:
                            noSwitches++;
                            break;

                        case PlanGlobals.ThemeIdType.Sensor:
                            noSensors++;
                            break;
                    }

                    items.Add(themeId);
                }

                NoSwitches = noSwitches;
                NoSignals = noSignals;
                NoBlocks = noBlocks;
                NoSensors = noSensors;

                ItemTypes = GetItemTypeCounts(items);

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

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libUserspace.Info.PODs
{
    public class Settings
    {
        [JsonProperty("noBlocks")] public int NoBlocks { get; set; }
        [JsonProperty("noSensors")] public int NoSensors { get; set; }

        public static Settings Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            var instance = new Settings();
            if (instance.Load(json)) return instance;
            return null;
        }
        
        private bool Load(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                var obj = JObject.Parse(json);
                if (obj["locomotives"] != null)
                {
                    // tbd
                }

                if (obj["routes"] != null)
                {
                    // tbd
                }

                if (obj["blockSensors"] != null)
                {
                    var arrBlocks = obj["blockSensors"] as JArray;
                    NoBlocks = arrBlocks?.Count ?? -1;
                }

                if (obj["sensors"] != null)
                {
                    var arrSensors = obj["sensors"] as JArray;
                    NoSensors = arrSensors?.Count ?? -1;
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

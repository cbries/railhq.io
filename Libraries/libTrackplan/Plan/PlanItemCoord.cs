// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItemCoord.cs

using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

// ReSharper disable InconsistentNaming

namespace libTrackplan.Plan
{
    public class PlanItemCoord
    {
        [JsonProperty("x")] public int X { get; set; }
        [JsonProperty("y")] public int Y { get; set; }

        public bool Parse(JToken tkn)
        {
            var o = tkn as JObject;
            if (o == null) return false;

            X = o.GetInt("x", -1);
            Y = o.GetInt("y", -1);

            if (X == -1 || Y == -1)
                return false;

            return true;
        }

        public static PlanItemCoord GetInstance(JToken tkn)
        {
            var instance = new PlanItemCoord();
            if (instance.Parse(tkn))
                return instance;
            return null;
        }
    }
}

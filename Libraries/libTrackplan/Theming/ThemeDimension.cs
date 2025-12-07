// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeDimension.cs

using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class ThemeDimension
    {
        [JsonProperty("w")] public int W { get; set; } = 1;
        [JsonProperty("h")] public int H { get; set; } = 1;
    }
}

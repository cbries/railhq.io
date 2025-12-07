// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeCategory.cs

using System.Collections.Generic;
using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class ThemeCategory
    {
        [JsonProperty("category")] public string Category { get; set; } = string.Empty;
        [JsonProperty("objects")] public List<ThemeItem> Objects { get; set; } = new();
    }
}

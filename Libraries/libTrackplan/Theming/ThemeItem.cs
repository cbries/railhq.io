// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeItem.cs

using System.Collections.Generic;
using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class ThemeItem
    {
        [JsonProperty("id")] public int Id { get; set; } = -1;
        [JsonProperty("name")] public string Name { get; set; } = string.Empty;
        [JsonProperty("basename")] public string BaseName { get; set; } = string.Empty;
        [JsonProperty("clickable")] public bool Clickable { get; set; } = false;
        [JsonProperty("routes")] public List<string> Routes { get; set; } = new();
        [JsonProperty("dimensions")] public List<ThemeDimension> Dimensions { get; set; } = new();
        [JsonProperty("states")] public Dictionary<string, List<ThemeSwitchState>> States { get; set; } = new();
    }
}

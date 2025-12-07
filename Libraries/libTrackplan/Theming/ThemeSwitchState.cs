// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: ThemeSwitchState.cs

using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class ThemeSwitchState
    {
        [JsonProperty("source")] public string Source { get; set; }
        [JsonProperty("to")] public string To { get; set; }
        [JsonProperty("state")] public string State { get; set; }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class Theme
    {
        [JsonProperty("themeName")] public string ThemeName { get; set; }
        [JsonProperty("themeItems")] public List<ThemeCategory> ThemeItems { get; set; }
    }
}

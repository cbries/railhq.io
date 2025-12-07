// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;

namespace libMetamodel.Settings
{
    public interface IStateBase
    {
        [JsonProperty("isEnabled")] bool IsEnabled { get; set; }
        [JsonProperty("isLocked")] bool IsLocked { get; set; }
    }
}

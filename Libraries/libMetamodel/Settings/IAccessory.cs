// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace libMetamodel.Settings
{
    public interface IAccessory
    {
        [JsonProperty("planfieldControlIdentifier")] string PlanfieldControlIdentifier { get; set; }

        [JsonProperty("accessoryDriver")] string AccessoryDriver { get; set; }
        [JsonProperty("accessoryIdentifier")] string AccessoryIdentifier { get; set; }
        
        // Changes the address index, 0 will be address 1, and 1 will be address 0.
        // Currently this invert is only tested and implemented for two state accessories.
        // TODO check for three-/four-state accessories, e.g. Kreuzungen
        [JsonProperty("invert")] bool Invert { get; set; }
        [JsonProperty("invertUi")] bool InvertUi { get; set; }

        [JsonProperty("isMaintenaceEnabled")] bool IsMaintenaceEnabled { get; set; }
    }
}

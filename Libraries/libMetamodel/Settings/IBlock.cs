// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace libMetamodel.Settings
{
    public interface IStaging : IStateBase
    {
        [JsonProperty("identifier")] string Identifier { get; set; }
        [JsonProperty("blocks")] List<Block> Blocks { get; set; }
    }

    public interface IBlock : IStateBase
    {
        [JsonProperty("identifier")] string Identifier { get; set; }
        [JsonProperty("length")] int Length { get; set; }
        [JsonProperty("startDelay")] int StartDelay { get; set; }
        [JsonProperty("signalsToRedDelay")] int SignalsToRedDelay { get; set; }
        [JsonProperty("sensorEnter")] string SensorEnter { get; set; }
        [JsonProperty("sensorOcc")] string SensorOcc { get; set; }
        [JsonProperty("sensorIn")] string SensorIn { get; set; }
    }

    public interface IBlockExtras
    {
        [JsonProperty("isCommuterAllowedPlus")] bool IsCommuterAllowedPlus { get; set; }
        [JsonProperty("isCommuterAllowedMinus")] bool IsCommuterAllowedMinus { get; set; }
    }

    public interface IBlockSignals
    {
        [JsonProperty("signal")]  string Signal { get; set; }
        [JsonProperty("vorsignal")]  string Vorsignal { get; set; }
    }
}

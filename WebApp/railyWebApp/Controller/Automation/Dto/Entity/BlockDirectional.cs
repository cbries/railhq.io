// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Dto.Entity;

public class BlockDirectional
{
    [JsonProperty("isEnabled")] public bool IsEnabled { get; set; } = true;
    [JsonProperty("isLocked")] public bool IsLocked { get; set; } = false;
    [JsonProperty("length")] public int Length { get; set; } = 0;

    [JsonProperty("feedbackEnter")] public BlockSensor FeedbackEnter { get; set; }
    [JsonProperty("feedbackOcc")]  public BlockSensor FeedbackOcc { get; set; }
    [JsonProperty("feedbackIn")] public BlockSensor FeedbackIn { get; set; }

    [JsonProperty("assignedLocomotive")]  public BlockLocomotive AssignedLocomotive { get; set; }

    //public int StartDelay { get; set; }
    //public int SignalsToRedDelay { get; set; }
    //public bool IsCommuterAllowed { get; set; }
    //public string Signal { get; set; }
    //public string Vorsignal { get; set; }
}
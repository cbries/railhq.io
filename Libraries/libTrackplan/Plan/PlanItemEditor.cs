// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItemEditor.cs

// ReSharper disable InconsistentNaming

using System.ComponentModel;
using Newtonsoft.Json;

namespace libTrackplan.Plan
{
    public class PlanItemEditor
    {
        [JsonProperty("themeId")] public int ThemeId { get; set; }

        private int _themeDimIdx;

        [JsonProperty("themeDimIdx")] public int ThemeDimIdx
        {
            get => _themeDimIdx;
            set
            {
                if (value < 0)
                {
                    if (value == -1) _themeDimIdx = 3;
                    else if (value == -2) _themeDimIdx = 2;
                    else if (value == -3) _themeDimIdx = 1;
                    else _themeDimIdx = 0;
                }
                else
                {
                    _themeDimIdx = value;
                }
            }
        }

        [JsonProperty("connectorId")] public int ConnectorId { get; set; } = 1;

        [JsonProperty(
            PropertyName = "innerHtml",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("")]
        public string InnerHtml { get; set; } = string.Empty;

        [JsonProperty(
            PropertyName = "fontSize",
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore)]
        [DefaultValue("14px")]
        public string FontSize { get; set; } = "14px";
    }
}

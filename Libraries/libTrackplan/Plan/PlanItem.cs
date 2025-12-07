// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: PlanItem.cs

using System;
using System.Collections.Generic;
using System.Linq;
using libTrackplan.Theming;
using Newtonsoft.Json;

// ReSharper disable InconsistentNaming

namespace libTrackplan.Plan
{
    public enum PlanItemT
    {
        Unknown,
        Block = 1, Bk = 1,
        Switch = 2, Sw = 2, 
        Sensor = 3, Fb = 3, 
        Track = 4, Tk = 4, 
        Signal = 5, Sg = 5, 
        Tx, 
        St, 
        Lc, 
        Vr, 
        Co,
        StagingBlock = 11, Sb = 11
    }

    public partial class PlanItem
    {
        [JsonIgnore] public Planfield Ctx { get; set; }

        [JsonIgnore] internal ThemeData ThemeData => Ctx?.ThemeData;

        #region metamodel
        
        [JsonProperty("identifier")] public string Identifier { get; set; }
        [JsonProperty("coord")] public PlanItemCoord Coord { get; set; } = new();
        [JsonProperty("editor")] public PlanItemEditor Editor { get; set; } = new();

        #endregion /metamodel

        private ThemeItem GetThemeItem()
        {
            if (ThemeData == null) return null;
            if (Editor == null) return null;
            var themeId = Editor.ThemeId;
            return ThemeData.GetThemeItemBy(themeId);
        }

        [JsonIgnore] public List<string> Routes => GetThemeItem()?.Routes ?? [];
        [JsonIgnore] public Dictionary<string, List<ThemeSwitchState>> States => GetThemeItem()?.States ?? [];
        [JsonIgnore] public List<ThemeDimension> Dimensions => GetThemeItem()?.Dimensions ?? [];

        /// <summary>
        /// Returns the current `themeDimIdx` values.
        /// In case the value does not fit into the range
        /// of routes, a modul division is executed to
        /// get the minimum allowed value for the
        /// rotation. Keep in mind a maximum range
        /// of `0..3` is allowed, because a track element
        /// can only have 0°, 90°, 180°, and 270° degree
        /// of rotation
        /// </summary>
        /// <returns></returns>
        public int GetThemeDimensionIndex()
        {
            var themeDimY = 0;
            if (Editor != null)
                themeDimY = Editor.ThemeDimIdx;

            if (Routes != null && Routes.Count > 0)
            {
                if (themeDimY >= Routes.Count)
                    themeDimY = Routes.Count % themeDimY;

                if (themeDimY >= Routes.Count)
                {
                    if (themeDimY % 2 == 0) return 1;
                    if (themeDimY % 3 == 0) return 2;
                    if (themeDimY % 4 == 0) return 3;

                    return 0;
                }
            }

            return themeDimY;
        }

        /// <summary>
        /// Queries the currently selected possible routes.
        /// It checks `themeDimIdx` if its fits into the range
        /// of `routes` and will return the list of allowed
        /// ways on the current rotation representation.
        /// </summary>
        /// <returns></returns>
        public List<string> GetDimensionRoutes()
        {
            if (!PlanGlobals.IsTrackItem(Editor.ThemeId))
                return new List<string>();

            string str;
            if (Editor == null)
            {
                str = Routes[0];
            }
            else
            {
                var dimIdx = GetThemeDimensionIndex();
                str = Routes[dimIdx];
            }
            if (string.IsNullOrEmpty(str))
                return new List<string>();
            var parts = str.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.ToList();
        }

        [JsonIgnore]
        public int Circumference => 2 * Width() + 2 * Height();

        public int Width()
        {
            var s = StartCoord();
            var e = EndCoord();
            var w = e.X - s.X;
            return w + 1;
        }

        public int Height()
        {
            var s = StartCoord();
            var e = EndCoord();
            var h = e.Y - s.Y;
            return h + 1;
        }

        public PlanItemCoord StartCoord()
        {
            return Coord;
        }

        public PlanItemCoord EndCoord()
        {
            if (Dimensions != null && Dimensions.Count > 0)
            {
                var dim = Dimensions[GetThemeDimensionIndex()];
                var endCoord = new PlanItemCoord
                {
                    X = Coord.X + dim.W - 1,
                    Y = Coord.Y + dim.H - 1
                };

                return endCoord;
            }

            return new PlanItemCoord
            {
                X = Coord.X,
                Y = Coord.Y
            };
        }

        [JsonIgnore] public bool IsTrack => PlanGlobals.TrackIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsSwitch => PlanGlobals.SwitchIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsSignal => PlanGlobals.SignalIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsBlock => PlanGlobals.BlockIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsStage => PlanGlobals.StageIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsSensor => PlanGlobals.SensorIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsDirection => PlanGlobals.DirectionIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsConnector => PlanGlobals.ConnectorIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsButton => PlanGlobals.ButtonIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsDecoupler => PlanGlobals.DecouplerIds.Contains(Editor.ThemeId);
        [JsonIgnore] public bool IsLabel => PlanGlobals.LabelIds.Contains(Editor.ThemeId);

        public override string ToString()
        {
            try
            {
                var s = StartCoord();
                var e = EndCoord();
                return $"{Identifier}  Start({s.X}, {s.Y}) -> Stop({e.X}, {e.Y})";
            }
            catch
            {
                return base.ToString();
            }
        }
    }
}

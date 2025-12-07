// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: AnalyzeResult.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using libTrackplan.Plan;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libTrackplan.Analyzer
{
    public enum SideMarker
    {
        Plus, Minus, None
    }

    public static class SideMarkerExtensions
    {
        public static SideMarker GetOpposite(this SideMarker type)
        {
            if (type == SideMarker.Plus) return SideMarker.Minus;
            if (type == SideMarker.Minus) return SideMarker.Plus;
            return SideMarker.None;
        }

        public static bool IsPlus(this SideMarker type)
        {
            return type == SideMarker.Plus;
        }

        public static bool IsMinus(this SideMarker type)
        {
            return type == SideMarker.Minus;
        }

        /// <summary>
        /// Returns the corresponding block marker symbol for the given SideMarker type.
        /// </summary>
        /// <param name="type">The SideMarker type for which to retrieve the block marker symbol.</param>
        /// <returns>
        /// A string representing the block marker symbol:
        /// - "[+]" for <see cref="SideMarker.Plus"/>
        /// - "[-]" for <see cref="SideMarker.Minus"/>
        /// - An empty string for <see cref="SideMarker.None"/>
        /// - "Unknown Type" for undefined values.
        /// </returns>
        public static string GetBlockMarker(this SideMarker type)
        {
            return type switch
            {
                SideMarker.Plus => "[+]",
                SideMarker.Minus => "[-]",
                SideMarker.None => string.Empty,
                _ => "Unknown Type"
            };
        }
    }

    public class AnalyzeResult
    {
        public int NumberOfRoutes => Routes.Count;
        public List<Plan.Route> Routes { get; set; } = new();

        private static void QueryInAndOut(Plan.Route route, out SideMarker outSide, out SideMarker inSide)
        {
            outSide = SideMarker.None;
            inSide = SideMarker.None;

            var startBlock = route.Items[0];
            var startBlockNext = route.Items[1];
            var xStart = startBlockNext.Item.Coord.X - startBlock.Item.Coord.X;
            var yStart = startBlockNext.Item.Coord.Y - startBlock.Item.Coord.Y;
            if (yStart == 0)
            {
                if (xStart == -1) 
                    outSide = SideMarker.Plus;
                else if (xStart > 1) 
                    outSide = SideMarker.Minus;
            }
            else if (xStart == 0)
            {
                if (yStart == -1) 
                    outSide = SideMarker.Plus;
                else if (yStart > 1) 
                    outSide = SideMarker.Minus;
            }

            var routeLength = route.NumberOfItems;
            var endBlock = route.Items[routeLength - 1];
            var endBlockPrevious = route.Items[routeLength - 2];
            var xEnd = endBlockPrevious.Item.Coord.X - endBlock.Item.Coord.X;
            var yEnd = endBlockPrevious.Item.Coord.Y - endBlock.Item.Coord.Y;
            if (yEnd == 0)
            {
                if (xEnd == -1) 
                    inSide = SideMarker.Plus;
                else if (xEnd > 1) 
                    inSide = SideMarker.Minus;
            }
            else if (xEnd == 0)
            {
                if (yEnd == -1) 
                    inSide = SideMarker.Plus;
                else if (yEnd > 1) 
                    inSide = SideMarker.Minus;
            }
        }

        public static JArray GetAllCoordinates(int x, int y, int width, int height)
        {
            var coords = new JArray();

            for (int dx = 0; dx < width; dx++)
            {
                for (int dy = 0; dy < height; dy++)
                {
                    var point = new JObject
                    {
                        ["x"] = x + dx,
                        ["y"] = y + dy
                    };

                    coords.Add(point);
                }
            }

            return coords;
        }

        public string ToJson()
        {
            var arRoutes = new JArray();

            foreach(var itRoute in Routes)
            {
                var itemTracks = new JArray();
                var itemSwitches = new JArray();
                var itemSensors = new JArray();
                var itemSignals = new JArray();
                var itemBlocks = new JArray();

                QueryInAndOut(itRoute, out var outSide, out var inSide);

                var startBlock = itRoute.Items.First();
                var endBlock = itRoute.Items.Last();

                foreach (var itItem in itRoute.Items)
                {
                    var item = itItem.Item;

                    var o = new JObject
                    {
                        ["x"] = item.Coord.X,
                        ["y"] = item.Coord.Y
                    };

                    var x = item.Coord.X;
                    var y = item.Coord.Y;
                    var w = item.Width();
                    var h = item.Height();
                    var pixel = GetAllCoordinates(x, y, w, h);
                    if (pixel != null && pixel.Count > 1)
                        o["pixels"] = pixel;

                    if (item.IsSwitch)
                    {
                        var oo = new JObject
                        {
                            ["from"] = new JObject
                            {
                                ["x"] = itItem.PreviousItemPath.From.Coord.X,
                                ["y"] = itItem.PreviousItemPath.From.Coord.Y
                            },
                            ["to"] = new JObject
                            {
                                ["x"] = itItem.PreviousItemPath.To.Coord.X,
                                ["y"] = itItem.PreviousItemPath.To.Coord.Y
                            },
                            ["state"] = itItem.GetThemeSwitchPrefix()
                        };

                        o["switch"] = oo;

                        itemSwitches.Add(o);

                    } else if(item.IsSensor)
                    {
                        itemSensors.Add(o);
                    }
                    else if(item.IsSignal)
                    {
                        itemSignals.Add(o);
                    }
                    else if(item.IsBlock || item.IsStage)
                    {
                        if (item.Identifier.Equals(startBlock.Item.Identifier))
                        {
                            o["start"] = true;
                            o["side"] = outSide.ToString();
                        }
                        else if(item.Identifier.Equals(endBlock.Item.Identifier))
                        {
                            o["start"] = false;
                            o["side"] = inSide.ToString();
                        }

                        o["identifier"] = item.Identifier;

                        itemBlocks.Add(o);
                    }
                    else
                    {
                        itemTracks.Add(o);
                    }
                }

                var or = new JObject
                {
                    ["name"] = $"{itRoute.DisplayName}",
                    ["uid"] = Guid.NewGuid().ToString("D"),
                    ["tracks"] = itemTracks,
                    ["switches"] = itemSwitches,
                    ["sensors"] = itemSensors,
                    ["signals"] = itemSignals,
                    ["blocks"] = itemBlocks
                };

                arRoutes.Add(or);
            }

            return arRoutes.ToString(Formatting.Indented);
        }
    }
}

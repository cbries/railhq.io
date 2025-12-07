// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Route.cs

using System;
using System.Collections.Generic;
using libTrackplan.Analyzer;
using Newtonsoft.Json;
using System.Diagnostics;

// ReSharper disable InconsistentNaming

namespace libTrackplan.Route
{
    public class Route
    {
        [JsonProperty("name")] public string Name { get; set; }
        [JsonProperty("tracks")] public List<RouteTrack> Tracks { get; set; } = new();
        [JsonProperty("switches")] public List<RouteSwitch> Switches { get; set; } = new();
        [JsonProperty("sensors")] public List<RouteSensors> Sensors { get; set; } = new();
        [JsonProperty("signals")] public List<RouteSignal> Signals { get; set; } = new();
        [JsonProperty("blocks")] public List<RouteBlock> Blocks { get; set; }
    }

    public class Pixel : IRouteCoord
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public interface IRouteCoord
    {
        int x { get; set; }
        int y { get; set; }
    }

    public class RouteTrack : IRouteCoord
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class RouteSwitch : IRouteCoord
    {
        public int x { get; set; }
        public int y { get; set; }
        public RouteSwitchSwitch Switch { get; set; } = new();
        public List<Pixel> Pixels { get; set; } = new();
    }

    public class RouteSwitchSwitch
    {
        public string State { get; set; }

        public int [] GetStateIndex()
        {
            if (State.Equals("straight", StringComparison.OrdinalIgnoreCase)) return [0];
            if (State.Equals("turn", StringComparison.OrdinalIgnoreCase)) return [1];
            if (State.Equals("turnright", StringComparison.OrdinalIgnoreCase)) return [1];
            if (State.Equals("turnleft", StringComparison.OrdinalIgnoreCase)) return [1];

            var parts = State.Split("|", StringSplitOptions.TrimEntries);

            // TODO das muss überarbeitet werden, wenn Zeit ist
            // Ich bin nicht glücklic mit dieser Lösung!
            // Ich glaube immernoch, dass man dies mit einer Bitmaske lösen kann.
            if (parts[0] == "straight" && parts[1] == "straight") return [0,0];
            if (parts[0] == "turnright" && parts[1] == "turnright") return [0, 1];
            if (parts[0] == "turnright" && parts[1] == "straight") return [1, 0];
            if (parts[0] == "straight" && parts[1] == "turnright") return [1, 1];

            return [-1];
        }
    }

    public class RouteSignal : IRouteCoord
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class RouteBlock : IRouteCoord
    {
        [JsonIgnore]
        public string Caption
        {
            get
            {
                if (side == SideMarker.None) return identifier;
                if (side == SideMarker.Plus) return $"{identifier}[+]";
                if (side == SideMarker.Minus) return $"{identifier}[-]";
                return identifier;
            }
        }

        public int x { get; set; }
        public int y { get; set; }
        public string identifier { get; set; }
        public bool start { get; set; }
        public SideMarker side { get; set; }
    }

    public class RouteSensors : IRouteCoord
    {
        public int x { get; set; }
        public int y { get; set; }
    }
}

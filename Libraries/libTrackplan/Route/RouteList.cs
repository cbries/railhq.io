// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteList.cs

using System;
using System.Collections.Generic;
using System.Linq;
using libTrackplan.Analyzer;

namespace libTrackplan.Route
{
    public class RouteList : List<Route>
    {
        /// <summary>
        /// Flag to inform that the routes are based on commuting,
        /// i.e. leaving side is the same as entering one specific block.
        /// </summary>
        public bool CommutingRoutes { get; set; } = false;

        private RouteList GetRoutesWith(int x, int y)
        {
            var res = new RouteList();

            foreach (var it in this)
            {
                var itms = new List<IRouteCoord>();
                itms.AddRange(it.Tracks);
                itms.AddRange(it.Sensors);
                itms.AddRange(it.Signals);
                itms.AddRange(it.Switches);
                itms.AddRange(it.Blocks);

                foreach (var itt in itms)
                {
                    var found = false;

                    if (itt is RouteSwitch ittSwitch)
                    {
                        if (ittSwitch.Pixels.Count > 1)
                        {
                            if (ittSwitch.Pixels.Any(p => p.x == x && p.y == y))
                            {
                                res.Add(it);

                                found = true;
                            }
                        }
                    }

                    if(!found)
                    {
                        if (itt.x == x && itt.y == y)
                        {
                            res.Add(it);

                            found = true;
                        }
                    }

                    if(found)
                        break;
                }
            }

            return res;
        }

        public RouteList GetCrossingRoutesOf(Route route)
        {
            var res = new RouteList();

            var itms = new List<IRouteCoord>();
            itms.AddRange(route.Tracks);
            itms.AddRange(route.Sensors);
            itms.AddRange(route.Signals);
            itms.AddRange(route.Switches);
            itms.AddRange(route.Blocks);

            foreach (var itTrack in itms)
            {
                if (itTrack == null) continue;
                
                var x = itTrack.x;
                var y = itTrack.y;

                var r = GetRoutesWith(x, y);

                foreach (var itR in r)
                {
                    var isAlreadyAdded = false;

                    foreach(var itt in res)
                    {
                        isAlreadyAdded = itt.Name.Equals(itR.Name, StringComparison.OrdinalIgnoreCase);
                        if (isAlreadyAdded) break;
                    }

                    if (!isAlreadyAdded)
                        res.Add(itR);
                }
            }

            return res;
        }

        public RouteList GetRoutesWithFromBlock(string fromBlock, SideMarker sideToLeave)
        {
            var res = new RouteList();

            foreach (var it in this)
            {
                var from = it?.Blocks?[0];
                if (from == null) continue;
                var id = from.identifier;
                if (string.IsNullOrEmpty(id)) continue;
                if (!id.Equals(fromBlock, StringComparison.Ordinal)) continue;
                if (from.side != sideToLeave) continue;

                res.Add(it);
            }

            return res;
        }

        public Route GetByName(string routeName)
        {
            if (string.IsNullOrEmpty(routeName)) return null;

            foreach (var it in this)
            {
                if (string.IsNullOrEmpty(it?.Name)) continue;
                if (it.Name.Equals(routeName, StringComparison.OrdinalIgnoreCase))
                    return it;
            }
            
            return null;
        }

        public List<Route> GetAllByNames(IReadOnlyList<string> names)
        {
            var res = new List<Route>();

            foreach (var it in this)
            {
                foreach (var itt in names)
                {
                    if (it.Name.StartsWith(itt) || it.Name.EndsWith(itt))
                    {
                        if (!res.Contains(it))
                            res.Add(it);
                    }
                }
            }

            return res;
        }
    }
}
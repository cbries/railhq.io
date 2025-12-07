// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Planfield.cs

// ReSharper disable ForCanBeConvertedToForeach

using System;
using libUtilities;
using Newtonsoft.Json;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libTrackplan.Theming;

namespace libTrackplan.Plan
{
    public class Planfield : ConcurrentDictionary<string, PlanItem>
    {
        internal ThemeData ThemeData { get; private set; }

        public FileInfo OriginalFile { get; set; }
        
        public async Task Save()
        {
            if (OriginalFile == null) return;

            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await File.WriteAllTextAsync(OriginalFile.FullName, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        public void InitContext(FileInfo originalFile, ThemeData themeData)
        {
            if(OriginalFile == null) OriginalFile = originalFile;

            ThemeData = themeData;

            foreach (var it in this)
            {
                var v = it.Value;
                v.Ctx = this;
            }
        }

        public string GetUnusedIdentifierBy(uint themeId)
        {
            var listOfItems = this.Select(it => it.Value).Where(planItem => planItem != null).ToList();
            var basename = PlanGlobals.GetThemeTypeName((int)themeId);
            return GenerateUniqueIdentifier(listOfItems, basename);
        }
        
        public static string GenerateUniqueIdentifier(List<PlanItem> planItems, string suffix)
        {
            string prefix = $"{suffix}_";

            var usedNumbers = planItems
                .Where(item => item.Identifier.StartsWith(prefix))
                .Select(item =>
                {
                    var parts = item.Identifier.Substring(prefix.Length);
                    return int.TryParse(parts, out int num) ? num : (int?)null;
                })
                .Where(num => num.HasValue)
                .Select(num => num.Value)
                .ToList();

            int newNumber = 1;
            while (usedNumbers.Contains(newNumber))
            {
                newNumber++;
            }

            return $"{prefix}{newNumber}";
        }

        public int GetMaxWidth()
        {
            var w = 0;

            foreach (var it in this)
            {
                var item = it.Value;
                if (item == null) continue;

                if (item.Coord.X > w)
                    w = item.Coord.X;

                var themeDimX = 0;
                if (item.Editor != null)
                    themeDimX = item.Editor.ThemeDimIdx;

                if (item.Dimensions != null && item.Dimensions.Count > themeDimX)
                {
                    var dim = item.Dimensions[themeDimX];
                    if (dim.W > 1)
                    {
                        var xw = item.Coord.X + dim.W;
                        if (xw > w)
                            w = xw;
                    }
                }
            }

            return w;

        }

        public int GetMaxHeight()
        {
            var h = 0;

            foreach (var it in this)
            {
                var item = it.Value;
                if (item == null) continue;

                if (item.Coord.Y > h)
                    h = item.Coord.Y;

                var themeDimY = 0;
                if (item.Editor != null)
                    themeDimY = item.Editor.ThemeDimIdx;

                if (item.Dimensions != null && item.Dimensions.Count > themeDimY)
                {
                    var dim = item.Dimensions[themeDimY];
                    if (dim.H > 1)
                    {
                        var yh = item.Coord.Y + dim.H;
                        if (yh > h)
                            h = yh;
                    }
                }
            }

            return h;
        }

        public PlanItem Get(string identifier)
        {
            if (string.IsNullOrEmpty(identifier)) return null;
            foreach (var it in this)
            {
                var item = it.Value;
                if (string.IsNullOrEmpty(item?.Identifier)) continue;

                if (item.Identifier.Equals(identifier, StringComparison.OrdinalIgnoreCase))
                    return item;
            }

            return null;
        }

        public PlanItem Get(int x, int y)
        {
            foreach (var it in this)
            {
                var item = it.Value;
                if (item == null) continue;

                var startCoord = item.StartCoord();
                var endCoord = item.EndCoord();

                if (x >= startCoord.X
                    && x <= endCoord.X
                    && y >= startCoord.Y
                    && y <= endCoord.Y)
                {
                    return item;
                }
            }

            return null;
        }

        public PlanItem[] GetConnectors(int connectorId)
        {
            if (connectorId == -1) return null;
            var items = new PlanItem[2];
            var index = 0;

            foreach (var it in this)
            {
                var item = it.Value;
                if (!item.IsConnector) continue;
                if (item.Editor.ConnectorId <= 1) continue;
                if (item.Editor.ConnectorId == connectorId)
                {
                    items[index] = item;
                    index++;

                    if (index == 2)
                        return items;
                }
            }

            return items;
        }
        
        public List<PlanItem> GetBlocks()
        {
            var result = new List<PlanItem>();

            foreach (var it in this)
            {
                var v = it.Value;
                if (v == null) continue;
                if (v.IsBlock || v.IsStage)
                    result.Add(v);
            }

            return result;
        }

        public bool Remove(int x, int y)
        {
            var item = Get(x, y);
            if (item == null) return true;
            this.Remove($"{x}x{y}", out var value);
            return value != null;
        }

        private readonly Stack<BranchInfo> _branches = new();

        /// <summary>
        /// This method is NOT thread-safe !!
        /// Searches all posible routes with `startBlock` as starting point.
        /// Only between blocks routes are allowed.
        /// Keep in mind, any call of this method calculates the route
        /// from the beginning and can have a deep impact on application
        /// performance when periodically called.
        /// </summary>
        /// <param name="startBlock"></param>
        /// <returns></returns>
        public RouteList GetRoutes(PlanItem startBlock)
        {
            var res = new RouteList();
            if (!startBlock.IsBlock && !startBlock.IsStage) return res;

            var allowedPath = startBlock.GetAllowedPath();
            if (allowedPath.Count == 0) return res;

            foreach (var it in allowedPath)
            {
                var startStep = new NextStep { Item = startBlock };
                var result = new List<NextStep> { startStep };
                var r = Walk(startBlock, new NextStep { Item = it.To }, ref result);
                if (r)
                {
                    var route = new Route();
                    result.ForEach(itr => route.Items.Add(itr));

                    if (route.Target.IsStage && route.TargetEnteringSide == Route.LeavingEnterType.Minus)
                    {
                        // Wenn das Ziel ein Stage ist und die EnterSide == "[-]", dann
                        // ist die Einfahrt aus Prinzip zu ignorieren, denn Stages dürfen
                        // nur von "[+]" nach "[-]" befahren werden.
                        //
                        // NOTE: do not add route
                        //

                        continue;
                    }

                    res.Add(route);
                }
            }

            // check for branches
            if (_branches.Count > 0)
            {
                BranchInfo branch;

                while ((branch = _branches.Pop()) != null)
                {
                    for (var j = 0; j < branch.NextAllowedSteps.Count; ++j)
                    {
                        var nextStep = branch.NextAllowedSteps[j];

                        // ## IMPORTANT IMPLEMENTATION NOTICE ##
                        // we have to make a copy of the PreviousItemPath
                        // attribute because it is a ref-value and any change
                        // will modify any previously analyzed route and
                        // all states are reseted, i.e. the last change
                        // would be always applied to any route -- that
                        // is absolutly incorrect and not acceptable
                        var copyOfRecentItems = new List<NextStep>();
                        foreach (var itRecent in branch.RecentItems)
                        {
                            if (itRecent == null) continue;
                            var instance = new NextStep
                            {
                                Item = itRecent.Item,
                                PreviousItemPath = new Path
                                {
                                    FromSide = itRecent.PreviousItemPath.FromSide,
                                    ToSide = itRecent.PreviousItemPath.ToSide,
                                    From = itRecent.PreviousItemPath.From,
                                    To = itRecent.PreviousItemPath.To
                                }
                            };
                            copyOfRecentItems.Add(instance);
                        }

                        var previousPath = nextStep.PreviousItemPath;
                        var lastRecentStep = copyOfRecentItems.Last();
                        lastRecentStep.PreviousItemPath = previousPath;
                        nextStep.PreviousItemPath = new Path();
                        var lastRecentItem = lastRecentStep.Item;

                        var resBranch = Walk(lastRecentItem, nextStep, ref copyOfRecentItems);
                        if (resBranch)
                        {
                            var route = new Route();
                            copyOfRecentItems.ForEach(itr => route.Items.Add(itr));
                            res.Add(route);
                        }
                    }

                    branch.NextAllowedSteps.Clear();

                    if (_branches.Count == 0)
                        break;
                }
            }

            // reset routes path information, 
            // only allowed for switches
            foreach (var it in res)
            {
                if (it == null) continue;
                foreach (var itItem in it.Items)
                {
                    if (itItem?.Item == null) continue;
                    if (itItem.Item.IsSwitch) continue;
                    itItem.PreviousItemPath = new Path();
                }
            }

            return res;
        }

        private bool Walk(PlanItem previousItem, NextStep currentStep, ref List<NextStep> result)
        {
            try
            {
                var currentItem = currentStep.Item;

                if (previousItem.Identifier.Equals(currentItem.Identifier))
                    return false;

                result.Add(currentStep);

                if (currentItem.IsBlock || currentItem.IsStage)
                {
                    // final stop, a block reached
                    return true;
                }

                var nextAllowedPath = currentItem.GetAllowedPath(out var connectors);
                if (nextAllowedPath.Count == 0)
                {
                    // final stop, no additional destinations available
                    return false;
                }

                var nextSteps = new List<NextStep>();
                var previousId = previousItem.Identifier;
                foreach (var it in nextAllowedPath)
                {
                    if (previousId.Equals(it.From.Identifier))
                    {
                        var step = new NextStep
                        {
                            Item = it.To,
                            PreviousItemPath = it
                        };

                        nextSteps.Add(step);
                    }
                }

                if (nextSteps.Count > 1)
                {
                    var branchInfo = new BranchInfo
                    {
                        Item = currentStep
                    };
                    result.ForEach(it => branchInfo.RecentItems.Add(it));
                    nextSteps.ForEach(it => branchInfo.NextAllowedSteps.Add(it));
                    _branches.Push(branchInfo);

                    // walk stop, branch has been detected
                    return false;
                }

                if (nextSteps.Count == 0)
                    return false;

                if (currentItem.IsConnector)
                {
                    if (connectors != null && connectors.Length == 2)
                    {
                        var fakeCurrentItem = connectors[1];
                        result.Add(new NextStep
                        {
                            Item = fakeCurrentItem
                        });
                        var r = Walk(fakeCurrentItem, nextSteps.First(), ref result);

                        return r;
                    }
                }
                else
                {
                    var nextStep = nextSteps.First();

                    // in case only one step if there to decide which 
                    // way we can go AND in case the current item is
                    // a switch, remember the way for later use during 
                    // auto-route and auto-switching
                    if (currentItem.IsSwitch)
                    {
                        var nextStepItemPath = nextStep.PreviousItemPath;

                        currentStep.PreviousItemPath = new Path
                        {
                            FromSide = nextStepItemPath.FromSide,
                            ToSide = nextStepItemPath.ToSide,
                            From = nextStep.PreviousItemPath.From,
                            To = nextStep.PreviousItemPath.To
                        };
                    }

                    var r = Walk(currentItem, nextStep, ref result);

                    return r;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }
    }
}

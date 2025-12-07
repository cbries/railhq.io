// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Route.cs

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace libTrackplan.Plan
{
    public class Route
    {
        [JsonIgnore]
        public int NumberOfItems => Items.Count;
        public List<NextStep> Items { get; set; } = new();
        public PlanItem Start
        {
            get
            {
                if (Items == null || NumberOfItems == 0)
                    return null;
                return Items.First()?.Item;
            }
        }
        public PlanItem Target
        {
            get
            {
                if (Items == null || NumberOfItems == 0)
                    return null;
                return Items.Last()?.Item;
            }
        }

        public string DisplayName
        {
            get
            {
                var startIdentifier = Start.Identifier;
                var targetIdentifier = Target.Identifier;
                
                var sourceLeaving = SourceLeavingSide;
                var targetEnter = TargetEnteringSide;

                var startSuffix = __getName(sourceLeaving);
                var targetSuffix = __getName(targetEnter);

                //var xStart = Start.Coord.X;
                //var yStart = Start.Coord.Y;

                //var nextItem = Items[1];
                //var x0 = nextItem.Item.Coord.X;
                //var y0 = nextItem.Item.Coord.Y;

                //var xTarget = Target.Coord.X;
                //var yTarget = Target.Coord.Y;

                //var previousItem = Items[Items.Count - 2];
                //var x1 = previousItem.Item.Coord.X;
                //var y1 = previousItem.Item.Coord.Y;

                //bool? isLeavingPlus = null;
                //if(xStart == x0)
                //{
                //    // check y;
                //    if (y0 < yStart) isLeavingPlus = true;
                //    else isLeavingPlus = false;
                //}
                //else if(yStart == y0)
                //{
                //    // check x
                //    if (x0 < xStart) isLeavingPlus = true;
                //    else isLeavingPlus = false;
                //}

                //bool? isEnteringPlus = null;
                //if(xTarget == x1)
                //{
                //    // check y
                //    if (y1 < yTarget) isEnteringPlus = true;
                //    else isEnteringPlus = false;
                //}
                //else if(yTarget == y1)
                //{
                //    // check x
                //    if (x1 < xTarget) isEnteringPlus = true;
                //    else isEnteringPlus = false;
                //}

                //string startSuffix;
                //string targetSuffix;

                //if (isLeavingPlus == null) startSuffix = "[?]";
                //else if (isLeavingPlus.Value) startSuffix = "[+]";
                //else startSuffix = "[-]";

                //if (isEnteringPlus == null) targetSuffix = "[?]";
                //else if (isEnteringPlus.Value) targetSuffix = "[+]";
                //else targetSuffix = "[-]";

                return $"{startIdentifier}{startSuffix}_{targetIdentifier}{targetSuffix}";
            }
        }

        public enum LeavingEnterType
        {
            Unknown = -1,
            Plus = 1,
            Minus = 2
        }

        public string __getName(LeavingEnterType type)
        {
            switch (type)
            {
                case LeavingEnterType.Minus: return "[-]";
                case LeavingEnterType.Plus: return "[+]";
                case LeavingEnterType.Unknown: return "[?]";
            }

            return "[?]";
        }

        public LeavingEnterType SourceLeavingSide
        {
            get
            {
                var xStart = Start.Coord.X;
                var yStart = Start.Coord.Y;

                var nextItem = Items[1];
                var x0 = nextItem.Item.Coord.X;
                var y0 = nextItem.Item.Coord.Y;

                bool? isLeavingPlus = null;
                if (xStart == x0)
                {
                    // check y;
                    if (y0 < yStart) isLeavingPlus = true;
                    else isLeavingPlus = false;
                }
                else if (yStart == y0)
                {
                    // check x
                    if (x0 < xStart) isLeavingPlus = true;
                    else isLeavingPlus = false;
                }


                if (isLeavingPlus.HasValue)
                {
                    if (isLeavingPlus.Value) return LeavingEnterType.Plus;

                    return LeavingEnterType.Minus;
                }

                return LeavingEnterType.Unknown;
            }
        }

        public LeavingEnterType TargetEnteringSide
        {
            get
            {
                var xTarget = Target.Coord.X;
                var yTarget = Target.Coord.Y;

                var previousItem = Items[Items.Count - 2];
                var x1 = previousItem.Item.Coord.X;
                var y1 = previousItem.Item.Coord.Y;

                bool? isEnteringPlus = null;
                if (xTarget == x1)
                {
                    // check y
                    if (y1 < yTarget) isEnteringPlus = true;
                    else isEnteringPlus = false;
                }
                else if (yTarget == y1)
                {
                    // check x
                    if (x1 < xTarget) isEnteringPlus = true;
                    else isEnteringPlus = false;
                }

                if (isEnteringPlus.HasValue)
                {
                    if (isEnteringPlus.Value) return LeavingEnterType.Plus;

                    return LeavingEnterType.Minus;
                }

                return LeavingEnterType.Unknown;
            }
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public List<PlanItem> Get(PlanItemT type)
        {
            var result = new List<PlanItem>();
            foreach(var it in Items)
            {
                var itItem = it?.Item;
                if (itItem == null) continue;
                if(type == PlanItemT.Block && itItem.IsBlock)
                    result.Add(itItem);
                else if(type == PlanItemT.Sensor && itItem.IsSensor)
                    result.Add(itItem);
                else if(type == PlanItemT.Signal && itItem.IsSignal)
                    result.Add(itItem);
                else if(type == PlanItemT.Switch && itItem.IsSwitch)
                    result.Add(itItem);
                else if(type == PlanItemT.Track && itItem.IsTrack)
                    result.Add(itItem);
            }
            return result;
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public PlanItem Get(string identifier)
        {
            foreach (var it in Items)
            {
                if (string.IsNullOrEmpty(it?.Item?.Identifier))
                    continue;
                if (it.Item.Identifier.Equals(identifier))
                    return it.Item;
            }

            return null;
        }

        /// <summary>
        /// NOTE: not thread-safe
        /// </summary>
        /// <param name="identifier"></param>
        /// <returns></returns>
        public NextStep GetStep(string identifier)
        {
            foreach (var it in Items)
            {
                if (string.IsNullOrEmpty(it?.Item?.Identifier))
                    continue;
                if (it.Item.Identifier.Equals(identifier))
                    return it;
            }

            return null;
        }

        public override string ToString()
        {
            var start = Start.Coord;
            var target = Target.Coord;

            return $"Start({start.X}, {start.Y}) -> Target({target.X}, {target.Y})";
        }
    }
}

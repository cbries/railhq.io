// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteList.cs

using System.Collections.Generic;

namespace libTrackplan.Plan
{
    public class RouteList : List<Route>
    {
        /// <summary>
        /// Queries all direct routes between two blocks.
        /// </summary>
        /// <param name="block0"></param>
        /// <param name="block1"></param>
        /// <returns></returns>
        public RouteList Get(PlanItem block0, PlanItem block1)
        {
            var res = new RouteList();
            if (block0 == null) return res;
            if (block1 == null) return res;
            foreach(var it in this)
            {
                if (it == null) continue;
                var s = it.Start;
                var e = it.Target;
                if(s == null || e == null) continue;

                if (!s.Identifier.Equals(block0.Identifier) && !s.Identifier.Equals(block1.Identifier)) continue;
                if (!e.Identifier.Equals(block0.Identifier) && !e.Identifier.Equals(block1.Identifier)) continue;

                // start of final checks
                if (s.Identifier.Equals(block0.Identifier) && e.Identifier.Equals(block1.Identifier))
                    res.Add(it);
                else if (e.Identifier.Equals(block0.Identifier) && s.Identifier.Equals(block1.Identifier))
                    res.Add(it);
                else if (s.Identifier.Equals(block1.Identifier) && e.Identifier.Equals(block0.Identifier))
                    res.Add(it);
                else if (e.Identifier.Equals(block1.Identifier) && s.Identifier.Equals(block0.Identifier))
                    res.Add(it);
            }

            return res;
        }
    }
}

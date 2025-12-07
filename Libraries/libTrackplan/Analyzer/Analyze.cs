// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: Analyze.cs

using System;
using libTrackplan.Plan;
using libTrackplan.Theming;

namespace libTrackplan.Analyzer
{
    public class Analyze
    {
        private readonly Planfield _planfield;
        private readonly ThemeData _themeData;

        public Analyze(Planfield planfield, ThemeData themeData)
        {
            _themeData = themeData;

            _planfield = planfield;
            _planfield.InitContext(null, themeData);
        }

        public AnalyzeResult Execute(Action<int, int> progressCallback = null)
        {
            //var maxW = _planfield.GetMaxWidth();
            //var maxH = _planfield.GetMaxHeight();

            var allBlocks = _planfield.GetBlocks();
            
            var res = new AnalyzeResult();

            var step = 0;
            var maxSteps = allBlocks.Count;

            foreach (var itBlock in allBlocks)
            {
                var routes = _planfield.GetRoutes(itBlock);
                if (routes.Count == 0) continue;
                res.Routes.AddRange(routes);
                ++step;
                progressCallback?.Invoke(step, maxSteps);
            }

            return res;
        }
    }
}

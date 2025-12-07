// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: RouteAnalyzer.cs

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using libTrackplan.Analyzer;
using libTrackplan.Plan;
using libTrackplan.Theming;
using libUtilities;
using Newtonsoft.Json;

namespace libTrackplan.Route
{
    public delegate void RouteAnalyzerProgress(object sender, RouteAnalyzerState state);
    public delegate void RouteAnalyzerStarted(RouteAnalyzer sender);
    public delegate void RouteAnalyzerFinished(RouteAnalyzer sender);
    public delegate void RouteAnalyzerFailed(RouteAnalyzer sender, string reason);
    public delegate void RouteAnalyzerFailedEx(RouteAnalyzer sender, Exception reason);

    public class RouteAnalyzerState
    {
        public string Message { get; set; }
    }

    public class RouteAnalyzer
    {
        public event RouteAnalyzerProgress Progress;
        public event RouteAnalyzerStarted Started;
        public event RouteAnalyzerFinished Finished;
        public event RouteAnalyzerFailed Failed;
        public event RouteAnalyzerFailedEx FailedEx;

        private static Planfield LoadPlanFieldFile(string path)
        {
            var json = File.ReadAllText(path, Encoding.UTF8);
            var field = LoadPlanField(json);
            return field;
        }

        private static Planfield LoadPlanField(string json)
        {
            var planfield = JsonConvert.DeserializeObject<Dictionary<string, Planfield>>(json);
            var field = planfield["planfield"];
            field.InitContext(null, null);
            return field;
        }

        public async Task Start(string inputMetamodelPath, string outputRoutePath, ThemeData themeData)
        {
            Started?.Invoke(this);
            Progress?.Invoke(this, new RouteAnalyzerState { Message = "Started" });

            if(string.IsNullOrEmpty(inputMetamodelPath) )
            {
                Logging.Log.Debug($"Input metamodel file is not set.");
                return;
            }
            if(!File.Exists(inputMetamodelPath))
            {
                Logging.Log.Debug($"Input metamodel file does not exist: {inputMetamodelPath}");
                return;
            }

            var fname = System.IO.Path.GetFileName(inputMetamodelPath);

            await Task.Run(() =>
            {
                try
                {
                    var field = LoadPlanFieldFile(inputMetamodelPath);
                    if(field != null)
                        Logging.Log.Debug($"Field is loaded, items: {field.Count}");

                    var analyzer = new Analyze(field, themeData);
                    var analyzerResult = analyzer.Execute((step, maxSteps) =>
                    {
                        var m = GetProgressMessage(step, maxSteps, $"Analyzing {fname}");
                        Progress?.Invoke(this, new RouteAnalyzerState { Message = $"{m}" });
                    });
                    Logging.Log.Debug($"Found {analyzerResult.NumberOfRoutes} routes.");
                    var json = analyzerResult.ToJson();
                    if (string.IsNullOrEmpty(json))
                        Failed?.Invoke(this, $"Result of the analyze call is empty.");
                    json.FixBomIfNeeded();
                    StringExtensions.WriteAllTextNoBom(outputRoutePath, json, out _);

                    Finished?.Invoke(this);
                }
                catch (Exception ex)
                {
                    FailedEx?.Invoke(this, ex);
                }
            });
        }

        private static string GetProgressMessage(int step, int maxStep, string msg)
        {
            return $"{msg} {(int)(step / (float)maxStep * 100.0)}%";
        }
    }
}

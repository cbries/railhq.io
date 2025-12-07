// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using libShared.DataProvider;
using libTrackplan.Analyzer;
using libTrackplan.Plan;
using libTrackplan.Trackdata;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libMetamodel
{
    public class MetamodelData
    {
        public Planfield Planfield { get; private set; }
        public JArray Routes { get; private set; }
        public JObject SystemInfo { get; private set; }
        public Settings.ISettings Settings { get; private set; }

        public void Reset()
        {
            Planfield = null;
            Routes = null;
            SystemInfo = null;
            Settings = null;
        }

        private static async Task CreateDummyFile(string path, string cnt)
        {
            try
            {
                if (path != null)
                    if (!File.Exists(path))
                        await File.WriteAllTextAsync(path, cnt, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        private FileInfo _recentRoutesFileInfo = null;

        public async Task<bool> CreateDummyFiles(
            string pathPlanfield,
            string pathRoutes,
            string pathSettings
        )
        {
            try
            {
                await CreateDummyFile(pathPlanfield, "{}");
                await CreateDummyFile(pathRoutes, "[]");
                await CreateDummyFile(pathSettings, "{}");
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return true;
        }

        public async Task<bool> LoadPlanfield(FileInfo info)
        {
            if (!info.Exists)
            {
                Logging.Log.Debug($"Planfield file does not exist: {info}");
                Planfield = new Planfield();
                return false;
            }

            var trackdata = Trackdata.Instance();
            Planfield = await trackdata.LoadPlanfieldAsync(info.FullName, null);
            return Planfield != null;
        }

        public async Task<bool> ApplyRoutes(AnalyzeResult analyzeResult)
        {
            try
            {
                if (analyzeResult == null) return false;
                var json = analyzeResult.ToJson();
                Routes = JArray.Parse(json);
                await File.WriteAllTextAsync(_recentRoutesFileInfo.FullName, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        public async Task<bool> ApplyRoutes(libTrackplan.Route.RouteList routes)
        {
            try
            {
                var json = JsonConvert.SerializeObject(routes);
                Routes = JArray.Parse(json);
                await File.WriteAllTextAsync(_recentRoutesFileInfo.FullName, json, Encoding.UTF8);
                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        public libTrackplan.Route.Route GetRoute(string routeIdentifier)
        {
            var routesList = JsonConvert.DeserializeObject<libTrackplan.Route.RouteList>(Routes.ToString(Formatting.None));
            return routesList?.GetByName(routeIdentifier);
        }

        public async Task<bool> LoadRoutes(FileInfo info)
        {
            _recentRoutesFileInfo = info;

            if (!info.Exists)
            {
                Logging.Log.Debug($"Routes file does not exist: {info}");
                Routes = new JArray();
                return false;
            }

            var cnt = await File.ReadAllTextAsync(info.FullName, Encoding.UTF8);
            Routes = JArray.Parse(cnt);

            return true;
        }

        public void UpdateSystemInfo(string uid, Dictionary<DataProviderType, List<string>> overview)
        {
            SystemInfo = new JObject
            {
                { "dataProvider", JObject.FromObject(overview) }
            };
        }

        public async Task<bool> LoadSettings(FileInfo info, string uid)
        {
            if (!info.Exists)
            {
                Logging.Log.Debug($"Settings file does not exist: {info}");
                Settings = new Settings.Settings();
                return false;
            }

            if (string.IsNullOrEmpty(uid)) return false;

            try
            {
                var cnt = await File.ReadAllTextAsync(info.FullName, Encoding.UTF8);
                Settings = JsonConvert.DeserializeObject<Settings.Settings>(cnt);
                Settings.OriginalFile = info;
                return true;
            }
            catch (Exception ex)
            {
                Logging.Log.Warn($"Load of settings failed: {ex.GetExceptionMessages()}");
            }

            return false;
        }
    }
}
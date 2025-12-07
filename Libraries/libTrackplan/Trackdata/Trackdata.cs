// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using libTrackplan.Plan;
using libTrackplan.Theming;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libTrackplan.Trackdata
{
    public class Trackdata : ISerialization
    {
        private Trackdata() { }

        public static Trackdata Instance()
        {
            return new Trackdata();
        }

        #region ISerialization

        public async Task<string> ReadFileAsync(string path)
        {
            try
            {
                if (string.IsNullOrEmpty(path)) return string.Empty;
                if (!File.Exists(path)) return string.Empty;
                return await File.ReadAllTextAsync(path, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, $"ReadAllText failed ({path})");
            }

            return string.Empty;
        }

        public async Task<JArray> ReadFileAsArrayAsync(string path)
        {
            var cnt = await ReadFileAsync(path);
            if (string.IsNullOrEmpty(cnt)) return [];
            try
            {
                return JArray.Parse(cnt);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, $"ReadFileAsArrayAsync failed ({path})");
                return [];
            }
        }

        public async Task<JObject> ReadFileAsObjectAsync(string path)
        {
            var cnt = await ReadFileAsync(path);
            if (string.IsNullOrEmpty(cnt)) return [];
            try
            {
                return JObject.Parse(cnt);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, $"ReadFileAsObjectAsync failed ({path})");
            }
            return [];
        }

        #endregion

        public async Task<Planfield> LoadPlanfieldAsync(string path, ThemeData themeData)
        {
            var cnt = await ReadFileAsync(path);
            if (string.IsNullOrEmpty(cnt)) return null;
            var planfield = JsonConvert.DeserializeObject<Planfield>(cnt);
            planfield.InitContext(new FileInfo(path), themeData);
            return planfield;
        }
    }
}

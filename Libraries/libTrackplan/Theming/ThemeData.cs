// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using libUtilities;
using Newtonsoft.Json;

namespace libTrackplan.Theming
{
    public class ThemeData
    {
        public Theme Data { get; private set; }

        public async Task<bool> Load(FileInfo info)
        {
            if (!info.Exists)
            {
                Logging.Log.Debug($"Theme file does not exist: {info}");
                return false;
            }

            var cnt = await File.ReadAllTextAsync(info.FullName, Encoding.UTF8);
            Data = JsonConvert.DeserializeObject<Theme>(cnt);

            return true;
        }

        public ThemeItem GetThemeItemBy(int themeId)
        {
            if (themeId < 0) return null;

            foreach (var it in Data.ThemeItems)
            {
                foreach (var itt in it.Objects)
                {
                    if (itt.Id == themeId)
                        return itt;
                }
            }

            return null;
        }
    }
}
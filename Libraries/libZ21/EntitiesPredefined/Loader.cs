// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace libZ21.EntitiesPredefined
{
    public static class Loader
    {
        public static bool IsValidPreloadFileName(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return false;
            var fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName)) return false;
            // Example rule: file must end with .json and not start with a dot
            if (!fileName.EndsWith(".json", System.StringComparison.OrdinalIgnoreCase)) return false;
            if (fileName.StartsWith(".")) return false;
            // check for:  {driverName}_{objectId}.json
            var parts = fileName.Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2) return false;
            return true;
        }

        public static IReadOnlyList<IPredefinedEntity> GetJsonFiles(string path, EntityFileType type)
        {
            var res = new List<IPredefinedEntity>();

            try
            {
                var jsonFiles = Directory.EnumerateFiles(path, "*.json", SearchOption.TopDirectoryOnly);
                foreach (var it in jsonFiles)
                {
                    if (!IsValidPreloadFileName(it)) 
                        continue;

                    try
                    {
                        var cnt = File.ReadAllText(it, Encoding.UTF8);

                        switch (type)
                        {
                            case EntityFileType.Accessory:
                            {
                                var instance = JsonConvert.DeserializeObject<PredefinedEntityAccessory>(cnt);
                                res.Add(instance);
                            }
                                break;

                            case EntityFileType.Locomotive:
                            {
                                var instance = JsonConvert.DeserializeObject<PredefinedEntityLocomotive>(cnt);
                                res.Add(instance);
                            }
                                break;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
            catch
            {
                // ignore
            }

            return res;
        }
    }
}

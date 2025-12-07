// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace libTrackplan.Trackdata
{
    public enum ModelType
    {
        Tracks,
        Routes,
        Locomotives
    }

    public interface ISerialization
    {
        Task<string> ReadFileAsync(string path);
        Task<JArray> ReadFileAsArrayAsync(string path);
        Task<JObject> ReadFileAsObjectAsync(string path);
    }
}

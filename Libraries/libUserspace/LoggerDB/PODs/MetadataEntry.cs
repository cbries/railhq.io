// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;
using System.Collections.Generic;

namespace libUserspace.LoggerDB.PODs
{
    public class MetadataEntry
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string DriverName { get; set; } = string.Empty;
        public int ObjectId { get; set; }
        public string Protocol { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int MaxSpeed { get; set; }
        public string Functions { get; set; } = string.Empty; // JSON als String gespeichert

        public List<FuncDescItem> GetFunctions()
        {
            return JsonConvert.DeserializeObject<List<FuncDescItem>>(Functions) ?? new List<FuncDescItem>();
        }
    }

    public class FuncDescItem
    {
        public int FunctionIndex { get; set; }
        public int FunctionType { get; set; }
        public bool Moment { get; set; }
        public bool State { get; set; }
    }

}

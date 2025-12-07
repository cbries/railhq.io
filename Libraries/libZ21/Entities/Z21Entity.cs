// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities;
using Newtonsoft.Json.Linq;
using System;

namespace libZ21.Entities
{
    public class Z21Entity : IEntity
    {
        public int ObjectId { get; internal set; } = -1;
        public virtual EntityType Type { get; } = EntityType.None;
        public string DisplayName { get; set; } = string.Empty;
        public bool IsEnabled { get; set; } = true;
        public string Name0 { get; set; } = string.Empty;
        public string Name1 { get; set; } = string.Empty;
        public string Name2 { get; set; } = string.Empty;
        public string DriverName { get; set; } = Globals.Z21Identifier;

        public virtual JObject ToJsonObject()
        {
            throw new NotImplementedException();
        }

        public virtual bool ParseData(object data)
        {
            throw new NotImplementedException();
        }
    }
}

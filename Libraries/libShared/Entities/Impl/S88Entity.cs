// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;
using System;

namespace libShared.Entities.Impl
{
    public class S88Entity : IEntityS88
    {
        public virtual bool ParseData(object data)
        {
            throw new NotImplementedException();
        }

        public int Port { get; set; } = 1;
        public int MaxPorts { get; set; } = 1;
        public int Pins { get; set; } = 16;
        public string HexState { get; set; } = "0000";
        public string BinaryState { get; set; } = "0000000000000000";
        public string DriverName { get; set; } = "S88";

        public virtual JObject ToJsonObject()
        {
            var o = new JObject
            {
                ["driverName"] = DriverName,
                ["port"] = Port,
                ["pins"] = Pins,
                ["hex"] = HexState,
                ["binary"] = BinaryState
            };

            return o;
        }
    }
}

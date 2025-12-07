// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace libShared.Entities
{
    public interface IAccessory : IEntity
    {
        [JsonProperty("address")] string Address { get; }
        [JsonProperty("protocol")] string Protocol { get; }
        [JsonProperty("category")] string Category { get; }
        [JsonProperty("addrExt")] List<string> AddrExt { get; }
        [JsonProperty("mode")] string Mode { get; }
        [JsonProperty("symbol")] string Symbol { get; }
        [JsonProperty("gates")] int Gates { get; }
        [JsonProperty("state")] string State { get; }
        [JsonProperty("switching")] string Switching { get; }

        public int GetStateIndex()
        {
            if (int.TryParse(State, out var istate))
                return istate;
            return -1;
        }
    }
}

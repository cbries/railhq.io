// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using libShared.Entities;
using libZ21.EntitiesPredefined;
using Newtonsoft.Json.Linq;

// ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

namespace libZ21.Entities
{
    public class Accessory : Z21Entity, IAccessory
    {
        public override EntityType Type => EntityType.Accessory;

        public string Address { get; private set; } = string.Empty;
        public string State { get; private set; } = "0";

        // Kann auf "2" festgelegt werden.
        // Bei z21 haben Schaltartikel wohl immer zwei Zustände.
        public int Gates { get; set; } = 2;

        public string Protocol { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public List<string> AddrExt { get; } = new();
        public string Mode { get; private set; } = string.Empty;
        public string Symbol { get; private set; } = string.Empty;
        public string Switching { get; private set; } = string.Empty;

        public override JObject ToJsonObject()
        {
            var o = new JObject
            {
                ["name1"] = DisplayName,
                ["driverName"] = DriverName,
                ["objectId"] = Address,
                ["type"] = "Accessory",
                ["protocol"] = Protocol,
                ["addrext"] = new JArray
                    {
                        $"{Address}g",
                        $"{Address}r"
                    },
                ["addr"] = Address,
                ["state"] = State,
                ["gates"] = Gates,
                ["mode"] = "SWITCH"
            };

            return o;
        }

        public override bool ParseData(object data)
        {
            if (data is PredefinedEntityAccessory predefinedAccData)
            {
                Name0 = $"{predefinedAccData.Name}";
                DisplayName = predefinedAccData.Name;
                Address = $"{predefinedAccData.Address}";
                ObjectId = predefinedAccData.Address;
                Protocol = predefinedAccData.Protocol;

                return true;
            }

            if (data is AccessoryInfo accData)
            {
                var changed = false;

                if (!Address.Equals($"{accData.Address}"))
                {
                    Address = $"{accData.Address}";
                    changed = true;
                }

                if (!State.Equals($"{accData.State}"))
                {
                    if (accData.State.HasValue)
                    {
                        State = $"{(accData.State.Value ? 0 : 1)}";
                        changed = true;
                    }
                }

                return changed;
            }

            return false;
        }
    }
}

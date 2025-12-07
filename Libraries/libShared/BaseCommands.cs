// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libShared
{
    public class BaseCommands
    {
        public static JObject CmdShutdown = new()
        {
            {"objectId", 1},
            {"command", "update"},
            {"argument", "power"},
            {"argumentValue", "SHUTDOWN"}
        };

        public static JObject CmdEmergencyStop = new()
        {
            {"objectId", 1},
            {"command", "emergencyStop"},
            {"argument", "emergencyStop"},
            {"argumentValue", "emergencyStop"}
        };

        public static JObject CmdInitialize = new()
        {
            {"objectId", 1},
            {"command", "initialize"},
            {"argument", "initialize"},
            {"argumentValue", new JObject
                {
                    {"initViews", true},
                    {"initAccessories", false}
                }
            }
        };

        public static JObject GetRelayCommand(string driverName, IPayload payload)
        {
            var jsonPayload = JsonConvert.SerializeObject(payload);
            var gatewayCommand = new JObject
            {
                {"driverName", driverName},
                {"objectId", -1},
                {"command", "relay"},
                {"argument", "relay"},
                {"argumentValue", jsonPayload}
            };
            return gatewayCommand;
        }
    }
}

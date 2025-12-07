// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;


namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class CommandFactory
    {
        public static JObject GetStationPowerCommand(string driverName, bool state)
        {
            /*
             {
                   "command": "system",
                   "cmddata": {
                       "command": "update",
                       "argument": "power",
                       "argumentValue": {
                           "driverName": "demo",
                           "action": "powerOn" | "powerOff"
                       }
                   },
                   "objectId": -1
               }
             */
            var obj = new JObject
            {
                { "command", "system" },
                { "cmddata", new JObject
                    {
                        {"command", "update"},
                        {"argument", "power"},
                        {"argumentValue", new JObject
                            {
                                {"driverName", driverName},
                                {"action", state ? "powerOn" : "powerOff"}
                            }
                        }
                    }
                }
            };

            return obj;
        }

        public static JObject GetSpeedCommand(string driverName, int address, int speed)
        {
            /*
             {
               "command": "locomotive",
               "cmddata": {
                 "objectId": 1010,
                 "command": "update",
                 "argument": "speedstep",
                 "argumentValue": {
                   "driverName": "ecos",
                   "value": "levelMinimum"
                 }
               }
             */
            var obj = new JObject
            {
                { "command", "locomotive" },
                { "cmddata", new JObject
                    {
                        {"objectId", address},
                        {"command", "update"},
                        {"argument", "speedstep"},
                        {"argumentValue", new JObject
                            {
                                {"driverName", driverName},
                                {"value", speed}
                            }
                        }
                    }
                }
            };

            return obj;
        }

        public static JObject GetDirectionCommand(string driverName, int address, bool forward)
        {
            /*
             {
                 "command": "locomotive",
                 "cmddata": {
                   "objectId": 1010,
                   "command": "update",
                   "argument": "direction",
                   "argumentValue": {
                     "driverName": "ecos",
                     "direction": "backward"
                   }
                 }
               }
             */
            var obj = new JObject
            {
                { "command", "locomotive" },
                { "cmddata", new JObject
                    {
                        {"objectId", address},
                        {"command", "update"},
                        {"argument", "direction"},
                        {"argumentValue", new JObject
                            {
                                {"driverName", driverName},
                                {"direction", forward ? "forward" : "backward"}
                            }
                        }
                    }
                }
            };

            return obj;
        }

        public static JObject GetFunctionCommand(
            string driverName, 
            int address,
            int fncidx,
            bool state)
        {
            /*
                {
                 "command": "locomotive",
                 "timestamp": 1747987842450,
                 "cmddata": {
                   "objectId": 1010,
                   "command": "update",
                   "argument": "function",
                   "argumentValue": {
                     "driverName": "ecos",
                     "value": [0, 1]
                   }
                 }
               }            
             */
            var obj = new JObject
            {
                { "command", "locomotive" },
                { "cmddata", new JObject
                    {
                        { "objectId", address },
                        { "command", "update" },
                        { "argument", "function" },
                        { "argumentValue", new JObject
                            {
                                { "driverName", driverName },
                                { "value", new JArray(fncidx, state ? 1 : 0) }
                            }
                        }
                    }
                }
            };

            return obj;
        }

        public static JObject GetAccessorySwitchCommand(
            string driverName,
            int address,
            string targetState)
        {
            /*
               {
                 "command": "accessory",
                 "timestamp": 1748089287071,
                 "cmddata": {
                   "objectId": 20001,
                   "command": "update",
                   "argument": "targetState",
                   "argumentValue": {
                     "driverName": "ecos",
                     "addrIndex": 0
                   }
                 },
                 "objectId": -1
               }
             */
            var obj = new JObject
            {
                { "command", "accessory" },
                { "cmddata", new JObject
                    {
                        { "objectId", address },
                        { "command", "update" },
                        { "argument", "targetState" },
                        { "argumentValue", new JObject
                            {
                                { "driverName", driverName },
                                { "addrIndex", targetState }
                            }
                        }
                    }
                }
            };

            return obj;
        }
    }
}

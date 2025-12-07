// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using libShared.MessageProtocol;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebApp.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
// ReSharper disable TooWideLocalVariableScope
// ReSharper disable UsePatternMatching

namespace railyWebApp.Playground
{
    public class PgControlStation
    {
        internal static int GetCalculatedSpeedstep(
            JObject argumentValue,
            ILocomotive locEntity,
            libUserspace.Workspace userWorkspace
            )
        {
            var speedstep = -1;

            var strValue = argumentValue.GetString("value");

            var driverName = locEntity?.DriverName;
            int objectId = -1;
            if (locEntity != null)
                objectId = locEntity.ObjectId;

            if (!string.IsNullOrEmpty(strValue))
            {
                var doIncrement = strValue.Trim().Equals("++", StringComparison.Ordinal);
                var doDecrement = strValue.Trim().Equals("--", StringComparison.Ordinal);

                var speedMinimum = strValue.Trim().Equals("levelMinimum", StringComparison.OrdinalIgnoreCase);
                var speedEnter = strValue.Trim().Equals("levelEnter", StringComparison.OrdinalIgnoreCase);
                var speedCruise = strValue.Trim().Equals("levelCruise", StringComparison.OrdinalIgnoreCase);
                var speedMax = strValue.Trim().Equals("levelMax", StringComparison.OrdinalIgnoreCase);

                var locSettings = userWorkspace.Metamodel.Settings.FindLocomotiveBy(driverName, objectId);
                if (locSettings == null)
                {
                    if (locEntity != null)
                    {
                        var speed = libMetamodel.Settings.Locomotive.GetSpeedBy(locEntity.Protocol);
                        if (speedMinimum) speedstep = speed.Minimum;
                        else if (speedEnter) speedstep = speed.Entering;
                        else if (speedCruise) speedstep = speed.Traveling;
                        else if (speedMax) speedstep = speed.Maximum;
                    }
                }
                else
                {
                    if (speedMinimum) speedstep = locSettings.Speed.Minimum;
                    else if (speedEnter) speedstep = locSettings.Speed.Entering;
                    else if (speedCruise) speedstep = locSettings.Speed.Traveling;
                    else if (speedMax) speedstep = locSettings.Speed.Maximum;
                }

                // when not set already
                if (speedstep < 0 && locEntity != null)
                {
                    if (doIncrement)
                    {
                        speedstep = locEntity.Speedstep;

                        ++speedstep;
                    }
                    else if (doDecrement)
                    {
                        speedstep = locEntity.Speedstep;

                        --speedstep;

                        if (speedstep < 0)
                            speedstep = 0;
                    }
                }
            }

            if (speedstep == -1)
            {
                var value = argumentValue.GetString("value");
                if (value.Equals("level0", StringComparison.OrdinalIgnoreCase))
                {
                    speedstep = 0;
                }
                else
                {
                    speedstep = int.TryParse(value, out var s) ? s : 0;
                }
            }

            if (speedstep <= 0)
                speedstep = 0;

            if (locEntity != null)
            {
                if (speedstep > locEntity.MaxSpeed)
                    speedstep = locEntity.MaxSpeed;
            }

            return speedstep;
        }

        internal static string GetToggleEcosStatus(libEsuEcos.Entities.Ecos2 ecos2Entity)
        {
            // GO
            // STOP
            // SHUTDOWN
            var currentState = ecos2Entity.Status;
            if (currentState.Equals("GO", StringComparison.OrdinalIgnoreCase))
                return "STOP";
            if (currentState.Equals("STOP", StringComparison.OrdinalIgnoreCase))
                return "GO";
            return string.Empty;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="request"></param>
        /// <param name="requestor"></param>
        /// <returns>true when handled</returns>
        internal static async Task<bool> HandleWebClientRequests(string uid, libShared.ExchangeProtocol.Request request, WebSocket requestor = null)
        {
            if (!PgHelper.HasCommand(request, out var cmd)) return false;
            if (request.Data.Payload is not JObject payloadData) return false;
            var cmddata = payloadData["cmddata"] as JObject;
            if (cmddata == null) return false;

            //
            // currently only
            //  (a) ESU ECoS
            //  (b) z21
            // locomotive entities are supported
            //
            var argumentValue = cmddata["argumentValue"] as JObject;
            var driverName = argumentValue.GetString("driverName");
            var objectId = cmddata.GetInt("objectId", -1);
            if (!driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier)
                && !driverName.Equals(libZ21.Globals.Z21Identifier))
            {
                return false;
            }

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres || userWorkspace == null)
            {
                Logging.Log.Warn($"Workspace not loaded nor available.");
                return false;
            }

            var updateMode = "update";
            if (userWorkspace.AutomaticRunner != null && userWorkspace.AutomaticRunner.IsStarted())
            {
                Logging.Log.Info("Automatic is started: no force");

                updateMode = "updateNoForce";
            }

            var dpEcos = DataProviderManager.GetDataProvider(uid, DataProviderType.ECoS50210).FirstOrDefault();
            var locEntitiesEcos = dpEcos?.Entities as IReadOnlyCollection<IEntity>;
            var isEcos = driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase);

            var dpZ21 = DataProviderManager.GetDataProvider(uid, DataProviderType.Z21).FirstOrDefault() as libZ21.DataProvider.DataProvider;
            var locEntitiesZ21 = dpZ21?.LocomotivesEntities as IReadOnlyCollection<IEntity>;
            var isZ21 = driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase);

            switch (cmd)
            {
                case "refreshEntity" when PgHelper.IsArgument(cmddata, "entity"):
                    {
                        JObject entity = null;
                        if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            entity = PgDpHelper.__getEntityFromDp(dpEcos, objectId);
                        }
                        else if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            entity = PgDpHelper.__getEntityFromDp(dpZ21, objectId);
                        }

                        if (entity == null)
                        {
                            Logging.Log.Info($"Entity does not exist: {driverName}::{objectId}");
                            return true;
                        }

                        var entityData = new JObject
                        {
                            ["command"] = "update",
                            ["entityType"] = "locomotive",
                            ["entityData"] = entity
                        };

                        await PgHelper.SendToClient(requestor, entityData.ToString(Formatting.None));
                    }
                    return true;

                case "locomotive" when PgHelper.IsArgument(cmddata, "speedstep"):
                    {
                        JObject gatewayCommand = null;
                        ILocomotive locEntity = null;

                        if (isEcos)
                        {
                            locEntity =
                                locEntitiesEcos?.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId)
                                    as ILocomotive;
                        }

                        if (isZ21)
                        {
                            locEntity =
                                locEntitiesZ21?.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId)
                                    as ILocomotive;
                        }

                        if (locEntity == null) return false;

                        var speedstep = GetCalculatedSpeedstep(argumentValue, locEntity, userWorkspace);
                        var speedstepObject = new MessageSpeedstep
                        {
                            Speed = speedstep,
                            MaxSpeedSteps = locEntity.Protocol,
                            Direction = (int)locEntity.Direction

                        };
                        gatewayCommand = new JObject
                        {
                            { "driverName", driverName },
                            { "objectId", cmddata.GetInt("objectId", -1) },
                            { "command", updateMode },
                            { "argument", "speedstep" },
                            { "argumentValue", speedstepObject.ToJson() }
                        };

                        await PgHelper.SendToGateway(uid, gatewayCommand);
                    }
                    return true;

                case "locomotive" when PgHelper.IsArgument(cmddata, "direction"):
                    {
                        ILocomotive locEntity;
                        JObject gatewayCommand;

                        //
                        // ESU ECoS
                        //
                        if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            await SendEcosStateIfNeeded(requestor, uid);

                            //
                            // erst die Geschwindigkeit auf Null setzen
                            //
                            gatewayCommand = new JObject
                            {
                                { "driverName", driverName },
                                { "objectId", cmddata.GetInt("objectId", -1) },
                                { "command", updateMode },
                                { "argument", "speedstep" },
                                { "argumentValue", 0 }
                            };
                            await PgHelper.SendToGateway(uid, gatewayCommand);

                            //
                            // dann erst den Richtungswechsel vornehmen
                            //
                            var direction = argumentValue.GetString("direction");
                            if (!int.TryParse(direction, out var value))
                            {
                                if (!string.IsNullOrEmpty(direction))
                                    value = direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                            }

                            gatewayCommand = new JObject
                            {
                                { "driverName", driverName },
                                { "objectId", cmddata.GetInt("objectId", -1) },
                                { "command", updateMode },
                                { "argument", "direction" },
                                { "argumentValue", value }
                            };

                            await PgHelper.SendToGateway(uid, gatewayCommand);
                        }
                        //
                        // z21
                        //
                        else if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            locEntity = locEntitiesZ21?.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId) as ILocomotive;
                            if (locEntity != null)
                            {
                                //
                                // erst die Geschwindigkeit auf Null setzen
                                //
                                gatewayCommand = new JObject
                                {
                                    { "driverName", driverName },
                                    { "objectId", cmddata.GetInt("objectId", -1) },
                                    { "command", updateMode },
                                    { "argument", "speedstep" },
                                    {
                                        "argumentValue", new JObject
                                        {
                                            { "speed", 0 },
                                            { "maxSpeedSteps", locEntity.Protocol },
                                            { "direction", (int)locEntity.Direction }
                                        }
                                    }
                                };

                                await PgHelper.SendToGateway(uid, gatewayCommand);

                                //
                                // dann erst den Richtungswechsel vornehmen
                                //
                                var direction = argumentValue.GetString("direction");
                                if (!int.TryParse(direction, out var value))
                                {
                                    if (!string.IsNullOrEmpty(direction))
                                        value = direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                                }

                                gatewayCommand = new JObject
                                {
                                    { "driverName", driverName },
                                    { "objectId", cmddata.GetInt("objectId", -1) },
                                    { "command", updateMode },
                                    { "argument", "direction" },
                                    {
                                        "argumentValue", new JObject
                                        {
                                            { "speed", 0 },
                                            { "maxSpeedSteps", locEntity.Protocol },
                                            { "direction", value }
                                        }
                                    }
                                };

                                await PgHelper.SendToGateway(uid, gatewayCommand);
                            }
                        }
                    }
                    return true;

                case "locomotive" when PgHelper.IsArgument(cmddata, "function"):
                    {
                        await SendEcosStateIfNeeded(requestor, uid);

                        var gatewayCommand = new JObject
                        {
                            {"driverName", driverName},
                            {"objectId", cmddata.GetInt("objectId", -1)},
                            {"command", updateMode},
                            {"argument", "function"},
                            {"argumentValue", argumentValue?["value"] as JArray}
                        };

                        await PgHelper.SendToGateway(uid, gatewayCommand);
                    }
                    return true;

                case "accessory":
                    {
                        Logging.Log.Debug($"accessory: {cmddata}");

                        //
                        // ESU ECoS
                        //
                        if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                        {
                            await SendEcosStateIfNeeded(requestor, uid);

                            // sample command
                            /*
                                {
                                    "objectId": 20020,
                                    "command": "update",
                                    "argument": "targetState",
                                    "argumentValue": {
                                           driverName: rec.driverName,
                                           addrIndex: addrIndex
                                       }
                                }
                             */

                            var addrIndex = argumentValue.GetInt("addrIndex", -1);
                            if (addrIndex == -1) return false;
                            // change command to fit RailyGateways command structure
                            cmddata["argumentValue"] = addrIndex;
                            // add driverName
                            cmddata["driverName"] = driverName;

                            await PgHelper.SendToGateway(uid, cmddata);

                            return true;
                        }

                        //
                        // z21
                        //
                        if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            var o = argumentValue as JObject;
                            var addrIndex = o?.GetInt("addrIndex", -1) ?? -1;
                            if (addrIndex == -1) return false;

                            var c = new JObject
                            {
                                {"objectId", cmddata.GetInt("objectId")},
                                {"driverName", driverName},
                                {"command", "update"},
                                {"argument", "targetState"},
                                {"argumentValue", addrIndex}
                            };

                            await PgHelper.SendToGateway(uid, c);

                            return true;
                        }

                    }
                    break;
            }

            return false;
        }

        private static async Task SendEcosStateIfNeeded(WebSocket requestor, string uid)
        {
            if (requestor == null) return;
            if (string.IsNullOrEmpty(uid)) return;

            // nicht den GO-Status prüfen und senden wenn wir m Simulationsmodus sind
            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (wsres && userWorkspace != null && userWorkspace.SimulationEnabled)
            {
                // ignore
            }
            else
            {
                var dps = DataProviderManager.GetDataProviders(uid);
                if (!dps.IsEcosInGo(out var message))
                {
                    if (message != null)
                        await PgHelper.SendToClient(requestor, message.ToString(Formatting.None));
                }
            }
        }
    }
}

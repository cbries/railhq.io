// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos.Blocks;
using libEsuEcos.Entities;
using libShared;
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

namespace railyWebApp.Playground
{
    public class PgDemo
    {
        public const string DriverName = DataProviderDemo.ProviderName;

        internal static JObject __getDemoData(IDataProvider dpDemo)
        {
            var data = new JObject();

            if (dpDemo?.Entities == null || dpDemo.Entities.Count == 0) return data;

            var arrLocomotives = new JArray();
            var arrAccessories = new JArray();

            foreach (var it in dpDemo.Entities)
            {
                if (it is not IEntity entity) continue;
                var jsonObj = __getObject(entity);
                if (jsonObj == null) continue;

                switch (entity.Type)
                {
                    case EntityType.Locomotive:
                        arrLocomotives.Add(jsonObj);
                        break;
                    case EntityType.Accessory:
                        arrAccessories.Add(jsonObj);
                        break;
                    case EntityType.Ecos2:
                        data.Add("demobase", jsonObj);
                        break;
                }
            }

            if (arrLocomotives.Count > 0) data.Add("locomotives", arrLocomotives);
            if (arrAccessories.Count > 0) data.Add("accessories", arrAccessories);

            return data;
        }

        internal static JObject __getObject(IEntity entity)
        {
            if (entity == null) return null;

            switch (entity.Type)
            {
                case EntityType.Locomotive:
                    {
                        var itLoc = entity as Locomotive;
                        if (itLoc == null) return null;

                        var arFncDesc = new JArray();
                        for (var i = 0; i < itLoc.Functions.Count; ++i)
                        {
                            if (itLoc.Functions[i].FunctionType == 0) continue;

                            var odesc = new JObject
                            {
                                ["idx"] = i,
                                ["state"] = itLoc.Functions[i].State,
                                ["type"] = itLoc.Functions[i].FunctionType,
                                ["moment"] = itLoc.Functions[i].Moment
                            };
                            arFncDesc.Add(odesc);
                        }

                        var o = new JObject
                        {
                            // Wir forcieren an dieser Stelle den DriverName als "demo".
                            ["driverName"] = DriverName,

                            ["objectId"] = itLoc.ObjectId,
                            ["name"] = itLoc.DisplayName,
                            ["protocol"] = itLoc.Protocol,
                            ["addr"] = itLoc.Address,

                            ["speedstep"] = itLoc.Speedstep,
                            ["speedstepMax"] = LocomotiveUtilities.GetNumberOfSpeedsteps(itLoc.Protocol),
                            ["direction"] = (int)itLoc.Direction,
                            ["funcdesc"] = arFncDesc,
                            ["nrOfFunctions"] = itLoc.Functions.Count
                        };

                        return o;
                    }

                case EntityType.Accessory:
                    {
                        var itAcc = entity as Accessory;
                        if (itAcc == null) return null;

                        var o = new JObject
                        {
                            ["name1"] = itAcc.Name0,
                            ["name2"] = itAcc.Name1,
                            ["name3"] = itAcc.Name2
                        };
                        var a0 = new JArray();
                        foreach (var e in itAcc.AddrExt)
                            a0.Add(e);

                        // Wir forcieren an dieser Stelle den DriverName als "demo".
                        o["driverName"] = DriverName;

                        o["objectId"] = itAcc.ObjectId;
                        o["addrext"] = a0;
                        o["addr"] = itAcc.Address;
                        o["protocol"] = itAcc.Protocol;
                        o["type"] = itAcc.Type.ToString();
                        o["mode"] = itAcc.Mode;
                        o["state"] = itAcc.State;
                        o["switching"] = itAcc.Switching;
                        o["gates"] = itAcc.Gates;

                        return o;
                    }

                case EntityType.Ecos2:
                    {
                        var itEcos = entity as Ecos2;
                        if (itEcos == null) return null;

                        var o = new JObject
                        {
                            // Wir forcieren an dieser Stelle den DriverName als "demo".
                            ["driverName"] = DriverName,

                            ["status"] = itEcos.Status,
                            ["name"] = itEcos.DisplayName,
                            ["protocolVersion"] = itEcos.ProtocolVersion,
                            ["applicationVersion"] = itEcos.ApplicationVersion,
                            ["hardwareVersion"] = itEcos.HardwareVersion
                        };

                        return o;
                    }
            }

            return null;
        }

        internal static async Task<bool> HandleWebClientRequests2(
            string uid,
            libShared.ExchangeProtocol.Request request,
            WebSocket requestor = null,
            IDataExchange dataExchange = null)
        {
            if (!PgHelper.HasCommand(request, out var cmd)) return false;
            if (request.Data.Payload is not JObject payloadData) return false;
            var cmddata = payloadData["cmddata"] as JObject;
            if (cmddata == null) return false;

            var dpDemo = DataProviderManager.GetDataProvider(uid, DataProviderType.Demo).FirstOrDefault();
            var entities = dpDemo?.Entities as IReadOnlyCollection<IEntity>;

            switch (cmd)
            {
                case "system" when PgHelper.IsArgument(cmddata, "power"):
                    {
                        var argValues = JsonMessageParser.Parse<MessagePower>(cmddata["argumentValue"]);
                        if (argValues == null) return false;
                        if (!argValues.Action.Equals("toggle", StringComparison.OrdinalIgnoreCase)) return false;
                        if (!argValues.DriverName.Equals(DataProviderDemo.ProviderName, StringComparison.OrdinalIgnoreCase)) return false;

                        const int demobaseId = 1;
                        var baseEntity = entities?.FirstOrDefault(it => it.ObjectId == demobaseId) as Ecos2;
                        if (baseEntity == null) return false;
                        var targetState = PgControlStation.GetToggleEcosStatus(baseEntity);
                        if (string.IsNullOrEmpty(targetState)) return false; // siehe "break"

                        /*
                           <EVENT 1>
                           1 status2[ALL]
                           1 status[GO]
                           <END 0 (OK)>
                         */

                        var s = $"<EVENT {demobaseId}>\n{demobaseId} status[{targetState}]\n{demobaseId}\n<END 0 (OK)>\n";

                        var eventBlock = new EventBlock();
                        eventBlock.Parse(s);

                        var changed = baseEntity.ParseData(eventBlock);
                        if (changed)
                        {
                            (dpDemo as IDataProviderDemoExtension)?.TriggerEntityUpdate(baseEntity);

                            var entity = __getObject(baseEntity);
                            var entityData = new JObject
                            {
                                ["command"] = "update",
                                ["railyData"] = new JObject
                                {
                                    ["demobase"] = entity
                                }
                            };

                            if (dataExchange != null)
                                await dataExchange.SendObjectToAllClients(uid, entityData);
                            else
                                await PgHelper.SendToClient(requestor, entityData.ToString(Formatting.None));
                        }
                    }
                    return false; // Wenn "false", dann werden auch andere Handler aufgerufen
            }

            return false;
        }

        internal static async Task<bool> HandleWebClientRequests(
            string uid,
            libShared.ExchangeProtocol.Request request,
            WebSocket requestor = null,
            IDataExchange dataExchange = null)
        {
            if (!PgHelper.HasCommand(request, out var cmd)) return false;
            if (request.Data.Payload is not JObject payloadData) return false;
            var cmddata = payloadData["cmddata"] as JObject;
            if (cmddata == null) return false;

            var argumentValue = cmddata["argumentValue"] as JObject;
            var driverName = argumentValue.GetString("driverName");
            if (!driverName.Equals(DriverName)) return false;
            var objectId = cmddata.GetInt("objectId", -1);

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres || userWorkspace == null)
            {
                Logging.Log.Warn($"Workspace not loaded nor available.");
                return false;
            }

            var dpDemo = DataProviderManager.GetDataProvider(uid, DataProviderType.Demo).FirstOrDefault();
            var entities = dpDemo?.Entities as IReadOnlyCollection<IEntity>;

            switch (cmd)
            {
                case "locomotive":
                    {
                        var locEntity = entities?.FirstOrDefault(it => it.ObjectId == objectId) as Locomotive;
                        if (locEntity == null) return false;

                        var changed0 = false;
                        var changed1 = false;
                        if (PgHelper.IsArgument(cmddata, "speedstep"))
                        {
                            var speedstep = PgControlStation.GetCalculatedSpeedstep(argumentValue, locEntity, userWorkspace);

                            changed0 = HandleDemoSpeedstep(locEntity, speedstep);
                        }
                        else if (PgHelper.IsArgument(cmddata, "direction"))
                        {
                            //
                            // setze die Geschwindigkeit bei dem Richtungswechsel immer auf Null
                            //
                            changed0 = HandleDemoSpeedstep(locEntity, 0);

                            //
                            // danach ändern wir die Richtung
                            //
                            var direction = argumentValue.GetString("direction");
                            if (!int.TryParse(direction, out var value))
                            {
                                if (!string.IsNullOrEmpty(direction))
                                    value = direction.Equals("forward", StringComparison.OrdinalIgnoreCase) ? 0 : 1;
                            }
                            /*
                               <EVENT 1034>
                               1034 dir[1]
                               <END 0 (OK)>
                             */
                            var s = $"<EVENT {objectId}>\n{objectId} dir[{value}]\n<END 0 (OK)>\n";
                            var eventBlock = new EventBlock();
                            eventBlock.Parse(s);
                            changed1 = locEntity.ParseData(eventBlock);
                        }
                        else if (PgHelper.IsArgument(cmddata, "function"))
                        {
                            /*
                               <EVENT 1034>
                               1034 func[1,0]
                               1034 funcset[00000000000000000000000000000]
                               <END 0 (OK)>                               
                             */

                            var v0 = (argumentValue?["value"] as JArray)[0];
                            var v1 = (argumentValue?["value"] as JArray)[1];

                            var s = $"<EVENT {objectId}>\n{objectId} func[{v0},{v1}]\n<END 0 (OK)>\n";

                            var eventBlock = new EventBlock();
                            eventBlock.Parse(s);

                            changed0 = locEntity.ParseData(eventBlock);
                        }

                        if (changed0 || changed1)
                        {
                            await PublishEntityUpdate(
                                dpDemo as IDataProviderDemoExtension,
                                locEntity,
                                null,
                                dataExchange,
                                uid);
                        }

                        return changed0;
                    }

                case "accessory":
                    {
                        var addrIndex = argumentValue.GetInt("addrIndex", -1);
                        if (addrIndex == -1) return false;

                        if (entities?.FirstOrDefault(it => it.ObjectId == objectId) is Accessory accEntity)
                        {
                            var changed = await HandleAccessory(accEntity, addrIndex, requestor, dataExchange, uid);

                            if (changed)
                            {
                                await PublishEntityUpdate(
                                    dpDemo as IDataProviderDemoExtension,
                                    accEntity,
                                    null,
                                    dataExchange,
                                    uid);
                            }

                            return changed;
                        }
                    }
                    break;
            }

            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dp"></param>
        /// <param name="entity"></param>
        /// <param name="requestor">When `null` the update will be published to all workspace connected clients.</param>
        /// <param name="exchange"></param>
        /// <param name="uid"></param>
        /// <returns></returns>
        internal static async Task PublishEntityUpdate(
            IDataProviderDemoExtension dp,
            IEntity entity,
            WebSocket requestor = null,
            IDataExchange exchange = null,
            string uid = "")
        {
            if (dp == null) return;

            dp.TriggerEntityUpdate(entity);

            var entityType = "none";
            if (entity is IAccessory) entityType = "accessory";
            else if (entity is ILocomotive) entityType = "locomotive";

            var entityDataObject = __getObject(entity);
            var entityData = new JObject
            {
                ["command"] = "update",
                ["entityType"] = entityType,
                ["entityData"] = entityDataObject
            };

            if (requestor != null)
            {
                var json = entityData.ToString(Formatting.None);

                await PgHelper.SendToClient(requestor, json);
            }
            else if (exchange != null)
            {
                await exchange.SendObjectToAllClients(uid, entityData);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="targetSpeed"></param>
        /// <returns>true when changed/updated</returns>
        internal static bool HandleDemoSpeedstep(Locomotive entity, int targetSpeed)
        {
            /*
            <EVENT 1034>
            1034 speed[26]
            1034 speedstep[26]
            <END 0 (OK)>
            */

            var objectId = entity.ObjectId;

            var s = $"<EVENT {objectId}>\n{objectId} speed[{targetSpeed}]\n{objectId} speedstep[{targetSpeed}]\n<END 0 (OK)>\n";

            var eventBlock = new EventBlock();
            eventBlock.Parse(s);
            return entity.ParseData(eventBlock);
        }

        internal static async Task<bool> HandleAccessory(
            Accessory accEntity,
            int addrIndex,
            WebSocket requestor = null,
            IDataExchange dataExchange = null,
            string uid = "")
        {

            var objectId = accEntity.ObjectId;

            /*
                <EVENT 20000>
                20000 state[0]    0 := addrIndex
                <END 0 (OK)>
             */

            var s = $"<EVENT {objectId}>\n{objectId} state[{addrIndex}]\n<END 0 (OK)>\n";

            var eventBlock = new EventBlock();
            eventBlock.Parse(s);

            var res = accEntity.ParseData(eventBlock);
            if (res)
            {
                var entity = __getObject(accEntity);
                var entityData = new JObject
                {
                    ["command"] = "update",
                    ["entityType"] = "accessory",
                    ["entityData"] = entity
                };

                if (requestor != null)
                {
                    await PgHelper.SendToClient(requestor, entityData.ToString(Formatting.None));
                }

                if (dataExchange != null)
                {
                    await dataExchange.SendObjectToAllClients(uid, entityData);

                    try
                    {
                        var debugMsg =
                            $"{accEntity.DriverName}::{accEntity.ObjectId} wurde geschaltet: {accEntity.AddrExt[addrIndex]}";
                        dataExchange?.QueueDebugMessage(uid, debugMsg, DebugMessageT.Accessories);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            return res;
        }
    }
}

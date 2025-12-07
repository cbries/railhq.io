// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos.Entities;
using libShared;
using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libTrackplan.Plan;
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
    public class PgTrackdata
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="request"></param>
        /// <param name="requestor"></param>
        /// <param name="dataExchange"></param>
        /// <returns>true when handled</returns>
        internal static async Task<bool> HandleWebClientRequests(
            string uid,
            Request request,
            WebSocket requestor = null,
            IDataExchange dataExchange = null)
        {
            if (!PgHelper.HasCommand(request, out var cmd)) return false;
            if (request.Data.Payload is not JObject payloadData) return false;
            var cmddata = payloadData["cmddata"] as JObject;
            if (cmddata == null) return false;

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres)
            {
                Logging.Log.Warn($"Workspace not loaded nor available.");
                return false;
            }

            switch (cmd)
            {
                case "trackplan" when cmddata.GetString("argument").Equals("request"):
                    {
                        if (cmddata["argumentValue"] is JObject requestCommand)
                        {
                            var methodName = requestCommand.GetString("methodName");

                            switch (methodName)
                            {
                                case "uniqueIdentifier":
                                    {
                                        var themeId = requestCommand.GetInt("methodParameter", -1);
                                        var uniqueGeneratedId = userWorkspace.Metamodel.Planfield.GetUnusedIdentifierBy((uint)themeId);

                                        var jsonData = new JObject
                                        {
                                            { "command", "reply" },
                                            { "uniqueIdentifier", uniqueGeneratedId }
                                        };

                                        await PgHelper.SendToClient(requestor, jsonData.ToString(Formatting.None));
                                    }
                                    break;
                            }
                        }
                    }
                    return true;

                case "trackplan" when cmddata.GetString("argument").Equals("updateControl"):
                    {
                        /*
                            2025-01-23 09:35:30,004 INFO railyGateway - Update control: 200
                               2025-01-23 09:35:30,008 DEBUG railyGateway - {
                                 "identifier": 200,
                                 "coord": {
                                   "x": 2,
                                   "y": 1
                                 },
                                 "themeId": 200,
                                 "themeDimIdx": 0
                               }                         
                         */

                        var jsonControl = cmddata.GetString("argumentValue");
                        var jsonControlObject = JObject.Parse(jsonControl);

                        var ctrlIdentifier = jsonControlObject.GetString("identifier");

                        var coord = jsonControlObject["coord"] as JObject;
                        var x = coord.GetInt("x");
                        var y = coord.GetInt("y");

                        var themeId = jsonControlObject.GetInt("themeId");
                        var themeDimIdx = jsonControlObject.GetInt("themeDimIdx");

                        var planItemEditor = new PlanItemEditor
                        {
                            ThemeId = themeId,
                            ThemeDimIdx = themeDimIdx
                        };

                        if (jsonControlObject["editor"] is JObject editor)
                        {
                            planItemEditor.InnerHtml = editor.GetString("innerHtml");
                            planItemEditor.FontSize = editor.GetString("fontSize");
                        }

                        var resRemove = userWorkspace.Metamodel.Planfield.Remove(x, y);
                        var planItem = new PlanItem
                        {
                            Coord = new PlanItemCoord { X = x, Y = y },
                            Editor = planItemEditor,
                            Identifier = ctrlIdentifier
                        };
                        var resAdd = userWorkspace.Metamodel.Planfield.TryAdd($"{x}x{y}", planItem);

                        if (resRemove && resAdd)
                            await userWorkspace.Metamodel.Planfield.Save();
                    }
                    return true;

                case "trackplan" when cmddata.GetString("argument").Equals("moveControl"):
                    {
                        var moveArg = cmddata?["argumentValue"] as JObject;
                        if (moveArg == null)
                        {
                            Logging.Log.Debug("Invalid move argument.");
                            return false;
                        }

                        var controlIdentifier = moveArg?.GetString("id");
                        if (string.IsNullOrEmpty(controlIdentifier))
                        {
                            Logging.Log.Debug("Control remove does not work without control identifier.");
                            return false;
                        }

                        var ctrl = userWorkspace.Metamodel.Planfield.Get(controlIdentifier);
                        if (ctrl == null) return true;

                        var currentCoord = moveArg?["currentCoord"] as JObject;
                        var currentX = currentCoord.GetInt("x");
                        var currentY = currentCoord.GetInt("y");
                        var resRemove = userWorkspace.Metamodel.Planfield.Remove(currentX, currentY);

                        var targetCoord = moveArg?["targetCoord"] as JObject;
                        var targetX = targetCoord.GetInt("x");
                        var targetY = targetCoord.GetInt("y");
                        ctrl.Coord = new PlanItemCoord { X = targetX, Y = targetY };
                        var resAdd = userWorkspace.Metamodel.Planfield.TryAdd($"{targetX}x{targetY}", ctrl);
                        if (resRemove && resAdd)
                            await userWorkspace.Metamodel.Planfield.Save();
                    }
                    return true;

                case "trackplan" when cmddata.GetString("argument").Equals("removeControl"):
                    {
                        var controlIdentifier = cmddata.GetString("argumentValue");
                        if (string.IsNullOrEmpty(controlIdentifier))
                        {
                            Logging.Log.Debug("Control remove does not work without control identifier.");
                            return false;
                        }

                        var ctrl = userWorkspace.Metamodel.Planfield.Get(controlIdentifier);
                        if (ctrl == null) return true;

                        var isBlock = ctrl.IsBlock;
                        var isStage = ctrl.IsStage;
                        var isSensor = ctrl.IsSensor;
                        var isAccessory = ctrl.IsDecoupler || ctrl.IsSignal || ctrl.IsSwitch;

                        var resRemove = userWorkspace.Metamodel.Planfield.Remove(ctrl.Coord.X, ctrl.Coord.Y);
                        if (resRemove)
                        {
                            await PgHelper.SendDebugToClient(uid, $"{controlIdentifier} removed");

                            //
                            // TODO all parts where the identifier is used must be cleaned
                            // routes, accessories, additional parts should party be reseted
                            //

                            // Sample for "Block_3":
                            // (a) remove all routes with specific block
                            //     - search in `routes.json` for entries like `"name": "Block_1[+]_Block_3[+]"`
                            // (b) remove all entries in `settings.json`
                            //     - "routes"::"Name": "Block_1[-]_Block_3[-]"
                            //     - "blockSensors"::"identifier": "Block_3[+]",
                            // (c) reset block identifier in locomotive area, file `settings.json`
                            //     - locomotives::"assignedToBlock": "Block_3",
                            // (d) when we remove a stage, the settings for the stage must be removed from settings
                            //

                            if (isBlock || isStage)
                            {
                                var blockIdPlus = $"{ctrl.Identifier}[+]";
                                var blockIsMinus = $"{ctrl.Identifier}[-]";

                                #region (a)

                                var metaRoutes = userWorkspace.Metamodel.Routes;
                                var routeList = metaRoutes.ToObject<libTrackplan.Route.RouteList>();
                                if (routeList != null)
                                {
                                    var routes = routeList.GetAllByNames([blockIdPlus, blockIsMinus]);

                                    foreach (var itRoute in routes)
                                        routeList.Remove(itRoute);

                                    // check if any route was found priously, if not update will be ok, but change was needed
                                    if (routes.Count > 0)
                                    {
                                        var resApply = await userWorkspace.Metamodel.ApplyRoutes(routeList);
                                        if (!resApply && routes.Count > 0)
                                        {
                                            Logging.Log.Debug($"Remove of additional routes failed.");
                                        }
                                        else
                                        {
                                            var jsonData = new JObject
                                            {
                                                ["command"] = "update",
                                                ["routes"] = userWorkspace.Metamodel.Routes,
                                            };

                                            await PgHelper.SendToClient(requestor, jsonData.ToString(Formatting.None));
                                        }
                                    }
                                }

                                #endregion

                                #region (b)

                                var settings = userWorkspace.Metamodel.Settings;

                                var routesCopy = settings.Routes.ToList();
                                foreach (var it in routesCopy)
                                {
                                    if (it.Key.Name.StartsWith(blockIdPlus)
                                        || it.Key.Name.StartsWith(blockIsMinus)
                                        || it.Key.Name.EndsWith(blockIdPlus)
                                        || it.Key.Name.EndsWith(blockIsMinus))
                                    {
                                        settings.Routes.TryRemove(it.Key, out _);
                                    }
                                }

                                var blockSensorsCopy = settings.BlockSensors.ToList();
                                foreach (var it in blockSensorsCopy)
                                {
                                    if (it.Key.Identifier.Equals(blockIdPlus))
                                        settings.BlockSensors.TryRemove(it.Key, out _);
                                    if (it.Key.Identifier.Equals(blockIsMinus))
                                        settings.BlockSensors.TryRemove(it.Key, out _);
                                }

                                #endregion

                                #region (c)

                                var locomotivesCopy = settings.Locomotives.ToList();
                                foreach (var it in locomotivesCopy)
                                {
                                    if (isBlock)
                                    {
                                        if (it.Key.AssignedToBlock.Equals(ctrl.Identifier))
                                            settings.Locomotives.TryRemove(it.Key, out _);
                                    }
                                    else if (isStage)
                                    {
                                        if (it.Key.AssignedToBlock.StartsWith(ctrl.Identifier))
                                            settings.Locomotives.TryRemove(it.Key, out _);
                                    }
                                }

                                #endregion

                                #region (d)

                                if (isStage)
                                {
                                    settings.RemoveStaging(ctrl.Identifier);
                                }

                                #endregion
                            }
                            else if (isSensor)
                            {
                                var settings = userWorkspace.Metamodel.Settings;

                                foreach (var it in settings.BlockSensors)
                                {
                                    if (it.Key.SensorEnter.Equals(ctrl.Identifier)) it.Key.SensorEnter = string.Empty;
                                    if (it.Key.SensorIn.Equals(ctrl.Identifier)) it.Key.SensorIn = string.Empty;
                                }

                                var sensorsCopy = settings.Sensors.ToList();
                                foreach (var it in sensorsCopy)
                                {
                                    if (it.Key.Name.Equals(ctrl.Identifier))
                                        settings.Sensors.TryRemove(it.Key, out _);
                                }
                            }
                            else if (isAccessory)
                            {
                                var settings = userWorkspace.Metamodel.Settings;

                                var accCopy = settings.Accessories.ToList();
                                foreach (var it in accCopy)
                                {
                                    if (it.Key.PlanfieldControlIdentifier.Equals(ctrl.Identifier))
                                        settings.Accessories.TryRemove(it.Key, out _);
                                }
                            }

                            await userWorkspace.Metamodel.Settings.Save();
                            await userWorkspace.Metamodel.Planfield.Save();
                        }
                    }
                    return true;

                case "trackplan" when cmddata.GetString("argument").Equals("assigment"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var controlIdentifier = argumentValue?.GetString("controlId");
                        var connectorId = argumentValue?.GetInt("connectorId") ?? 1;

                        if (string.IsNullOrEmpty(controlIdentifier))
                        {
                            Logging.Log.Debug("Control remove does not work without control identifier.");
                            return false;
                        }

                        var ctrl = userWorkspace.Metamodel.Planfield.Get(controlIdentifier);
                        if (ctrl == null) return false;

                        var isConnector = ctrl.IsConnector;
                        if (isConnector)
                        {
                            if (ctrl.Editor != null)
                                ctrl.Editor.ConnectorId = connectorId;
                        }

                        await userWorkspace.Metamodel.Planfield.Save();
                    }
                    return true;

                case "trackplan" when PgHelper.IsPayloadCommand(cmddata, "itemClicked"):
                    {
                        // Ob die ECoS in GO ist nur prüfen wenn wir uns NICHT im Simulationsmodus befinden!
                        if (!userWorkspace.SimulationEnabled)
                        {
                            var dps2 = DataProviderManager.GetDataProviders(uid);
                            if (!dps2.IsEcosInGo(out var message))
                            {
                                var smsg = message?.ToString(Formatting.None);
                                if (!string.IsNullOrEmpty(smsg))
                                    await PgHelper.SendToClient(requestor, message.ToString(Formatting.None));
                            }
                        }

                        var argument = cmddata.GetString("argument");
                        switch (argument)
                        {
                            case "execute":
                                {
                                    var argumentValue = cmddata["argumentValue"] as JObject;
                                    var ctrlIdentifier = argumentValue.GetString("ctrlIdentifier");
                                    //var coordX = argumentValue.GetInt("x");
                                    //var coordY = argumentValue.GetInt("y");

                                    var settings = userWorkspace.Metamodel.Settings;

                                    //
                                    // check if click was triggered on Sensor
                                    //
                                    var sensorInfo = settings.Sensors.FirstOrDefault(it => it.Key.Name.Equals(ctrlIdentifier)).Key;
                                    if (sensorInfo != null)
                                    {
                                        if (!userWorkspace.SimulationEnabled) return false;

                                        //
                                        // when we are in simulation mode, clicks on sensors 
                                        // are planned to be routed to the s88 simulation data provider
                                        //

                                        // sensorInfo.Provider is irrelevant at this point, we force simulation provider
                                        var dps = DataProviderManager.GetDataProvider(uid, DataProviderType.S88FeedbackSimulator);
                                        var dp = dps.FirstOrDefault() ?? EmptyDataProvider.Empty;
                                        if (dp is IDataProviderSimulator dpSimulator)
                                        {
                                            // TODO Module:Pin Handling fehlt!

                                            if (int.TryParse(sensorInfo.Address, out var pin0) && pin0 >= 1)
                                            {
                                                var res = S88Math.HandleInput(pin0);
                                                dpSimulator.ToggleState(res.OutputPort, res.OutputPin);
                                            }
                                        }
                                    }

                                    //
                                    // check if click was triggered on Accessory (e.g. Switch, Signal, etc.)
                                    //
                                    var accInfo = settings.Accessories.FirstOrDefault(it => it.Key.PlanfieldControlIdentifier.Equals(ctrlIdentifier)).Key;
                                    if (accInfo != null)
                                    {
                                        //
                                        // get DataProvider
                                        //
                                        var dpAcc = DataProviderManager.GetDataProviderByName(uid, accInfo.AccessoryDriver);
                                        if (dpAcc != null)
                                        {
                                            IReadOnlyCollection<IEntity> entities = null;
                                            if (dpAcc is libZ21.DataProvider.DataProvider dpZ21Acc)
                                            {
                                                entities = dpZ21Acc.AccessoriesEntities as IReadOnlyCollection<IEntity>;
                                            }
                                            else
                                            {
                                                entities = dpAcc.Entities as IReadOnlyCollection<IEntity>;
                                            }
                                            var accEntity = entities?.FirstOrDefault(it =>
                                                it.Type == EntityType.Accessory
                                                && it.Name0.Equals(accInfo.AccessoryIdentifier, StringComparison.OrdinalIgnoreCase));

                                            if (accEntity != null && accEntity is IAccessory acc)
                                            {
                                                var newState = 0;

                                                //
                                                // Invertierung von Schaltartikeln funktioniert nur bei einfachen Weichen/Lampen/etc.
                                                //
                                                if (acc.Gates == 2)
                                                {
                                                    if (dpAcc is IEntityInverter dbInverter)
                                                    {
                                                        var doCommandInvert = accInfo.Invert;
                                                        if (doCommandInvert)
                                                        {
                                                            newState = dbInverter.InvertAccessoryState(accEntity);
                                                        }
                                                        else
                                                        {
                                                            newState = dbInverter.ChangeAccessoryState(accEntity);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        // fallback behaviour when dataprovider does not support invert
                                                        var currentState = acc.GetStateIndex();
                                                        newState = currentState == 0 ? 1 : 0;
                                                    }
                                                }
                                                else
                                                {
                                                    var maxStateIdx = acc.Gates;
                                                    if (int.TryParse(acc.State, out var currentState))
                                                    {
                                                        newState = currentState + 1;
                                                        if (newState >= maxStateIdx)
                                                            newState = 0;
                                                    }
                                                }

                                                string debugMsg;
                                                if (acc.AddrExt != null && acc.AddrExt.Count > 0 && acc.AddrExt.Count >= newState)
                                                {
                                                    debugMsg = $"Entity: {accEntity.DisplayName} -> {accInfo.AccessoryDriver}::{accEntity.ObjectId}, Change to {acc.AddrExt[newState]}";
                                                }
                                                else
                                                {
                                                    debugMsg = $"Entity: {accEntity.DisplayName} -> {accInfo.AccessoryDriver}::{accEntity.ObjectId}, Change to state index: {newState}";
                                                }

                                                Logging.Log.Info(debugMsg);
                                                dataExchange?.QueueDebugMessage(userWorkspace.Uid, debugMsg);

                                                //
                                                // ecos, z21
                                                //
                                                if (dpAcc.Type.HasFlag(DataProviderType.ECoS50210)
                                                    || dpAcc.Type.HasFlag(DataProviderType.Z21))
                                                {
                                                    var commandStationCommand = new JObject
                                                            {
                                                                {"driverName", accInfo.AccessoryDriver},
                                                                {"objectId", accEntity.ObjectId},
                                                                {"command", "update"},
                                                                {"argument", "targetState"},
                                                                {"argumentValue", newState}
                                                            };

                                                    await PgHelper.SendToGateway(uid, commandStationCommand);

                                                    return false;
                                                }

                                                //
                                                // Demo
                                                //
                                                if (dpAcc.Type.HasFlag(DataProviderType.Demo))
                                                {
                                                    if (accEntity is Accessory accDemo)
                                                        return await PgDemo.HandleAccessory(accDemo, newState, requestor);
                                                }
                                            }
                                        }

                                    }
                                }
                                break;
                        }
                    }
                    break;
            }

            return false;
        }
    }
}

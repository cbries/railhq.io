// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus;
using libEsuEcos.Blocks;
using libMetamodel.Settings;
using libShared;
using libShared.DataProvider;
using libShared.Entities;
using libUserspace;
using libUtilities;
using libZ21;
using Newtonsoft.Json.Linq;
using railyWebApp.DataProvider;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using ILocomotive = libMetamodel.Settings.ILocomotive;
using Locomotive = libEsuEcos.Entities.Locomotive;
// ReSharper disable RedundantAssignment

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace railyWebApp.Playground
{
    public class PgAutoMode
    {
        private static async Task<bool> UpdateBlock(
            Workspace userWorkspace,
            string identifier,
            bool? isLocked,
            bool? isEnabled,
            bool? isCommuterAllowedPlus,
            bool? isCommuterAllowedMinus)
        {
            var settings = userWorkspace.Metamodel.Settings;
            if (settings == null) return false;

            var block = settings.FindBlockByName(identifier);
            IStaging stage = null;
            if (block == null)
                stage = settings.FindStagingByName(identifier);

            //
            // no block / stage - create new...
            //
            if (block == null && stage == null && identifier.StartsWith("Block", StringComparison.OrdinalIgnoreCase))
            {
                block = new Block { Identifier = identifier };
                settings.BlockSensors.TryAdd((Block)block, false);
            }

            if (isLocked != null)
            {
                if (block != null)
                    block.IsLocked = isLocked.Value;
                else if (stage != null)
                    stage.IsLocked = isLocked.Value;

                return true;
            }

            if (isEnabled != null)
            {
                if (block != null)
                    block.IsEnabled = isEnabled.Value;
                else if (stage != null)
                    stage.IsEnabled = isEnabled.Value;

                return true;
            }

            if (isCommuterAllowedPlus != null || isCommuterAllowedMinus != null)
            {
                if (isCommuterAllowedPlus != null && block is IBlockExtras blockExtras0)
                    blockExtras0.IsCommuterAllowedPlus = isCommuterAllowedPlus.Value;
                if (isCommuterAllowedMinus != null && block is IBlockExtras blockExtras1)
                    blockExtras1.IsCommuterAllowedMinus = isCommuterAllowedMinus.Value;

                return true;
            }

            return false;
        }

        private static async Task<bool> RepairBlock(
            Workspace userWorkspace,
            IReadOnlyList<IDataProvider> allDps,
            JObject cmddata
            )
        {

            //
            // Wir versuchen hier über bestimmte Heuristiken einen Block zu reparieren,
            // oder zumindest soweit zu bereinigen, dass dieser wieder problemlos 
            // vom Anwender genutzt werden kann. Dazu gehört u.a. das Entfernen
            // von Lokzuweisungen, falls die ControlStation dieser
            // Lokomotive nicht mehr existiert oder erreichbar ist.
            //

            var argumentValue = cmddata["argumentValue"] as JObject;
            var blockIdentifier = argumentValue.GetString("blockId");
            if (string.IsNullOrEmpty(blockIdentifier))
                return true; // missing Block-Identifizierer, es muss nichts weiter gemacht werden

            var settings = userWorkspace?.Metamodel?.Settings;
            var blockPlus = settings?.FindBlockByName(blockIdentifier + "[+]");
            var blockMinus = settings?.FindBlockByName(blockIdentifier + "[-]");
            if (blockPlus != null || blockMinus != null)
            {
                // TO BE DEFINED
                // ...
            }

            //
            // Suche alle Lokomotiven die zu diesem Block zugewiesen sind. 
            //
            var assignedLocomotives = new List<ILocomotive>();
            foreach (var it in settings?.Locomotives ??
                               new ConcurrentDictionary<libMetamodel.Settings.Locomotive, bool>())
            {
                var loc = it.Key;
                if (string.IsNullOrEmpty(loc?.AssignedToBlock)) continue;
                if (loc.AssignedToBlock.Equals(blockIdentifier, StringComparison.OrdinalIgnoreCase))
                    assignedLocomotives.Add(loc);
            }

            var invalidLocomotives = new List<ILocomotive>();
            foreach (var it in assignedLocomotives)
            {
                var driverName = it.DriverName;
                var objectId = it.ObjectId;

                var dp = allDps.FirstOrDefault(itt => itt.Name.Equals(driverName)) ?? EmptyDataProvider.Empty;
                var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                var locEntity = entities?.FirstOrDefault(itt =>
                    itt.Type == EntityType.Locomotive
                    && itt.ObjectId == objectId);
                if (locEntity == null)
                    invalidLocomotives.Add(it);
            }

            foreach (var it in invalidLocomotives)
                it.AssignedToBlock = string.Empty;

            var updated = invalidLocomotives.Count > 0;
            if (updated && settings != null)
                await settings.Save();

            return updated;
        }

        private static async Task<bool> RepairAccessory(
            Workspace userWorkspace,
            IReadOnlyList<IDataProvider> allDps,
            JObject cmddata
        )
        {
            var argumentValue = cmddata["argumentValue"] as JObject;
            var accessoryIdentifier = argumentValue.GetString("accessoryIdentifier");
            if (string.IsNullOrEmpty(accessoryIdentifier))
                return true; // missing Accessory-Identifizierer, es muss nichts weiter gemacht werden

            var settings = userWorkspace?.Metamodel?.Settings;
            var accSettings = settings?.FindAccessoriesByPlanId(accessoryIdentifier);
            if (accSettings == null || accSettings.Count == 0) return true;

            //
            // Suche alle Accessories bei denen in den Settings der Treiber (z.B. ecos)
            // nicht im System geladen ist, bzw. keine Entitäten besitzt.
            //
            var invalidAccSettings = new List<libMetamodel.Settings.IAccessory>();
            foreach (var it in accSettings)
            {
                //
                // Prüfe Treiber:
                // - wenn dieser leer ist
                // - wenn dieser keine Schaltartikel hat, also dann auch nicht existiert
                // dann ist das nicht valide.
                //
                var driverName = it.AccessoryDriver;
                if (string.IsNullOrEmpty(driverName))
                {
                    invalidAccSettings.Add(it);
                }
                else
                {
                    var dp = allDps.FirstOrDefault(itt => itt.Name.Equals(driverName)) ?? EmptyDataProvider.Empty;
                    if (dp.Entities.Count == 0)
                        invalidAccSettings.Add(it);
                }

                //
                // Prüfe ob der Schaltartikel existiert.
                //

                var dp1 = allDps.FirstOrDefault(itt => itt.Name.Equals(driverName)) ?? EmptyDataProvider.Empty;
                if (dp1 != null)
                {
                    var found = false;
                    var accId = it?.AccessoryIdentifier;
                    if (!string.IsNullOrEmpty(accId))
                    {
                        foreach (var itEntity in dp1.Entities)
                        {
                            var entity = itEntity as IEntity;
                            if (string.IsNullOrEmpty(entity?.Name0)) continue;
                            found = entity.Name0.Equals(accId, StringComparison.OrdinalIgnoreCase);

                            if (found) break;
                        }
                    }

                    if (!found)
                        invalidAccSettings.Add(it);
                }
            }

            // setze die Settings zurück
            foreach (var it in invalidAccSettings)
                it.PlanfieldControlIdentifier = string.Empty;

            var updated = invalidAccSettings.Count > 0;
            if (updated)
                await settings.Save();

            return updated;
        }

        public const string CaseAutomode = "automode";

        private static string GetCommandNameForAutoMode(string driverName)
        {
            if (string.IsNullOrEmpty(driverName)) return "update";

            //
            // `updateNoForce` is used to inform the ECoS that no 
            // request / release command have to be used; to take
            // effect the relevant entities have to be request 
            // before any subsequent speedstep call
            //
            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                return "updateNoForce";

            if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                return "update";

            return "update";
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

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres)
            {
                Logging.Log.Warn($"Workspace not loaded nor available.");
                return false;
            }

            var allDps = DataProviderManager.GetDataProviders(uid);

            switch (cmd)
            {
                case CaseAutomode when PgHelper.IsPayloadCommand(cmddata, "repair") &&
                                       PgHelper.IsArgument(cmddata, "block"):
                    return await RepairBlock(userWorkspace, allDps, cmddata);

                case CaseAutomode when PgHelper.IsPayloadCommand(cmddata, "repair") &&
                                       PgHelper.IsArgument(cmddata, "accessory"):
                    return await RepairAccessory(userWorkspace, allDps, cmddata);

                case CaseAutomode when PgHelper.IsArgument(cmddata, "isBlockEnabled"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        var state = argumentValue.GetBool("state");
                        return await UpdateBlock(userWorkspace, blockIdentifier, null, state, null, null);
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "isBlockLocked"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        var state = argumentValue.GetBool("state");
                        return await UpdateBlock(userWorkspace, blockIdentifier, state, null, null, null);
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "isCommuterAllowedPlus"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        var state = argumentValue.GetBool("state");
                        return await UpdateBlock(userWorkspace, blockIdentifier + "[+]", null, null, state, null);
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "isCommuterAllowedMinus"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        var state = argumentValue.GetBool("state");
                        return await UpdateBlock(userWorkspace, blockIdentifier + "[-]", null, null, null, state);
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "isMaintenaceEnabled"):
                    {
                        try
                        {
                            var argumentValue = cmddata["argumentValue"] as JObject;
                            var planfieldControlIdentifier = argumentValue.GetString("planfieldControlIdentifier");
                            var isMaintenance = argumentValue.GetBool("isMaintenance");
                            var settings = userWorkspace.Metamodel.Settings;
                            var settingAcc = settings.FindAccessoryByPlanId(planfieldControlIdentifier);
                            if (settingAcc == null)
                            {
                                settingAcc = new Accessory
                                {
                                    PlanfieldControlIdentifier = planfieldControlIdentifier,
                                    IsMaintenaceEnabled = isMaintenance
                                };
                                settings.Accessories.TryAdd((Accessory)settingAcc, false);
                                await settings.Save();
                                return true;
                            }

                            if (settingAcc.IsMaintenaceEnabled != isMaintenance)
                            {
                                settingAcc.IsMaintenaceEnabled = isMaintenance;
                                await settings.Save();
                                return true;
                            }
                        }
                        catch (Exception ex)
                        {
                            Logging.ExceptionLog(ex);
                        }
                    }
                    break;

                case CaseAutomode when PgHelper.IsArgument(cmddata, "simulationMode"):
                    {
                        var argumentValue = cmddata.GetBool("argumentValue");
                        var automaticRunner = userWorkspace.AutomaticRunner;
                        automaticRunner.IsSimulationMode = argumentValue;
                    }
                    break;

                case CaseAutomode when PgHelper.IsArgument(cmddata, "forcestop"):
                    {
                        Logging.Log.Info("Stop (force) automatic!");
                        var automaticRunner = userWorkspace.AutomaticRunner;
                        automaticRunner?.StopForce();
                    }

                    return true;

                case CaseAutomode when
                    PgHelper.IsArgument(cmddata, "start") ||
                    PgHelper.IsArgument(cmddata, "stop"):
                    {
                        if (PgHelper.IsArgument(cmddata, "start")) Logging.Log.Info("Start automatic...");
                        else if (PgHelper.IsArgument(cmddata, "stop")) Logging.Log.Info("Stop automatic...");

                        #region local callback functions for automatic

                        void AutomaticRunnerOnStatusUpdated(object sender, string statusMessage)
                        {
                            Logging.Log.Info($"Automatic::{statusMessage}");
                        }

                        async void AutomaticRunnerOnActionTriggered(
                            object sender,
                            ActionTriggeredData data)
                        {
                            try
                            {
                                if (data.Data == null) return;
                                var cmd0 = data.Data.Command;
                                if (string.IsNullOrEmpty(cmd0)) return;

                                var argument = data.Data.Argument;
                                if (string.IsNullOrEmpty(argument)) return;

                                switch (data.Data)
                                {
                                    case libAutomaticModus.pods.ActionPrepareSwitches aps when argument == "route":
                                        {
                                            await PgRoutes.PrepareRoute(aps.RouteName, userWorkspace, null, dataExchange);
                                        }
                                        break;

                                    case libAutomaticModus.pods.ActionPrepareSignals aps when argument == "sourceBlock":
                                        {
                                            var sourceBlock = aps.SourceBlock;
                                            var sourceSide = aps.SourceLeavingSide;
                                            await PgSignals.PrepareSignals(sourceBlock, sourceSide, userWorkspace, requestor, dataExchange);
                                            // TODO PrepareSignals returns a list of errors in case, handling?
                                        }
                                        break;

                                    case libAutomaticModus.pods.ActionRequest ar when argument == "locomotive":
                                        {
                                            var driverName = ar.DriverName;
                                            var objectId = ar.ObjectId;

                                            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier))
                                            {
                                                var gatewayCommand = new JObject
                                                    {
                                                        { "driverName", driverName },
                                                        { "objectId", objectId },
                                                        { "command", "update" },
                                                        { "argument", "request" },
                                                        {
                                                            "argumentValue", new JObject
                                                            {
                                                                { "driverName", driverName },
                                                                { "objectId", objectId }
                                                            }
                                                        }
                                                    };

                                                await PgHelper.SendToGateway(uid, gatewayCommand);
                                            }
                                        }
                                        break;

                                    case libAutomaticModus.pods.ActionRelease ar when argument == "locomotive":
                                        {
                                            var driverName = ar.DriverName;
                                            var objectId = ar.ObjectId;

                                            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier))
                                            {
                                                var gatewayCommand = new JObject
                                                {
                                                    { "driverName", driverName },
                                                    {"objectId", objectId},
                                                    {"command", "update"},
                                                    {"argument", "release"},
                                                    {"argumentValue", new JObject
                                                    {
                                                        {"driverName", driverName},
                                                        {"objectId", objectId}
                                                    }}
                                                };

                                                await PgHelper.SendToGateway(uid, gatewayCommand);
                                            }
                                        }
                                        break;

                                    case libAutomaticModus.pods.ActionSpeedstep ar:
                                        {
                                            var driverName = ar.DriverName;
                                            var objectId = ar.ObjectId;
                                            var speed = ar.Speed;

                                            //
                                            // ecos, z21
                                            //
                                            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase)
                                                || driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var dp = allDps.FirstOrDefault(it => it.Name == driverName);
                                                var locEntities = (dp.Entities as IReadOnlyCollection<IEntity>).Where(it => it.Type == EntityType.Locomotive);
                                                var locEntity = locEntities.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId) as libShared.Entities.ILocomotive;

                                                var gatewayCommand = new JObject
                                                {
                                                    {"driverName", driverName},
                                                    {"objectId", objectId},
                                                    {"command", GetCommandNameForAutoMode(driverName)},
                                                    {"argument", "speedstep"},
                                                    {
                                                        "argumentValue", new JObject
                                                        {
                                                            { "speed", speed },
                                                            { "maxSpeedSteps", locEntity.Protocol },
                                                            { "direction", (int)locEntity.Direction }
                                                        }
                                                    }
                                                };

                                                await PgHelper.SendToGateway(uid, gatewayCommand);
                                            }
                                            else
                                            // 
                                            // demo
                                            //
                                            if (driverName.Equals(DataProviderDemo.ProviderName))
                                            {
                                                var locEntity = GetDpDemoEntity(uid, objectId) as Locomotive;
                                                if (locEntity != null)
                                                {
                                                    var s = $"<EVENT {objectId}>\n{objectId} speed[{speed}]\n{objectId} speedstep[{speed}]\n<END 0 (OK)>\n";
                                                    var eventBlock = new EventBlock();
                                                    eventBlock.Parse(s);
                                                    if (locEntity.ParseData(eventBlock))
                                                    {
                                                        var entity = PgDemo.__getObject(locEntity);
                                                        var entityData = new JObject
                                                        {
                                                            ["command"] = "update",
                                                            ["entityType"] = "locomotive",
                                                            ["entityData"] = entity
                                                        };

                                                        dataExchange?.SendObjectToAllClients(uid, entityData);
                                                    }
                                                }
                                            }

                                        }
                                        break;

                                    case libAutomaticModus.pods.ActionDirection ar:
                                        {
                                            var driverName = ar.DriverName;
                                            var objectId = ar.ObjectId;
                                            var direction = ar.Direction;

                                            //
                                            // ecos
                                            //
                                            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var gatewayCommand = new JObject
                                                {
                                                    {"driverName", driverName},
                                                    {"objectId", objectId},
                                                    {"command", GetCommandNameForAutoMode(driverName)},
                                                    {"argument", "direction"},
                                                    {"argumentValue", direction}
                                                };

                                                await PgHelper.SendToGateway(uid, gatewayCommand);
                                            }
                                            //
                                            // z21
                                            //
                                            else if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                                            {
                                                var dpZ21 = DataProviderManager.GetDataProvider(uid, DataProviderType.Z21).FirstOrDefault() as libZ21.DataProvider.DataProvider;
                                                var locEntitiesZ21 = dpZ21?.LocomotivesEntities as IReadOnlyCollection<IEntity>;
                                                var locEntity = locEntitiesZ21?.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId) as libShared.Entities.ILocomotive;

                                                if (locEntity != null)
                                                {
                                                    var gatewayCommand = new JObject
                                                    {
                                                        { "driverName", driverName },
                                                        { "objectId", objectId },
                                                        { "command", GetCommandNameForAutoMode(driverName) },
                                                        { "argument", "direction" },
                                                        {
                                                            "argumentValue", new JObject
                                                            {
                                                                { "speed", 0 },
                                                                { "maxSpeedSteps", locEntity.Protocol },
                                                                { "direction", direction }
                                                            }
                                                        }
                                                    };

                                                    await PgHelper.SendToGateway(uid, gatewayCommand);
                                                }
                                            }
                                            // 
                                            // demo
                                            //
                                            else if(driverName.Equals(DataProviderDemo.ProviderName))
                                            {
                                                if (GetDpDemoEntity(uid, objectId) is Locomotive locEntity)
                                                {
                                                    /*
                                                       <EVENT 1034>
                                                       1034 dir[1]
                                                       <END 0 (OK)>
                                                     */
                                                    var s = $"<EVENT {objectId}>\n{objectId} dir[{direction}]\n<END 0 (OK)>\n";
                                                    var eventBlock = new EventBlock();
                                                    eventBlock.Parse(s);
                                                    if (locEntity.ParseData(eventBlock))
                                                    {
                                                        var entity = PgDemo.__getObject(locEntity);
                                                        var entityData = new JObject
                                                        {
                                                            ["command"] = "update",
                                                            ["entityType"] = "locomotive",
                                                            ["entityData"] = entity
                                                        };

                                                        dataExchange?.SendObjectToAllClients(uid, entityData);
                                                    }
                                                }
                                            }

                                        }
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                Logging.ExceptionLog(ex);
                            }
                        }

                        #endregion

                        var automaticRunner = userWorkspace.AutomaticRunner;

                        if (automaticRunner.IsStarted())
                        {
                            //
                            // stop
                            //
                            automaticRunner.Stop();
                            automaticRunner.StatusUpdated -= AutomaticRunnerOnStatusUpdated;
                            automaticRunner.ActionTriggered -= AutomaticRunnerOnActionTriggered;
                        }
                        else
                        {
                            //
                            // check of no previous run is still finalizing
                            //
                            if (automaticRunner != null && automaticRunner.RunningRoutes > 0)
                            {
                                var n = automaticRunner.RunningRoutes;
                                var s = n == 1 ? " a single route" : $" {n} routes";
                                var m = $"A previous auto mode run is still finalizing {s}.";
                                dataExchange?.QueueDebugMessage(userWorkspace.Uid, m, DebugMessageT.Routes);
                                dataExchange?.SendWarning(requestor, m);
                                return false;
                            }

                            // if not previous run is finalizing, we can start 

                            //var allDps = DataProviderManager.GetDataProviders(uid);
                            var metaRoutes = userWorkspace.Metamodel.Routes;
                            var routeList = metaRoutes.ToObject<libTrackplan.Route.RouteList>();
                            var settings = userWorkspace.Metamodel.Settings;
                            var planfield = userWorkspace.Metamodel.Planfield;

                            // 
                            // start
                            //
                            var automaticData = new AutomaticData(dataExchange, uid, allDps, routeList, settings, planfield);
                            automaticRunner.StatusUpdated += AutomaticRunnerOnStatusUpdated;
                            automaticRunner.ActionTriggered += AutomaticRunnerOnActionTriggered;
                            automaticRunner.Start(automaticData);
                        }
                    }

                    return true;

                case CaseAutomode when PgHelper.IsArgument(cmddata, "removeLocomotiveFromBlock"):
                    {
                        Logging.Log.Debug("Remove locomotive from block!");

                        var argumentObject = cmddata["argumentValue"] as JObject;
                        if (argumentObject == null)
                        {
                            Logging.Log.Debug("Incorrect command to assign locomotive to block.");
                            return false;
                        }

                        var driverName = argumentObject.GetString("driverName");
                        var objectId = argumentObject.GetInt("objectId", -1);

                        Logging.Log.Debug($"Remove {driverName}::{objectId} from any assignment.");

                        var settings = userWorkspace.Metamodel.Settings;
                        var updated = settings.RemoveLocomotiveAssignment(driverName, objectId);
                        if (updated)
                            await settings.Save();

                        return true;
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "assignLocomotiveToBlock"):
                    {
                        Logging.Log.Debug("Assign locomotive to block!");

                        var argumentObject = cmddata["argumentValue"] as JObject;
                        if (argumentObject == null)
                        {
                            Logging.Log.Debug("Incorrect command to assign locomotive to block.");
                            return false;
                        }

                        var driverName = argumentObject.GetString("driverName");
                        var objectId = argumentObject.GetInt("objectId", -1);
                        var blockName = argumentObject.GetString("blockName");

                        //var allDps = DataProviderManager.GetDataProviders(uid);
                        var dp = allDps.FirstOrDefault(it => it.Name.Equals(driverName)) ?? EmptyDataProvider.Empty;
                        var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                        var locEntity = entities?.FirstOrDefault(it =>
                            it.Type == EntityType.Locomotive
                            && it.ObjectId == objectId);

                        Logging.Log.Debug($"Assign {driverName}::{objectId} to {blockName}.");

                        var settings = userWorkspace.Metamodel.Settings;
                        var updated = settings.AssignLocomotiveToBlock(driverName, objectId, blockName, locEntity);
                        if (updated)
                            await settings.Save();

                        return true;
                    }

                case CaseAutomode when PgHelper.IsArgument(cmddata, "sendLocomotiveToBlock"):
                    {
                        Logging.Log.Debug("Send locomotive to target block!");

                        var argumentObject = cmddata["argumentValue"] as JObject;
                        if (argumentObject == null)
                        {
                            Logging.Log.Debug("Incorrect command to assign locomotive to block.");
                            return false;
                        }

                        var driverName = argumentObject.GetString("driverName");
                        var objectId = argumentObject.GetInt("objectId", -1);
                        var startBlock = argumentObject.GetString("startBlock");
                        var targetBlock = argumentObject.GetString("targetBlock");

                        Logging.Log.Debug($"Send {driverName}::{objectId} from {startBlock} to {targetBlock}.");

                        // TODO will be stored in-memory during enabled automatic mode
                        // we will implement this approach because when the customer
                        // stops and leaves its control unit he can forget what he 
                        // planned to do; after restart the planned target is declined

                        return true;
                    }

                case CaseAutomode:
                    {
                        var command = cmddata.GetString("command");
                        var argument = cmddata.GetString("argument");
                        var argumentValue = cmddata["argumentValue"] as JObject;

                        var driverName = argumentValue.GetString("driverName");
                        var objectId = argumentValue.GetInt("objectId");

                        var settings = userWorkspace.Metamodel.Settings;
                        var settingsLoc = settings.FindLocomotiveBy(driverName, objectId);

                        bool changed = false;

                        switch (argument)
                        {
                            case "startLocomotive":
                                {
                                    changed = true;
                                    settingsLoc.IsEnabled = true;
                                    settingsLoc.EarlistTimeForNextTrip = DateTime.MinValue;
                                }
                                break;

                            case "finalizeLocomotive":
                                {
                                    changed = true;
                                    settingsLoc.IsEnabled = false;
                                }
                                break;

                            case "setEnterSidePlus":
                            case "setEnterSideMinus":
                                {
                                    changed = true;
                                    if (argument.Equals("setEnterSidePlus"))
                                        settingsLoc.EnterSide = LocomotiveEnterSide.Plus;
                                    else if (argument.Equals("setEnterSideMinus"))
                                        settingsLoc.EnterSide = LocomotiveEnterSide.Minus;
                                }
                                break;

                            case "changeOrientation":
                                {
                                    changed = true;
                                    if (string.IsNullOrEmpty(settingsLoc.Orientation))
                                        settingsLoc.Orientation = "right";
                                    else if (settingsLoc.Orientation.Equals("right", StringComparison.OrdinalIgnoreCase))
                                        settingsLoc.Orientation = "left";
                                    else if (settingsLoc.Orientation.Equals("left", StringComparison.OrdinalIgnoreCase))
                                        settingsLoc.Orientation = "right";
                                    else
                                        settingsLoc.Orientation = "right";
                                }
                                break;

                            case "isLockedLocomotive":
                                {
                                    changed = true;
                                    var targetState = argumentValue.GetBool("state");
                                    settingsLoc.IsLocked = targetState;
                                }
                                break;

                            default:
                                {
                                    Logging.Log.Info($"<TODO::Settings> {command} -> {argument}:={argumentValue}");
                                }
                                return false;
                        }

                        // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                        if (changed)
                            await settings.Save();

                        return true;
                    }
            }

            return false;
        }

        private static IEntityBase GetDpDemoEntity(string uid, int objectId)
        {
            var dpDemo = DataProviderManager.GetDataProvider(uid, DataProviderType.Demo).FirstOrDefault();
            var entities = dpDemo?.Entities as IReadOnlyCollection<IEntity>;
            return entities?.FirstOrDefault(it => it.ObjectId == objectId);
        }
    }
}

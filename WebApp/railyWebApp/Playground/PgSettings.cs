// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libMetamodel.Settings;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libUtilities;
using Newtonsoft.Json.Linq;
using railyWebApp.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace railyWebApp.Playground
{
    public class PgSettings
    {
        internal static async Task<bool> HandleWebClientRequests(string uid, Request request)
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
                case "setting" when PgHelper.IsArgument(cmddata, "debugging"):
                    {
                        var debugging = userWorkspace.Metamodel.Settings?.Debugging;
                        if (debugging == null && userWorkspace.Metamodel.Settings != null)
                            userWorkspace.Metamodel.Settings.Debugging = new Debugging();
                        debugging = userWorkspace.Metamodel.Settings?.Debugging;
                        var debuggingOptions = cmddata.GetValue("argumentValue") as JObject;
                        if (debugging != null && debuggingOptions != null)
                        {
                            debugging.Runtime = debuggingOptions.GetBool("runtime");
                            debugging.Accessories = debuggingOptions.GetBool("accessories");
                            debugging.Locomotives = debuggingOptions.GetBool("locomotives");
                            debugging.Routes = debuggingOptions.GetBool("routes");
                            debugging.Exceptions = debuggingOptions.GetBool("exceptions");

                            await userWorkspace.Metamodel?.Settings?.Save()!;

                            return true;
                        }
                    }
                    return false;

                case "setting" when PgHelper.IsArgument(cmddata, "screenshot"):
                    {
                        var command = cmddata.GetString("command");
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;
                        if (argumentValue == null)
                        {
                            Logging.Log.Debug($"Incorrect format 'setting'::{command}.");
                            return false;
                        }

                        //var workspaceName = argumentValue.GetString("workspaceName");
                        var imageData = argumentValue.GetString("imageData");

                        try
                        {
                            var res = await userWorkspace.SaveScreenshot(imageData);
                            if (!res)
                            {
                                Logging.Log.Info($"Serialization of screenshot failed.");
                                return false;
                            }
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                    return true;

                case "setting" when PgHelper.IsArgument(cmddata, "blockExtras"):
                    {
                        var command = cmddata.GetString("command");
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;
                        if (argumentValue == null)
                        {
                            Logging.Log.Debug($"Incorrect format 'setting'::{command}.");
                            return false;
                        }

                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        int? blockLength = argumentValue.GetInt("length");
                        if (blockLength == 0) blockLength = null;

                        int? blockStartDelay = argumentValue.GetInt("startDelay");
                        if (blockStartDelay == 0) blockStartDelay = null;

                        int? blockSignalsToRedDelay = argumentValue.GetInt("signalsToRedDelay");
                        if (blockSignalsToRedDelay == 0) blockSignalsToRedDelay = 15;

                        var settings = userWorkspace.Metamodel.Settings;
                        var updatedPlus = settings.UpdateBlockExtras(blockIdentifier + "[+]", blockLength, blockStartDelay, blockSignalsToRedDelay);
                        var updatedMinus = settings.UpdateBlockExtras(blockIdentifier + "[-]", blockLength, blockStartDelay, blockSignalsToRedDelay);
                        if (updatedPlus || updatedMinus)
                            await settings.Save();

                        return updatedPlus || updatedMinus;
                    }

                case "setting" when PgHelper.IsArgument(cmddata, "block"):
                    {
                        var command = cmddata.GetString("command");
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;
                        if (argumentValue == null)
                        {
                            Logging.Log.Debug($"Incorrect format 'setting'::{command}.");
                            return false;
                        }

                        /*
                           update -> block:={
                             "blockIdentifier": "Block_0[+]",
                             "sensorEnter": "Sensor_3",
                             "sensorIn": "Sensor_1"
                           }
                         */

                        var blockIdentifier = argumentValue.GetString("blockIdentifier");
                        var sensorEnter = argumentValue.GetString("sensorEnter");
                        var sensorIn = argumentValue.GetString("sensorIn");
                        var signal = argumentValue.GetString("signal");
                        var vorsignal = argumentValue.GetString("vorsignal");

                        var settings = userWorkspace.Metamodel.Settings;
                        var updated = settings.UpdateBlock(blockIdentifier, sensorEnter, sensorIn, signal, vorsignal);
                        if (updated)
                            await settings.Save();

                        return updated;
                    }

                case "setting" when PgHelper.IsArgument(cmddata, "sensorAddress"):
                    {
                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var name = argumentValue?.GetString("name");
                        var provider = argumentValue?.GetString("provider");
                        var address = argumentValue?.GetString("address");

                        var changed = false;
                        var sensorData = userWorkspace.Metamodel.Settings.FindSensorByName(name);
                        if (sensorData == null)
                        {
                            var r0 = userWorkspace.Metamodel.Settings.UpdateSensor(name, provider);
                            var r1 = userWorkspace.Metamodel.Settings.UpdateSensorAddress(name, $"{address}");
                            changed = r0 || r1;
                        }
                        else
                        {
                            if (!sensorData.Provider.Equals(provider))
                            {
                                sensorData.Provider = provider;
                                changed = true;
                            }

                            if (!sensorData.Address.Equals(address, StringComparison.OrdinalIgnoreCase))
                            {
                                sensorData.Address = address;
                                changed = true;
                            }
                        }

                        if (changed)
                            await userWorkspace.Metamodel.Settings.Save();

                        return changed;
                    }

                case "setting" when PgHelper.IsPayloadCommand(cmddata, "routeDisabled"):
                    {
                        /*
                           routeDisabled -> Block_1[-]_Block_2[-]:=True
                         */

                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var routeName = argumentValue.GetString("name");
                        var routeUid = argumentValue.GetString("uid");
                        var isDisabled = argumentValue.GetBool("state");

                        var settings = userWorkspace.Metamodel.Settings;
                        var updated = settings.SetRouteDisable(routeName, routeUid, isDisabled);
                        if (updated)
                            await settings.Save();
                    }
                    return true;

                case "setting" when PgHelper.IsArgument(cmddata, "accessory"):
                    {
                        /*
                           update -> accessory:={
                             "accessoryDriver": "ecos",
                             "accessoryIdentifier": "O1_10",
                             "planfieldControlIdentifier": "Switch_1"
                           }
                         */

                        //var command = cmddata.GetString("command");
                        //var argument = cmddata.GetString("accessoryIdentifier");
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;
                        if (argumentValue == null)
                        {
                            Logging.Log.Debug($"Incorrect format 'setting'::{cmddata}.");
                            return false;
                        }

                        var accessoryDriver = argumentValue.GetString("accessoryDriver");
                        var accessoryIdentifier = argumentValue.GetString("accessoryIdentifier");
                        var planfieldControlIdentifier = argumentValue.GetString("planfieldControlIdentifier");

                        var settings = userWorkspace.Metamodel.Settings;
                        var updated = settings.SetAccessoryMapping(accessoryDriver, accessoryIdentifier, planfieldControlIdentifier);
                        if (updated)
                            await settings.Save();
                    }
                    return true;

                case "setting" when PgHelper.IsArgument(cmddata, "accessoryAddress"):
                    {
                        /*
                           planfieldControlIdentifier: ctrlId,
                           provider: this.record.accessoryDriver.text,
                           address: this.record.accessoryIdentifier.text
                         */

                        var argumentValue = cmddata["argumentValue"] as JObject;
                        var planfieldControlIdentifier = argumentValue.GetString("planfieldControlIdentifier");
                        var provider = argumentValue.GetString("provider");
                        var address = argumentValue.GetString("address");
                        var invert = argumentValue.GetBool("invert");
                        var invertUi = argumentValue.GetBool("invertUi");

                        var changed = false;

                        var accData = userWorkspace.Metamodel.Settings.FindAccessoryByPlanId(planfieldControlIdentifier);
                        if (accData != null)
                        {
                            if (!accData.AccessoryDriver.Equals(provider))
                            {
                                accData.AccessoryDriver = provider;
                                changed = true;
                            }

                            if (!accData.AccessoryIdentifier.Equals(address))
                            {
                                accData.AccessoryIdentifier = address;
                                changed = true;
                            }

                            if (accData.Invert != invert)
                            {
                                accData.Invert = invert;
                                changed = true;
                            }

                            if (accData.InvertUi != invertUi)
                            {
                                accData.InvertUi = invertUi;
                                changed = true;
                            }
                        }
                        else
                        {
                            userWorkspace.Metamodel.Settings.SetAccessoryMapping(
                                provider,
                                address,
                                planfieldControlIdentifier);

                            accData = userWorkspace.Metamodel.Settings.FindAccessoryByPlanId(planfieldControlIdentifier);

                            if (accData != null)
                            {
                                accData.Invert = invert;
                                accData.InvertUi = invertUi;
                            }

                            changed = true;
                        }

                        if (changed)
                            await userWorkspace.Metamodel.Settings.Save();

                        return changed;
                    }

                case "setting" when PgHelper.IsArgument(cmddata, "staging"):
                    {
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;
                        var stagingId = argumentValue?.GetString("stagingId") ?? string.Empty;
                        if (argumentValue?["rows"] is not JArray rows) return true;
                        var settings = userWorkspace.Metamodel.Settings;
                        var assignedAnyLocomotive = false;

                        var listOfBlocks = new List<libMetamodel.Settings.Block>();

                        for (var idx = 0; idx < rows.Count; ++idx)
                        {
                            var itRow = rows[idx];

                            var stagingStepId = $"{stagingId}_{idx}";

                            #region part for assigning locomotive entity

                            var driverName = itRow.GetString("driverName");
                            var objectId = itRow.GetInt("objectId", -1);

                            if (objectId > 0)
                            {
                                var allDps = DataProviderManager.GetDataProviders(uid);
                                var dp = allDps.FirstOrDefault(it => it.Name.Equals(driverName)) ?? EmptyDataProvider.Empty;
                                var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                                var locEntity = entities?.FirstOrDefault(it => 
                                    it.Type == EntityType.Locomotive
                                    && it.ObjectId == objectId);

                                var r = settings.AssignLocomotiveToBlock(driverName, objectId, stagingStepId, locEntity);
                                if (r) assignedAnyLocomotive = true;
                                var locSettings = settings.FindLocomotiveBy(driverName, objectId);
                                if (locSettings != null)
                                    locSettings.EnterSide = LocomotiveEnterSide.Plus;
                                await settings.Save();
                            }

                            #endregion

                            var length = itRow.GetInt("length", 100);
                            var sensorEnter = itRow.GetString("enter");
                            var sensorOcc = itRow.GetString("occ");
                            var sensorIn = itRow.GetString("in");

                            const string sNotSet = "-.-";
                            const string sNotSet2 = "--";
                            if (sensorEnter.Equals(sNotSet, StringComparison.OrdinalIgnoreCase) || sensorEnter.Equals(sNotSet2, StringComparison.OrdinalIgnoreCase))
                                sensorEnter = string.Empty;
                            if (sensorOcc.Equals(sNotSet, StringComparison.OrdinalIgnoreCase) || sensorOcc.Equals(sNotSet2, StringComparison.OrdinalIgnoreCase))
                                sensorOcc = string.Empty;
                            if (sensorIn.Equals(sNotSet, StringComparison.OrdinalIgnoreCase) || sensorIn.Equals(sNotSet2, StringComparison.OrdinalIgnoreCase))
                                sensorIn = string.Empty;

                            var blockInstance = new libMetamodel.Settings.Block
                            {
                                Identifier = stagingStepId,
                                Length = length,
                                SensorEnter = sensorEnter,
                                SensorOcc = sensorOcc,
                                SensorIn = sensorIn
                            };

                            listOfBlocks.Add(blockInstance);
                        }

                        var updated = settings.UpdateStaging(stagingId, listOfBlocks);
                        if (updated || assignedAnyLocomotive)
                            await settings.Save();
                    }
                    return true;

                case "setting" when PgHelper.IsArgument(cmddata, "locomotiveExtras"):
                    {
                        var argumentValue = cmddata.GetValue("argumentValue") as JObject;

                        var driverName = argumentValue.GetString("driverName");
                        var objectId = argumentValue.GetInt("objectId");

                        var machineType = argumentValue?["machineType"] as JArray ?? new JArray();
                        var length = argumentValue.GetInt("length", 30);
                        var isCommuter = argumentValue.GetBool("isCommuter");
                        var doAutoAccelerate = argumentValue.GetBool("doAutoAccelerate", true);
                        var doAutoDeaccelerate = argumentValue.GetBool("doAutoDeaccelerate", true);
                        var minimumBlockWait = argumentValue.GetInt("minimumBlockWait");
                        var minimumBlockWaitAfterError = argumentValue.GetInt("minimumBlockWaitAfterError");
                        var speed = argumentValue["speed"] as JObject;
                        var speedMinimum = speed.GetInt("minimum", -1);
                        var speedEnter = speed.GetInt("entering", -1);
                        var speedStaging = speed.GetInt("staging", -1);
                        var speedTraveling = speed.GetInt("traveling", -1);
                        var speedMaximum = speed.GetInt("maximum", -1);

                        var settings = userWorkspace.Metamodel.Settings;

                        try
                        {
                            var locSettings = settings.FindLocomotiveBy(driverName, objectId);
                            if (locSettings == null)
                            {
                                locSettings = new Locomotive
                                {
                                    DriverName = driverName,
                                    ObjectId = objectId
                                };
                                settings.Locomotives.TryAdd((Locomotive)locSettings, false);
                            }

                            if (machineType.Count == 0)
                                locSettings.MachineType = LocomotiveType.None;
                            else
                                locSettings.MachineType = Locomotive.GetTypeOf(machineType[0].GetString("text"));
                            locSettings.Length = length;
                            locSettings.IsCommuter = isCommuter;
                            locSettings.DoAutoAccelerate = doAutoAccelerate;
                            locSettings.DoAutoDeaccelerate = doAutoDeaccelerate;
                            locSettings.MinimumBlockWait = minimumBlockWait;
                            locSettings.MinimumBlockWaitAfterError = minimumBlockWaitAfterError;
                            if (speedMinimum > 0) locSettings.Speed.Minimum = speedMinimum;
                            if (speedEnter > 0) locSettings.Speed.Entering = speedEnter;
                            if (speedStaging > 0) locSettings.Speed.Staging = speedStaging;
                            if (speedTraveling > 0) locSettings.Speed.Traveling = speedTraveling;
                            if (speedTraveling > 0) locSettings.Speed.Maximum = speedMaximum;

                            await settings.Save();
                        }
                        catch (Exception ex)
                        {
                            Logging.ExceptionLog(ex);
                        }
                    }
                    return true;

                case "setting":
                    {
                        var command = cmddata.GetString("command");
                        var argument = cmddata.GetString("argument");
                        var argumentValue = cmddata.GetString("argumentValue");

                        Logging.Log.Info($"<TODO::Settings> {command} -> {argument}:={argumentValue.Inline()}");
                    }
                    return false;
            }

            return false;
        }
    }
}

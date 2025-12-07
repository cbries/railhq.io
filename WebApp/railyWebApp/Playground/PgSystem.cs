// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos.Blocks;
using libEsuEcos.Entities;
using libShared;
using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libShared.MessageProtocol;
using libUtilities;
using libZ21;
using Newtonsoft.Json.Linq;
using railyWebApp.DataProvider;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable UsePatternMatching

namespace railyWebApp.Playground
{
    public class PgSystem
    {
        public static async Task<bool> StopAllTrains(string uid, IDataExchange exchange)
        {
            try
            {
                var dps = DataProviderManager.GetDataProviders(uid);
                foreach (var dp in dps)
                {
                    var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                    if (entities == null) continue;
                    foreach (var itEntity in entities)
                    {
                        if (itEntity is ILocomotive itLoc)
                        {
                            if (itLoc.Speedstep == 0) continue;

                            var ecosCommand = new JObject
                            {
                                { "driverName", itLoc.DriverName},
                                { "objectId", itLoc.ObjectId },
                                { "command", "update" },
                                { "argument", "speedstep" },
                                { "argumentValue", 0 }
                            };

                            await PgHelper.SendToGateway(uid, ecosCommand);
                        }
                    }
                }

                //
                // Demo
                //
                var dpDemo = DataProviderManager.GetDataProvider(uid, DataProviderType.Demo);
                var dpDemo0 = dpDemo.FirstOrDefault() ?? EmptyDataProvider.Empty;
                if (dpDemo0 != EmptyDataProvider.Empty)
                {
                    var locEntities = dpDemo0.Entities as IReadOnlyCollection<IEntity>;
                    if (locEntities != null)
                    {
                        foreach (var itt in locEntities)
                        {
                            var locEntity = itt as Locomotive;
                            if (locEntity == null) continue;
                            if (locEntity.Speedstep == 0) continue;

                            var changed = PgDemo.HandleDemoSpeedstep(locEntity, 0);
                            if (changed)
                            {
                                Trace.WriteLine(
                                    $"Ok, speed set to STOP for {locEntity.DriverName}::{locEntity.ObjectId}.");

                                await PgDemo.PublishEntityUpdate(
                                    dpDemo0 as IDataProviderDemoExtension,
                                    itt,
                                    null,
                                    exchange,
                                    uid);
                            }
                        }
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        #region Power ECOS & demo

        internal static async Task TogglePowerEcos(string uid, Ecos2 entity)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (entity == null) return;
            if (!entity.DriverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier)
                && !entity.DriverName.Equals(DataProviderDemo.ProviderName))
            {
                return;
            }

            // 
            // demo is based on ecos datamodel
            //
            if (entity.DriverName.Equals(DataProviderDemo.ProviderName, StringComparison.OrdinalIgnoreCase))
            {
                var targetState = PgControlStation.GetToggleEcosStatus(entity);
                if (string.IsNullOrEmpty(targetState)) return;

                var state = OnOff.Off;
                if (targetState.Equals("GO", StringComparison.OrdinalIgnoreCase))
                    state = OnOff.On;

                await SetPowerDemo(state, uid, entity);
            }
            else
            {
                //
                // physically ecos
                //

                var targetState = PgControlStation.GetToggleEcosStatus(entity);
                if (string.IsNullOrEmpty(targetState)) return;

                var state = OnOff.Off;
                if (targetState.Equals("GO", StringComparison.OrdinalIgnoreCase))
                    state = OnOff.On;

                await SetPowerEcos(state, uid, entity);
            }
        }

        internal static async Task SetPowerEcos(OnOff state, string uid, Ecos2 entityEcos)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (entityEcos == null) return;

            var ecosCommand = new JObject
            {
                {"driverName", libEsuEcos.Globals.EsuEcosIdentifier},
                {"objectId", 1},
                {"command", "update"},
                {"argument", "power"},
                {"argumentValue", state == OnOff.On ? "GO" : "STOP"}
            };

            await PgHelper.SendToGateway(uid, ecosCommand);
        }

        internal static async Task SetPowerDemo(OnOff state, string uid, Ecos2 entityDemo)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (entityDemo == null) return;

            var dps = DataProviderManager.GetDataProviders(uid);
            var dp = dps.FirstOrDefault(it => it.Type == DataProviderType.Demo);

            /*
                <EVENT 1>
                1 status2[ALL]
                1 status[GO]
                <END 0 (OK)>
             */
            const int demobaseId = 1;
            var s = $"<EVENT {demobaseId}>\n{demobaseId} status[{(state == OnOff.On ? "GO" : "STOP")}]\n{demobaseId}\n<END 0 (OK)>\n";
            var eventBlock = new EventBlock();
            eventBlock.Parse(s);
            var r = entityDemo.ParseData(eventBlock);
            if (r)
            {
                (dp as IDataProviderDemoExtension)?.TriggerEntityUpdate(entityDemo);
            }

            await Task.Delay(25);
        }

        #endregion

        #region Power Z21

        internal static async Task TogglePowerZ21(
            string uid,
            libZ21.Entities.Z21Station entity)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (entity == null) return;

            await SetPowerZ21(entity.TrackOn ? OnOff.Off : OnOff.On, uid, entity);
        }

        internal enum OnOff { On, Off }

        internal static async Task SetPowerZ21(OnOff state, string uid, libZ21.Entities.Z21Station entity)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (entity == null) return;

            var bytes = state == OnOff.On
                 ? Z21.PowerOnBytes
                 : Z21.PowerOffBytes;

            var payload = new Payload();
            payload.AddBytes(bytes);
            var gatewayCommand =
                BaseCommands.GetRelayCommand(libZ21.Globals.Z21Identifier, payload);

            await PgHelper.SendToGateway(uid, gatewayCommand);
        }

        #endregion

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

            switch (cmd)
            {
                //
                // `toggle` power
                //
                case "system" when PgHelper.IsArgument(cmddata, "power"):
                    {
                        var argValues = JsonMessageParser.Parse<MessagePower>(cmddata["argumentValue"]);
                        if (argValues == null) return false;

                        var driverName = argValues.DriverName;
                        if (string.IsNullOrEmpty(driverName)) return false;

                        var dps = DataProviderManager.GetDataProviders(uid);
                        var dp = dps.FirstOrDefault(it => it.Name == argValues.DriverName) ??
                                 EmptyDataProvider.Empty;
                        var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                        var stationEntity = entities?.FirstOrDefault(it => it.ObjectId == 1);

                        if (stationEntity == null) return false;

                        //
                        // toggle power
                        //
                        if (argValues.Action.Equals("toggle", StringComparison.OrdinalIgnoreCase))
                        {
                            if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier)
                                || driverName.Equals(DataProviderDemo.ProviderName))
                                await TogglePowerEcos(uid, stationEntity as Ecos2);

                            if (driverName.Equals(libZ21.Globals.Z21Identifier))
                                await TogglePowerZ21(uid, stationEntity as libZ21.Entities.Z21Station);

                            return true;
                        }

                        //
                        // switch on/off directly
                        //
                        if (argValues.Action.StartsWith("power", StringComparison.OrdinalIgnoreCase))
                        {
                            var powerOn = argValues.Action.Equals("powerOn", StringComparison.OrdinalIgnoreCase);

                            if (powerOn)
                            {
                                // ECoS
                                if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier))
                                    await SetPowerEcos(OnOff.On, uid, stationEntity as Ecos2);
                                
                                // Demo
                                if (driverName.Equals(DataProviderDemo.ProviderName))
                                    await SetPowerDemo(OnOff.On, uid, stationEntity as Ecos2);

                                // Z21
                                if (driverName.Equals(libZ21.Globals.Z21Identifier))
                                    await SetPowerZ21(OnOff.On, uid, stationEntity as libZ21.Entities.Z21Station);
                            }
                            else
                            {
                                // ECoS
                                if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier))
                                    await SetPowerEcos(OnOff.Off, uid, stationEntity as Ecos2);

                                // Demo
                                if (driverName.Equals(DataProviderDemo.ProviderName))
                                    await SetPowerDemo(OnOff.Off, uid, stationEntity as Ecos2);

                                // Z21
                                if (driverName.Equals(libZ21.Globals.Z21Identifier))
                                    await SetPowerZ21(OnOff.Off, uid, stationEntity as libZ21.Entities.Z21Station);
                            }
                        }

                        return true;
                    }

                case "system" when PgHelper.IsArgument(cmddata, "power", "stopAllTrains"):
                    return await StopAllTrains(uid, dataExchange);

                case "system" when PgHelper.IsArgument(cmddata, "power", "shutdown"):
                    {
                        // TODO wait to all locomotives to reach their current destination
                        // TODO after reaching, bring all other accessories in a standard position

                        await PgHelper.SendToGateway(uid, BaseCommands.CmdShutdown);
                    }
                    return true;

                case "system" when PgHelper.IsPayloadCommand(cmddata, "emergencyStop"):
                    {
                        await PgHelper.SendToGateway(uid, BaseCommands.CmdEmergencyStop);

                        var dps = DataProviderManager.GetDataProviders(uid);
                        foreach (var dp in dps)
                        {
                            var entities = dp.Entities as IReadOnlyCollection<IEntity>;
                            var stationEntity = entities?.FirstOrDefault(it => it.ObjectId == 1);
                            if (stationEntity == null) continue;

                            if (stationEntity is Ecos2 e0)
                                await SetPowerEcos(OnOff.Off, uid, e0);

                            if (stationEntity is libZ21.Entities.Z21Station e1)
                                await SetPowerZ21(OnOff.Off, uid, e1);
                        }
                    }
                    return true;

                case "system" when PgHelper.IsPayloadCommand(cmddata, "initialize"):
                    {
                        var cmdInit = BaseCommands.CmdInitialize;
                        if (cmddata["argumentValue"] is JObject argValue)
                            cmdInit["argumentValue"] = argValue;
                        else
                            cmdInit["argumentValue"] = new JObject();

                        argValue = cmdInit["argumentValue"] as JObject;

                        var checkWithWorkspace = false;
                        var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
                        if (wsres)
                        {
                            checkWithWorkspace = true;
                            Logging.Log.Info($"Workspace available, check only relevant accessories.");
                        }

                        // 
                        // add list of all relevant accessories which are
                        // ready for initialization; the local side is
                        // responsible for execution, not the controller,
                        // the controller just fires the startup of initialization
                        //
                        // Aber auch nur, wenn Accessories überhaupt geschaltet werden sollen!
                        //
                        var initAccessories = cmdInit["argumentValue"].GetBool("initAccessories");
                        if (initAccessories)
                        {
                            var ecosAccForInit = new List<AccessoryInitEntity>();
                            var z21AccForInit = new List<AccessoryInitEntity>();

                            // die Liste um die es am Ende geht
                            var listOfAccs = new List<IAccessory>();

                            //
                            // ecos
                            //
                            var dp0 = DataProviderManager.GetDataProvider(uid, DataProviderType.ECoS50210);
                            var dpEcos0 = dp0.FirstOrDefault() ?? EmptyDataProvider.Empty;
                            var accEntitiesEcos = dpEcos0.Entities as IReadOnlyCollection<IEntity>;
                            if (accEntitiesEcos != null)
                            {
                                foreach (var it in accEntitiesEcos)
                                {
                                    if (it is IAccessory itAcc)
                                        listOfAccs.Add(itAcc);
                                }
                            }

                            //
                            // z21
                            //
                            var dp1 = DataProviderManager.GetDataProvider(uid, DataProviderType.Z21);
                            var dpZ21 = dp1.FirstOrDefault() ?? EmptyDataProvider.Empty;
                            var accEntitiesZ21 = dpZ21.Entities as IReadOnlyCollection<IEntity>;
                            if (accEntitiesZ21 != null)
                            {
                                foreach (var it in accEntitiesZ21)
                                {
                                    if (it == null) continue;
                                    if (it.Type == EntityType.Accessory)
                                        listOfAccs.Add(it as IAccessory);
                                }
                            }

                            //
                            // iteriere über alle Schaltartikel
                            //
                            if (listOfAccs.Count > 0)
                            {
                                foreach (var itt in listOfAccs)
                                {
                                    if (itt is not { } accEntity) continue;

                                    if (accEntity.Type == EntityType.Accessory)
                                    {
                                        AccessoryInitEntity instance = null;

                                        if (checkWithWorkspace)
                                        {
                                            if (IsAccUsedInWorkspace(userWorkspace, accEntity))
                                            {
                                                instance = new AccessoryInitEntity
                                                {
                                                    Address = accEntity.Address,
                                                    AddrExt = accEntity.AddrExt,
                                                    ObjectId = accEntity.ObjectId,
                                                    DriverName = accEntity.DriverName
                                                };
                                            }
                                        }
                                        else
                                        {
                                            // wenn kein Workspace vorhanden
                                            // dann füge das Accessory in jedem Fall dazu

                                            instance = new AccessoryInitEntity
                                            {
                                                Address = accEntity.Address,
                                                AddrExt = accEntity.AddrExt,
                                                ObjectId = accEntity.ObjectId,
                                                DriverName = accEntity.DriverName
                                            };
                                        }

                                        if (instance != null)
                                        {
                                            if (accEntity.DriverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                                                ecosAccForInit.Add(instance);
                                            else if (accEntity.DriverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                                                z21AccForInit.Add(instance);
                                        }
                                    }
                                }
                            }

                            argValue?.Add(libEsuEcos.Globals.EsuEcosIdentifier, JArray.FromObject(ecosAccForInit));
                            argValue?.Add(libZ21.Globals.Z21Identifier, JArray.FromObject(z21AccForInit));
                        }

                        await PgHelper.SendToGateway(uid, cmdInit);
                    }
                    return true;
            }

            return false;
        }

        private static bool IsAccUsedInWorkspace(libUserspace.Workspace workspace, IEntity entity)
        {
            try
            {
                var settingsAccessories = workspace?.Metamodel?.Settings?.Accessories;
                if (settingsAccessories == null || settingsAccessories.Count == 0) return true;

                foreach (var it in settingsAccessories)
                {
                    if (it.Key.AccessoryDriver.Equals(entity.DriverName, StringComparison.OrdinalIgnoreCase)
                        && (
                            it.Key.AccessoryIdentifier.Equals(entity.DisplayName, StringComparison.OrdinalIgnoreCase)
                            || it.Key.AccessoryIdentifier.Equals(entity.Name0, StringComparison.OrdinalIgnoreCase)
                            ))
                    {
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                // ignore
            }

            // in any error case we will add the Accessory to the init list
            return true;
        }
    }

}

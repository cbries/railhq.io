// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using libShared.Entities.Impl;
using libShared.ExchangeProtocol;
using libUtilities;
using libZ21.Entities;
using libZ21.EntitiesPredefined;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace libZ21.DataProvider
{
    public enum EntityContainerType
    {
        None,
        General,
        Accessory,
        Locomotive
    }

    internal class EntitiesContainer : ConcurrentBag<IEntity>
    {
        public EntityContainerType Type { get; set; } = EntityContainerType.None;

        public bool Has(int objectId, out IEntity entity)
        {
            entity = null;

            foreach (var it in this)
            {
                if (it.ObjectId < 0) continue;
                if (it.ObjectId == objectId)
                {
                    entity = it;
                    return true;
                }
            }

            return false;
        }

        public IEntity FlagAsRemoved(int objectId)
        {
            if (Has(objectId, out var entity))
            {
                if (entity is Z21Entity z21Entity)
                {
                    var newIdx = GetNextUnusedNegativeId();
                    z21Entity.ObjectId = newIdx;

                    return z21Entity;
                }
            }

            return null;
        }

        private int GetNextUnusedNegativeId()
        {
            var usedNegativeIds = this
                .Select(e => e.ObjectId)
                .Where(id => id < 0)
                .ToHashSet();

            // Suche nach der kleinsten nicht verwendeten negativen ID
            var nextId = -2; // -1 ist dein "gelöscht"-Flag
            while (usedNegativeIds.Contains(nextId))
            {
                nextId--;
            }

            return nextId;
        }
    }

    internal class EntitiesFeedbackContainer : ConcurrentBag<Z21ModuleFeedback>
    {
        public bool Has(int module, out IEntityS88 entity)
        {
            entity = null;

            foreach (var it in this)
            {
                if (it.Port == -1) continue;
                if (it.Port == module)
                {
                    entity = it;
                    return true;
                }
            }

            return false;
        }
    }

    public class DataProvider
        : libShared.DataProvider.DataProvider,
            IDataProviderFeedback
    {
        public override DataProviderType Type => DataProviderType.Z21 | DataProviderType.S88Feedback;

        public override string Name => Globals.Z21Identifier;

        private readonly EntitiesContainer _generalEntitiesContainer = new() { Type = EntityContainerType.General };
        private readonly EntitiesContainer _locomotiveEntitiesContainer = new() { Type = EntityContainerType.Locomotive };
        private readonly EntitiesContainer _accessoryEntitiesContainer = new() { Type = EntityContainerType.Accessory };

        public override ICollection Entities
        {
            get
            {
                var container = new EntitiesContainer();
                foreach (var it in _generalEntitiesContainer)
                    container.Add(it);
                foreach (var it in _locomotiveEntitiesContainer)
                    container.Add(it);
                foreach (var it in _accessoryEntitiesContainer)
                    container.Add(it);
                return container;
            }
        }

        public ICollection GeneralEntities => _generalEntitiesContainer;
        public ICollection LocomotivesEntities => _locomotiveEntitiesContainer;
        public ICollection AccessoriesEntities => _accessoryEntitiesContainer;

        private readonly Dictionary<int, List<Z21ModuleFeedback>> _feedbackGroups = new ();

        #region IDataProviderFeedback

        public Dictionary<int, S88Entity> Ports
        {
            get
            {
                try
                {
                    var res = new Dictionary<int, S88Entity>();

                    foreach (var itGroup in _feedbackGroups)
                    {
                        var offset = itGroup.Key * 10;

                        foreach (var itFb in itGroup.Value)
                        {
                            var instance = new S88Entity
                            {
                                DriverName = itFb.DriverName,
                                BinaryState = itFb.BinaryState,
                                HexState = itFb.HexState,
                                MaxPorts = itFb.MaxPorts,
                                Pins = itFb.Pins,
                                Port = itFb.Port + offset
                            };

                            res.Add(instance.Port, instance);
                        }
                    }

                    return res;
                }
                catch
                {
                    // ignore
                }

                return new Dictionary<int, S88Entity>();
            }
        }

        #endregion

        #region Handling of Predefined entities

        private string _fleetBaseDir;
        private string _fleetLocDir;
        private string _fleetAccDir;

        private void SafeFileDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // ignore
            }

        }

        public bool RemovePredefinedEntity(string driverName, int address, RemoveType removeType)
        {
            var predefinedFileName = $"{driverName}_{address}.json";

            if (removeType == RemoveType.Accessory)
            {
                var accPath = Path.Combine(_fleetAccDir, predefinedFileName);
                SafeFileDelete(accPath);

                var updatedEntity = _accessoryEntitiesContainer?.FlagAsRemoved(address);
                if (updatedEntity != null)
                    OnEntityRemoved(driverName, address, removeType);

                return updatedEntity != null;
            }

            if (removeType == RemoveType.Locomotive)
            {
                var locPath = Path.Combine(_fleetLocDir, predefinedFileName);
                SafeFileDelete(locPath);

                var updatedEntity = _locomotiveEntitiesContainer?.FlagAsRemoved(address);
                if (updatedEntity != null)
                    OnEntityRemoved(driverName, address, removeType);

                return updatedEntity != null;
            }

            return false;
        }

        public bool AddPredefinedAccessoryEntity(
            string driverName, int address,
            string protocol,
            string name,
            string acctype,
            AddType addType)
        {
            var predefinedFileName = $"{driverName}_{address}.json";

            if (addType != AddType.Accessory) return false;

            var instance = new PredefinedEntityAccessory
            {
                Address = address,
                Protocol = protocol,
                Name = name,
                AccType = acctype
            };

            var accPath = Path.Combine(_fleetAccDir, predefinedFileName);
            try
            {
                File.WriteAllText(accPath, JsonConvert.SerializeObject(instance, Formatting.Indented));
            }
            catch
            {
                // ignore
            }

            return LoadEntities([instance]);
        }

        public bool AddPredefinedLocomotiveEntity(
            string driverName, int address,
            string protocol,
            string name,
            int oid,
            AddType addType)
        {
            var predefinedFileName = $"{driverName}_{address}.json";

            if (addType != AddType.Locomotive) return false;

            var instance = new PredefinedEntityLocomotive
            {
                Address = address,
                Protocol = protocol,
                Name = name,
                Functions = new List<FunctionEntry>()
            };

            var locPath = Path.Combine(_fleetLocDir, predefinedFileName);
            try
            {
                File.WriteAllText(locPath, JsonConvert.SerializeObject(instance, Formatting.Indented));
            }
            catch
            {
                // ignore
            }

            return LoadEntities([instance]);
        }
        
        public bool UpdatePredefinedLocomotiveFunction(PredefinedLocomotiveFunction fnc)
        {
            var predefinedFileName = $"{fnc.DriverName}_{fnc.Address}.json";
            var locPath = Path.Combine(_fleetLocDir, predefinedFileName);

            // update file on harddisk
            try
            {
                var cnt = File.ReadAllText(locPath, Encoding.UTF8);
                var obj = JObject.Parse(cnt);
                var arrFnc = obj["functions"] as JArray;
                if (arrFnc == null)
                    arrFnc = new JArray();
                var idxFound = -1;
                for (var i = 0; i < arrFnc.Count; ++i)
                {
                    var itm = arrFnc[i] as JObject;
                    var itmIdx = itm?.GetInt("fncidx", -1);
                    if (itmIdx == fnc.FncIdx)
                    {
                        idxFound = i;
                        break;
                    }
                }

                if (idxFound != -1)
                {
                    // update
                    arrFnc[idxFound]["name"] = fnc.Name;
                    arrFnc[idxFound]["description"] = fnc.Description;
                    arrFnc[idxFound]["isUsed"] = fnc.IsUsed;
                    arrFnc[idxFound]["icon"] = fnc.Icon;
                }
                else
                {
                    // new
                    var o = new JObject
                    {
                        {"fncidx", fnc.FncIdx},
                        {"name", fnc.Name},
                        {"description", fnc.Description},
                        {"isUsed", fnc.IsUsed},
                        {"icon", fnc.Icon}
                    };
                    arrFnc.Add(o);
                }

                // sortieren
                arrFnc = new JArray(arrFnc
                    .OfType<JObject>()
                    .OrderBy(o => (int?)o["fncidx"] ?? -1)
                );
                obj["functions"] = arrFnc;

                File.WriteAllText(locPath, obj.ToString(Formatting.Indented), Encoding.UTF8);
            }
            catch
            {
                // ignore
            }

            // update internal entity in data provider
            try
            {
                var cnt = File.ReadAllText(locPath, Encoding.UTF8);
                var instance = JsonConvert.DeserializeObject<PredefinedEntityLocomotive>(cnt);
                return LoadEntities([instance]);
            }
            catch
            {
                // ignore
            }

            return true;
        }

        public bool LoadPredefinedEntities(string uid, string fleetBaseDir)
        {
            _fleetBaseDir = fleetBaseDir;
            _fleetLocDir = Path.Combine(_fleetBaseDir, uid, "Locomotives");
            _fleetAccDir = Path.Combine(_fleetBaseDir, uid, "Accessories");

            // lese Lokomotiven ein
            Filesystem.CreateDir(_fleetLocDir);
            var locs = Loader.GetJsonFiles(_fleetLocDir, EntityFileType.Locomotive);

            Filesystem.CreateDir(_fleetAccDir);
            var accs = Loader.GetJsonFiles(_fleetAccDir, EntityFileType.Accessory);

            var mergedList = new List<IPredefinedEntity>();
            mergedList.AddRange(locs);
            mergedList.AddRange(accs);

            return LoadEntities(mergedList);
        }

        public bool LoadEntities(IReadOnlyList<IPredefinedEntity> entities)
        {
            foreach (var it in entities)
            {
                if (it == null) continue;

                Logging.Log.Debug($"{it.EntityType} := {it.Name}");

                switch (it.EntityType)
                {
                    case EntityFileType.Locomotive:
                        {
                            if (it is PredefinedEntityLocomotive locEntity)
                                HandleEntity(_locomotiveEntitiesContainer, locEntity.Address, locEntity);
                        }
                        break;

                    case EntityFileType.Accessory:
                        {
                            if (it is PredefinedEntityAccessory accEntity)
                                HandleEntity(_accessoryEntitiesContainer, accEntity.Address, accEntity);
                        }
                        break;
                }
            }

            return true;
        }

        #endregion

        private bool _isSimulationMode;

        public DataProvider()
        {
            InitZ21Controller();
        }

        #region z21

        private Z21 _z21Instance;

        private void InitZ21Controller()
        {
            _z21Instance = new Z21(null);
            _z21Instance.Register_LAN_X_STATUS_CHANGED(CallbackStatusChanged);
            _z21Instance.Register_LAN_CAN_DETECTOR(CallbackLanCanDetector);
            _z21Instance.Register_LAN_SYSTEMSTATE_DATACHANGED(CallbackLanSystemStateChanged);
            _z21Instance.Register_LAN_RAILCOM_DATACHANGED(CallbackLanRailcomChanged);
            _z21Instance.Register_LAN_RMBUS_DATACHANGED(CallbackLanRmBusChanged);
            _z21Instance.Register_LAN_X_LOCO_INFO(CallbackLanXLocoInfo);
            _z21Instance.Register_LAN_X_TURNOUT_INFO(CallbackTurnoutInfo);
            _z21Instance.Register_LAN_X_BC_TRACK_POWER_OFF(CallbackTrackPowerOff);
            _z21Instance.Register_LAN_X_BC_TRACK_POWER_ON(CallbackTrackPowerOn);
            _z21Instance.Register_LAN_X_BC_TRACK_SHORT_CIRCUIT(CallbackShortCircuit);

            _z21Instance.Register_LAN_GET_HWINFO(CallbackGetHwinfo);
        }

        private void CallbackStatusChanged(byte state)
        {
            var statusInterpreter = new Z21StatusInterpreter(state);
            HandleEntity(_generalEntitiesContainer, Globals.ID_EV_Z21Station, statusInterpreter);
        }

        private void CallbackGetHwinfo(HardwareInfo hwInfo)
        {
            HandleEntity(_generalEntitiesContainer, Globals.ID_EV_Z21Station, hwInfo);
        }

        private void CallbackTurnoutInfo(AccessoryInfo accessoryInfo)
        {
            Logging.Log.Debug(accessoryInfo);
            HandleEntity(_accessoryEntitiesContainer, accessoryInfo.Address, accessoryInfo);
        }

        private void CallbackShortCircuit()
        {
            var state = new ShortCircuitState { On = true };
            HandleZ21StationDatagram(state);
        }

        private void CallbackTrackPowerOn()
        {
            HandleTrackPower(true);
        }

        private void CallbackTrackPowerOff()
        {
            HandleTrackPower(false);
        }

        private void HandleTrackPower(bool state)
        {
            var onOffState = new OnOffState
            {
                On = state,
                ShortCircuit = state ? false : null
            };
            HandleZ21StationDatagram(onOffState);
        }

        private void CallbackLanXLocoInfo(LocomotiveInfo info)
        {
            HandleEntity(_locomotiveEntitiesContainer, info.Address, info);
        }

        private void HandleZ21StationDatagram(object data)
        {
            HandleEntity(_generalEntitiesContainer, Globals.ID_EV_Z21Station, data);
        }

        private void HandleEntity(
            EntitiesContainer container,
            int objectId,
            object data)
        {
            if (data == null) return;

            var res = container.Has(objectId, out var entity);
            if (!res && entity == null)
            {
                IEntity e = null;
                switch (container.Type)
                {
                    case EntityContainerType.General:
                        e = new Z21Station();
                        break;

                    case EntityContainerType.Accessory:
                        e = new Accessory();
                        break;

                    case EntityContainerType.Locomotive:
                        e = new Locomotive();
                        break;
                }

                if (e != null && e.ParseData(data))
                {
                    container.Add(e);
                    OnEntityUpdated(e);
                }

                return;
            }

            if (res)
            {
                var r = entity.ParseData(data);
                if (r)
                    OnEntityUpdated(entity);
            }
        }

        private void CallbackLanSystemStateChanged(SystemState state)
        {
            HandleZ21StationDatagram(state);
        }

        private void CallbackLanRmBusChanged(byte gruppenIndex, byte[] rmStatus)
        {
            if (!_feedbackGroups.ContainsKey(gruppenIndex))
            {
                var listOf = new List<Z21ModuleFeedback>();
                for(var i = 0; i < 10; ++i)
                    listOf.Add(new Z21ModuleFeedback
                    {
                        Port = (gruppenIndex * 10) + i + 1
                    });

                _feedbackGroups.Add(gruppenIndex, listOf);
            }

            var fbs = _feedbackGroups[gruppenIndex];
            for (var i = 0; i < 10; ++i)
            {
                var res = fbs[i].ParseData(rmStatus[i]);
                if (!res)
                {
                    //Logging.Log.Debug($"No update applied: {gruppenIndex}::{i}");
                }
                else
                {
                    OnEntityUpdated(fbs[i]);
                }
            }
        }

        // TODO
        private void CallbackLanRailcomChanged(RailcomData data)
        {
            Logging.Log.Debug("RailcomData:");
            Logging.Log.Debug(data);
        }

        // TODO
        private void CallbackLanCanDetector(CanDetectorMessage msg)
        {
            Logging.Log.Debug("CanDetectorMessage:");
            Logging.Log.Debug(msg);
        }

        #endregion

        public override bool Update(IReadOnlyList<Request> requests, bool isSimulationMode)
        {
            _isSimulationMode = isSimulationMode;

            if (requests == null) return false;
            if (requests.Count == 0) return false;

            try
            {
                foreach (var req in requests)
                {
                    var res = UpdateWith(req);
                    if (!res)
                        Logging.Log.Debug($"Request update failed: {req.Data.Payload.ToString().Inline()}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        private bool UpdateWith(Request request)
        {
            //Logging.Log.Debug($"<UpdateWith> {request.Data.Payload}");

            var payload = request.Data.Payload as JObject;
            if (payload == null)
            {
                Logging.Log.Debug("Invalid payload format, JSON required.");
                return false;
            }

            if (_z21Instance == null)
            {
                Logging.Log.Debug($"No z21 parser instance initialized.");
                return false;
            }

            try
            {
                var payloadData = payload.ToObject<Payload>();
                var encodedCommands = payloadData?.GetEncodedCommands();
                if (encodedCommands == null) return true;
                if (encodedCommands.Count == 0) return true;

                foreach (var z21Cmd in encodedCommands)
                {
                    if (z21Cmd == null) continue;
                    if (z21Cmd.Length == 0) continue;

                    _z21Instance.ParseData(z21Cmd);
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.Log.Error($"Invalid payload format", ex);
            }

            return false;
        }
    }
}

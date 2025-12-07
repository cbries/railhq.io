// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities;
using libUtilities;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace libMetamodel.Settings
{
    public class Settings : ISettings
    {
        public FileInfo OriginalFile { get; set; }

        public string Uuid { get; set; } = Guid.NewGuid().ToString("D");

        public IDebugging Debugging { get; set; } = new Debugging();

        public ConcurrentDictionary<Locomotive, bool> Locomotives { get; set; } = new();
        public ConcurrentDictionary<Route, bool> Routes { get; set; } = new();
        public ConcurrentDictionary<Block, bool> BlockSensors { get; set; } = new();
        public ConcurrentDictionary<Sensor, bool> Sensors { get; set; } = new();
        public ConcurrentDictionary<Accessory, bool> Accessories { get; set; } = new();
        public ConcurrentDictionary<Staging, bool> Stagings { get; set; } = new();

        public async Task Save()
        {
            try
            {
                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await File.WriteAllTextAsync(OriginalFile.FullName, json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        #region Locomotive Methods

        public ILocomotive FindLocomotiveBy(string driverName, int objectId)
        {
            if (string.IsNullOrEmpty(driverName)) return null;
            if (objectId < 0) return null;
            return Locomotives.FirstOrDefault(loc => loc.Key.DriverName == driverName && loc.Key.ObjectId == objectId).Key;
        }

        public const string DefaultLocomotiveOrientationNone = "none";
        public const LocomotiveEnterSide DefaultLocomotiveEnterSideNone = LocomotiveEnterSide.None;

        public const string DefaultLocomotiveOrientation = "left";
        public const LocomotiveEnterSide DefaultEnterSide = LocomotiveEnterSide.Plus;

        public bool AssignLocomotiveToBlock(string driverName, int objectId, string block, IEntity entity)
        {
            if (string.IsNullOrEmpty(driverName)) return false;
            if (objectId < 0) return false;
            if (string.IsNullOrEmpty(block)) return false;

            var loc = FindLocomotiveBy(driverName, objectId);
            if (loc == null)
            {
                loc = new Locomotive();
                loc.DriverName = driverName;
                loc.ObjectId = objectId;
                loc.AssignedToBlock = block;
                loc.Orientation = DefaultLocomotiveOrientation;
                loc.EnterSide = DefaultEnterSide;
                loc.InitSpeed(entity as libShared.Entities.ILocomotive);

                Locomotives.TryAdd((Locomotive)loc, false);

                return true;
            }

            var changed = false;

            // wenn die Lokomotive noch kein Assignment hat,
            // dann werden unsere Standardwerte gesetzt
            if (string.IsNullOrEmpty(loc.AssignedToBlock))
            {
                loc.Orientation = DefaultLocomotiveOrientation;
                loc.EnterSide = DefaultEnterSide;
                changed = true;
            }
            
            if (loc?.AssignedToBlock != null && !loc.AssignedToBlock.Equals(block))
            {
                loc.AssignedToBlock = block;
                changed = true;
            }

            return changed;
        }

        public bool RemoveLocomotiveAssignment(string driverName, int objectId)
        {
            if (string.IsNullOrEmpty(driverName)) return false;
            if (objectId < 0) return false;

            var loc = FindLocomotiveBy(driverName, objectId);
            if (loc == null) return false;

            var changed = false;
            if (!string.IsNullOrEmpty(loc.AssignedToBlock))
            {
                loc.AssignedToBlock = string.Empty;
                loc.Orientation = DefaultLocomotiveOrientationNone;
                loc.OrientationPrevious = DefaultLocomotiveOrientationNone;
                loc.EnterSide = DefaultLocomotiveEnterSideNone;
                changed = true;
            }

            return changed;
        }

        public bool RemoveLocomotive(string driverName, int objectId)
        {
            if (string.IsNullOrEmpty(driverName)) return false;
            if (objectId < 0) return false;
            var loc = Locomotives.FirstOrDefault(loc => loc.Key.DriverName == driverName && loc.Key.ObjectId == objectId);
            if (loc.Key == null) return false;
            return Locomotives.TryRemove(loc);
        }

        #endregion

        #region Staging Methods

        public IStaging FindStagingByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Stagings.FirstOrDefault(stage => stage.Key.Identifier == name).Key;
        }

        public Block FindStagingBlockByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            var cleanedName = name.Replace("[+]", string.Empty).Replace("[-]", string.Empty);

            foreach (var it in Stagings)
            {
                if (it.Key == null) continue;
                var stage = it.Key;
                foreach (var itt in stage.Blocks)
                {
                    if (string.IsNullOrEmpty(itt.Identifier)) continue;
                    if (itt.Identifier.Equals(cleanedName)) return itt;
                }
            }

            return null;
        }

        public bool IsLeavingStageBlock(Block stagingBlock, out Staging owner)
        {
            owner = null;

            if (string.IsNullOrEmpty(stagingBlock?.Identifier)) return false;

            foreach (var it in Stagings)
            {
                var lastBlock = it.Key?.Blocks?.Last();
                if (lastBlock == null) continue;

                if (string.IsNullOrEmpty(lastBlock.Identifier)) continue;
                if (lastBlock.Identifier.Equals(stagingBlock.Identifier))
                {
                    owner = it.Key;

                    return true;
                }
            }

            return false;
        }

        public bool UpdateStaging(string stagingIdentifier, List<libMetamodel.Settings.Block> blocks)
        {
            if (string.IsNullOrEmpty(stagingIdentifier)) return false;

            var stage = FindStagingByName(stagingIdentifier);
            if (stage != null && blocks.Count == 0)
            {
                var n = stage.Blocks.Count;
                stage.Blocks.Clear();
                return n > 0;
            }

            if (stage == null)
            {
                stage = new Staging();
                stage.Identifier = stagingIdentifier;
                stage.Blocks.AddRange(blocks);

                Stagings.TryAdd((Staging)stage, false);

                return true;
            }

            //var changed = false;
            if (!stage.Identifier.Equals(stagingIdentifier))
            {
                stage.Identifier = stagingIdentifier;
                //changed = true;
            }

            stage.Blocks.Clear();
            stage.Blocks.AddRange(blocks);

            return true;
        }

        public bool RemoveStaging(string stagingIdentifier)
        {
            if (string.IsNullOrEmpty(stagingIdentifier)) return false;
            try
            {
                var keyValue = Stagings.FirstOrDefault(stage => stage.Key.Identifier == stagingIdentifier);
                return Stagings.TryRemove(keyValue);
            }
            catch
            {
                // ignore
            }

            return false;
        }

        #endregion

        #region Block Methods

        public IBlock FindBlockByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            
            return BlockSensors.FirstOrDefault(block => block.Key.Identifier == name).Key;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blockIdentifier"></param>
        /// <param name="sensorEnter"></param>
        /// <param name="sensorIn"></param>
        /// <param name="signal"></param>
        /// <param name="vorsignal"></param>
        /// <returns>true when an update was applied</returns>
        public bool UpdateBlock(
            string blockIdentifier, 
            string sensorEnter, 
            string sensorIn,
            string signal,
            string vorsignal)
        {
            if (string.IsNullOrEmpty(blockIdentifier)) return false;
            //if (string.IsNullOrEmpty(sensorEnter)) return false;
            //if (string.IsNullOrEmpty(sensorIn)) return false;

            var block = FindBlockByName(blockIdentifier);
            if (block == null)
            {
                block = new Block();
                block.Identifier = blockIdentifier;
                block.SensorEnter = sensorEnter;
                block.SensorIn = sensorIn;
                if (block is IBlockSignals blockSignals)
                {
                    blockSignals.Signal = signal;
                    blockSignals.Vorsignal = vorsignal;
                }

                BlockSensors.TryAdd((Block)block, false);

                return true;
            }

            var changed = false;
            if (!block.Identifier.Equals(blockIdentifier))
            {
                block.Identifier = blockIdentifier;
                changed = true;
            }

            if (!block.SensorEnter.Equals(sensorEnter))
            {
                block.SensorEnter = sensorEnter;
                changed = true;
            }

            if (!block.SensorIn.Equals(sensorIn))
            {
                block.SensorIn = sensorIn;
                changed = true;
            }

            if (block is IBlockSignals blockSig)
            {
                if (!blockSig.Signal.Equals(signal))
                {
                    blockSig.Signal = signal;
                    changed = true;
                }

                if (!blockSig.Vorsignal.Equals(vorsignal))
                {
                    blockSig.Vorsignal = vorsignal;
                    changed = true;
                }
            }

            return changed;
        }

        public bool UpdateBlockExtras(string blockIdentifier, int? length, int? startDelay, int? signalsToRedDelay)
        {
            if (string.IsNullOrEmpty(blockIdentifier)) return false;

            var block = FindBlockByName(blockIdentifier);
            if (block == null)
            {
                block = new Block();
                block.Identifier = blockIdentifier;
                if (length.HasValue)
                    block.Length = length.Value;

                if (startDelay.HasValue)
                    block.StartDelay = startDelay.Value;

                if (signalsToRedDelay.HasValue)
                    block.SignalsToRedDelay = signalsToRedDelay.Value;

                BlockSensors.TryAdd((Block)block, false);

                return true;
            }

            var changed = false;
            if (length.HasValue)
            {
                block.Length = length.Value;
                changed = true;
            }

            if (startDelay.HasValue)
            {
                block.StartDelay = startDelay.Value;
                changed = true;
            }

            if (signalsToRedDelay.HasValue)
            {
                block.SignalsToRedDelay = signalsToRedDelay.Value;
                changed = true;
            }

            return changed;
        }

        #endregion

        #region Sensor Methods

        public ISensor FindSensorByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Sensors.FirstOrDefault(sensor => sensor.Key.Name == name).Key;
        }

        public bool UpdateSensor(string sensorName, string sensorProvider)
        {
            if (string.IsNullOrEmpty(sensorName)) return false;
            if (string.IsNullOrEmpty(sensorProvider)) return false;

            var sensor = FindSensorByName(sensorName);
            if (sensor == null)
            {
                sensor = new Sensor();
                sensor.Name = sensorName;
                sensor.Provider = sensorProvider;
                sensor.Address = string.Empty;

                Sensors.TryAdd((Sensor)sensor, false);

                return true;
            }

            var changed = false;
            if (!sensor.Provider.Equals(sensorProvider))
            {
                sensor.Provider = sensorProvider;
                changed = true;
            }

            return changed;
        }

        public bool UpdateSensorAddress(string sensorName, string sensorAddress)
        {
            if (string.IsNullOrEmpty(sensorName)) return false;
            if (string.IsNullOrEmpty(sensorAddress)) return false;

            var sensor = FindSensorByName(sensorName);
            if (sensor == null)
            {
                sensor = new Sensor();
                sensor.Name = sensorName;
                sensor.Provider = string.Empty;
                sensor.Address = sensorAddress;

                Sensors.TryAdd((Sensor)sensor, false);

                return true;
            }

            var changed = false;
            if (!sensor.Address.Equals(sensorAddress))
            {
                sensor.Address = sensorAddress;
                changed = true;
            }

            return changed;
        }

        #endregion

        #region Route Methods

        public IRoute FindRouteByName(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Routes.FirstOrDefault(route => route.Key.Name == name).Key;
        }

        public IRoute FindRouteByUid(string uid)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            return Routes.FirstOrDefault(route => route.Key.Uid == uid).Key;
        }

        public bool SetRouteDisable(string name, string routeUid, bool state)
        {
            if (string.IsNullOrEmpty(routeUid)) return false;

            var route = FindRouteByUid(routeUid);
            if (route == null)
            {
                route = new Route();
                route.Name = name;
                route.Uid = routeUid;
                route.IsEnabled = !state;

                Routes.TryAdd((Route)route, false);

                return true;
            }

            var changed = false;
            if (!route.IsEnabled != state)
            {
                route.IsEnabled = !state;
                changed = true;
            }

            return changed;
        }

        public bool SetRouteOccupied(string routeIdentifier, bool state, string reason = "")
        {
            if (string.IsNullOrEmpty(routeIdentifier)) return false;

            var route = FindRouteByName(routeIdentifier);
            if (route == null)
            {
                route = new Route();
                route.Name = routeIdentifier;
                route.IsOccupied = state;
                route.IsOccupiedReason = reason ?? string.Empty;

                Routes.TryAdd((Route)route, false);

                return true;
            }

            var changed = false;
            if (route.IsOccupied != state)
            {
                route.IsOccupied = state;
                changed = true;
            }
            if (route.IsOccupiedReason != null && !route.IsOccupiedReason.Equals(reason))
            {
                route.IsOccupiedReason = reason;
                changed = true;
            }

            return changed;
        }

        #endregion

        #region Accessory Methods

        public IAccessory FindAccessoryByPlanId(string planfieldId)
        {
            if (string.IsNullOrEmpty(planfieldId)) return null;
            return Accessories.FirstOrDefault(acc => acc.Key.PlanfieldControlIdentifier.Equals(planfieldId, StringComparison.OrdinalIgnoreCase)).Key;
        }

        public IReadOnlyList<IAccessory> FindAccessoriesByPlanId(string planfieldId)
        {
            if (string.IsNullOrEmpty(planfieldId)) return null;
            var res = Accessories.Where(acc => acc.Key.PlanfieldControlIdentifier == planfieldId);
            var res2 = new List<IAccessory>();
            foreach (var it in res)
                res2.Add(it.Key);
            return res2;
        }

        public IAccessory FindAccessoryByName(string driver, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            return Accessories.FirstOrDefault(acc => acc.Key.AccessoryDriver == driver && acc.Key.AccessoryIdentifier == name).Key;
        }

        public bool SetAccessoryMapping(string accessoryDriver, string accessoryIdentifier, string planfieldControlIdentifier)
        {
            if (string.IsNullOrEmpty(accessoryDriver)) return false;
            if (string.IsNullOrEmpty(accessoryIdentifier)) return false;
            if (string.IsNullOrEmpty(planfieldControlIdentifier)) return false;

            var accessoryDriverTrimmer = accessoryDriver.Trim();
            var accessoryIdentifierTrimmed = accessoryIdentifier.Trim();
            var planfieldControlIdentifierTrimmed = planfieldControlIdentifier.Trim();

            var acc = FindAccessoryByName(accessoryDriverTrimmer, accessoryIdentifierTrimmed);
            if (acc == null)
            {
                acc = new Accessory
                {
                    AccessoryDriver = accessoryDriverTrimmer,
                    AccessoryIdentifier = accessoryIdentifierTrimmed,
                    PlanfieldControlIdentifier = planfieldControlIdentifierTrimmed
                };

                Accessories.TryAdd((Accessory)acc, false);

                return true;
            }

            var changed = false;
            if (!acc.AccessoryDriver.Equals(accessoryDriverTrimmer))
            {
                acc.AccessoryDriver = accessoryDriverTrimmer;
                changed = true;
            }
            if (!acc.AccessoryIdentifier.Equals(accessoryIdentifierTrimmed))
            {
                acc.AccessoryIdentifier = accessoryIdentifierTrimmed;
                changed = true;
            }
            if (!acc.PlanfieldControlIdentifier.Equals(planfieldControlIdentifierTrimmed))
            {
                acc.PlanfieldControlIdentifier = planfieldControlIdentifierTrimmed;
                changed = true;
            }

            if (string.IsNullOrEmpty(planfieldControlIdentifier))
            {

            }

            return changed;
        }

        #endregion    
    }

    public class Debugging : IDebugging
    {
        public bool Runtime { get; set; } = true;
        public bool Accessories { get; set; } = false;
        public bool Locomotives { get; set; } = false;
        public bool Routes { get; set; } = false;
        public bool Exceptions { get; set; } = false;
    }

    public class Locomotive : ILocomotive
    {
        #region IStateBase

        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; } = false;

        #endregion

        [JsonIgnore] public string Selector => $"{DriverName}::{ObjectId}";

        public string DriverName { get; set; } = string.Empty;
        public int ObjectId { get; set; } = -1;
        public string AssignedToBlock { get; set; } = string.Empty;
        public LocomotiveEnterSide EnterSide { get; set; } = LocomotiveEnterSide.None;
        public ILocomotiveSpeed Speed { get; set; } = new LocomotiveSpeed();

        public int MinimumBlockWait { get; set; } = 10;
        public DateTime EarlistTimeForNextTrip { get; set; } = DateTime.MinValue;

        public int MinimumBlockWaitAfterError { get; set; } = 60;
        public DateTime EarlistNextTimeAfterError { get; set; } = DateTime.MinValue;

        public static ILocomotiveSpeed GetSpeedBy(string protocol)
        {
            var maxSpeedsteps = LocomotiveUtilities.GetNumberOfSpeedsteps(protocol);
            var speed = new LocomotiveSpeed();
            switch (maxSpeedsteps)
            {
                case 14:
                    {
                        if (speed.Minimum < 0) speed.Minimum = 1;
                        if (speed.Entering < 0) speed.Entering = 6;
                        if (speed.Traveling < 0) speed.Traveling = 8;
                        if (speed.Maximum < 0) speed.Maximum = 10;
                    }
                    break;

                case 27:
                case 28:
                    {
                        if (speed.Minimum < 0) speed.Minimum = 1;
                        if (speed.Entering < 0) speed.Entering = 12;
                        if (speed.Traveling < 0) speed.Traveling = 15;
                        if (speed.Maximum < 0) speed.Maximum = 18;
                    }
                    break;

                case 128:
                    {
                        if (speed.Minimum < 0) speed.Minimum = 5;
                        if (speed.Entering < 0) speed.Entering = 25;
                        if (speed.Traveling < 0) speed.Traveling = 40;
                        if (speed.Maximum < 0) speed.Maximum = 60;
                    }
                    break;
            }

            return speed;
        }

        public void InitSpeed(libShared.Entities.ILocomotive locomotiveEntity)
        {
            var protocol = locomotiveEntity.Protocol;
            var speed = GetSpeedBy(protocol);
            if (Speed.Minimum != speed.Minimum) Speed.Minimum = speed.Minimum;
            if (Speed.Entering != speed.Entering) Speed.Entering = speed.Entering;
            if (Speed.Traveling != speed.Traveling) Speed.Traveling = speed.Traveling;
            if (Speed.Maximum != speed.Maximum) Speed.Maximum = speed.Maximum;

            Speed.Stepping = LocomotiveUtilities.GetStepsForSpeedsteps(protocol);
        }

        #region ILocomotiveExtras

        public LocomotiveType MachineType { get; set; } = LocomotiveType.None;
        public int Length { get; set; } = 30;
        public bool IsCommuter { get; set; } = false;
        public bool DoAutoAccelerate { get; set; } = true;
        public bool DoAutoDeaccelerate { get; set; } = true;
        public string Orientation { get; set; } = "right";
        public string OrientationPrevious { get; set; } = "none";

        #endregion

        public static LocomotiveType GetTypeOf(string text)
        {
            if (string.IsNullOrEmpty(text)) return LocomotiveType.None;

            if (text.Equals("Dampf", StringComparison.OrdinalIgnoreCase)) return LocomotiveType.Steam;
            if (text.Equals("Steam", StringComparison.OrdinalIgnoreCase)) return LocomotiveType.Steam;

            if (text.Equals("Elektrisch", StringComparison.OrdinalIgnoreCase)) return LocomotiveType.Electric;
            if (text.Equals("Electric", StringComparison.OrdinalIgnoreCase)) return LocomotiveType.Electric;
            
            if (text.Equals("Diesel", StringComparison.OrdinalIgnoreCase)) return LocomotiveType.Diesel;
            
            return LocomotiveType.None;
        }
    }

    /// <summary>
    /// All values are pecentages of possible highest speedstep (ECoS wording).
    /// Märklin: 14     
    /// </summary>
    public class LocomotiveSpeed : ILocomotiveSpeed
    {
        public int Stepping { get; set; } = 3;

        public int Minimum { get; set; } = -1;
        public int Entering { get; set; } = -1;
        public int Staging { get; set; } = -1;
        public int Traveling { get; set; } = -1;
        public int Maximum { get; set; } = -1;
    }

    public class Route : IRoute
    {
        #region IStateBase

        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; } = false;

        #endregion

        public string Name { get; set; }
        public string Uid { get; set; }
        public bool IsOccupied { get; set; }
        public string IsOccupiedReason { get; set; }
    }

    public class Staging : IStaging
    {
        #region IStateBase

        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; } = false;

        #endregion

        public string Identifier { get; set; } = string.Empty;

        public List<Block> Blocks { get; set; } = new();

        [JsonIgnore]
        public List<string> BlockIdentifiers
        {
            get
            {
                var res = new List<string>();
                for (var i = 0; i < Blocks.Count; ++i)
                    res.Add($"{Identifier}_{i}");
                return res;
            }
        }
    }

    public class Block : IBlock, IBlockExtras, IBlockSignals
    {
        #region IStateBase

        public bool IsEnabled { get; set; } = true;
        public bool IsLocked { get; set; } = false;

        #endregion

        public string Identifier { get; set; } = string.Empty;
        public int Length { get; set; } = 50;
        public int StartDelay { get; set; } = 5;
        public int SignalsToRedDelay { get; set; } = 15;
        public string SensorEnter { get; set; } = string.Empty;
        public string SensorOcc { get; set; } = string.Empty;
        public string SensorIn { get; set; } = string.Empty;

        #region IBlockExtras

        public bool IsCommuterAllowedPlus { get; set; } = false;
        public bool IsCommuterAllowedMinus { get; set; } = false;

        #endregion

        #region IBlockSignals

        public string Signal { get; set; } = string.Empty;
        public string Vorsignal { get; set; } = string.Empty;

        #endregion
    }

    public class Sensor : ISensor
    {
        public string Name { get; set; } = string.Empty;
        public string Provider { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class Accessory : IAccessory
    {
        public string PlanfieldControlIdentifier { get; set; } = string.Empty;
        public string AccessoryDriver { get; set; } = string.Empty;
        public string AccessoryIdentifier { get; set; } = string.Empty;
        public bool Invert { get; set; } = false;
        public bool InvertUi { get; set; } = false;
        public bool IsMaintenaceEnabled { get; set; } = false;
    }
}

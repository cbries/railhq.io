// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus.pods;
using libMetamodel.Settings;
using libShared.DataProvider;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace libAutomaticModus.Running
{
    public enum RouteStates
    {
        Idle,
        WaitForAccessories,
        AccessoriesTimeout,
        AccessoriesReady,
        DelayStart,
        Accelerate,
        Cruise,
        WaitForSensorEnter,
        SensorEnter,
        Deaccelerate,
        WaitForSensorIn,
        SensorIn,
        Stop
    }

    public class RouteData
    {
        private readonly IReadOnlyList<IDataProvider> _dataProviders;

        public IReadOnlyList<IDataProvider> Dps => _dataProviders;

        public RouteData(IReadOnlyList<IDataProvider> dataProviders)
        {
            _dataProviders = dataProviders;
        }

        public void Cleanup()
        {
            // null this variables before Sensor*
            // otherwise the events will end in a null ptr exception
            if (_dpForEnter != null)
            {
                _dpForEnter.EntityUpdated -= DpForEnterOnEntityUpdated;
                _dpForEnter = null;
            }
            if (_dpForIn != null)
            {
                _dpForIn.EntityUpdated -= DpForInOnEntityUpdated;
                _dpForIn = null;
            }

            RouteName = string.Empty;
            RoutesCrossing?.Clear();
            _locomotive = null;
            TargetBlock = string.Empty;
            if (SensorEnter != null)
                SensorEnter.DataProvider = null;
            SensorEnter = null;
            if (SensorIn != null)
                SensorIn.DataProvider = null;
            SensorIn = null;
            LocomotiveRequestCommand = null;
            LocomotiveReleaseCommand = null;
        }

        private bool _canceled = false;
        public bool IsCanceled => _canceled;

        public void Cancel()
        {
            _canceled = true;
        }

        public string RouteName { get; set; }
        public List<string> RoutesCrossing { get; set; } = new();

        private Locomotive _locomotive;

        public Locomotive Locomotive
        {
            get => _locomotive;
            set
            {
                _locomotive = value;

                if (_locomotive == null)
                {
                    LocomotiveRequestCommand = null;
                    LocomotiveReleaseCommand = null;

                    return;
                }

                LocomotiveRequestCommand = new ActionRequest
                {
                    DriverName = _locomotive.DataProvider.Name,
                    ObjectId = _locomotive.Entity.ObjectId
                };

                LocomotiveReleaseCommand = new ActionRelease
                {
                    DriverName = _locomotive.DataProvider.Name,
                    ObjectId = _locomotive.Entity.ObjectId
                };
            }
        }

        /// <summary>
        /// This attribute is important.
        /// It is feasible that an Locomotive is assigned
        /// but associated control station is offline,
        /// i.e. not synchronization of the data is done.
        /// We would have a ghost assignement, but it is not.
        /// Anyway, a ghost assignment IS an assignment.
        /// The block is occupied, the route can be planned
        /// but NOT used.
        /// </summary>
        public bool IsValidLocomotive => Locomotive != null;

        /// <summary>
        /// Anzahl an Sekunden bis die Signale zurück auf rot geschaltet
        /// werden, wenn diese beim Start einer Route mit einer
        /// Lokomotive auf grün geschaltet wurden.
        /// </summary>
        public int SignalsToRedDelay { get; set; } = 15;

        public int SourceBlockDelay { get; set; } = 1;
        public string SourceBlock { get; set; } = string.Empty;

        public string TargetBlock { get; set; } = string.Empty;

        /// <summary>
        /// Mit diesem Flag wird bescrieben ob eine Lokomotive "virtuell" gedreht werden muss.
        /// Wenn zwei Blocks miteinander verbunden sind, dann gibt es mehrere Möglichkeiten:
        ///    [+] verlassen --> [-]    die Lokansicht muss NICHT gedreht werden
        ///    [+] verlassen --> [+]    die Lokansicht muss gedreht werden
        /// </summary>
        public bool TargetBlockFlip { get; set; } = false;

        public LocomotiveEnterSide SourceLeavingSide { get; set; } = LocomotiveEnterSide.None;
        public LocomotiveEnterSide TargetEnterSide { get; set; } = LocomotiveEnterSide.None;
        public Sensor SensorEnter { get; set; } = new();
        public Sensor SensorIn { get; set; } = new();

        /// <summary>
        /// Is used to change the driving directory (forward, backward).
        /// </summary>
        public bool IsCommuting { get; set; } = false;

        /// <summary>
        /// Must be set for simulation because the S88 sensor events must be detoured to the S88-Simulator data provider.
        /// </summary>
        public bool IsSimulationMode { get; set; } = false;

        /// <summary>
        /// Predefined commands forcing request / release the command control of the Locomotive on this route.
        /// </summary>
        public ActionRequest LocomotiveRequestCommand { get; private set; }

        public ActionRelease LocomotiveReleaseCommand { get; private set; }

        #region Sensoring / S88

        private IDataProvider _dpForEnter = null;
        private IDataProvider _dpForIn = null;

        private void PrepareSensorEnter()
        {
            if (SensorEnter?.DataProvider == null) return;

            var dpNameEnter = SensorEnter.DataProvider.Name;

            if (IsSimulationMode)
                dpNameEnter = "S88-Simulator";

            _dpForEnter = _dataProviders.FirstOrDefault(it => it.Name.Equals(dpNameEnter));
            if (_dpForEnter != null)
                _dpForEnter.EntityUpdated += DpForEnterOnEntityUpdated;
            else
                Logging.Log.Debug($"Route({RouteName}): SensorEnter without data provider: {SensorEnter.Pin}");
        }

        private void PrepareSensorIn()
        {
            if (SensorIn?.DataProvider == null) return;

            var dpNameIn = SensorIn.DataProvider.Name;

            if (IsSimulationMode)
                dpNameIn = "S88-Simulator";

            _dpForIn = _dataProviders.FirstOrDefault(it => it.Name.Equals(dpNameIn));
            if (_dpForIn != null)
                _dpForIn.EntityUpdated += DpForInOnEntityUpdated;
            else
                Logging.Log.Debug($"Route({RouteName}): SensorIn without data provider: {SensorIn.Pin}");
        }

        internal void PrepareSensorMonitoring()
        {
            PrepareSensorEnter();
            PrepareSensorIn();
        }

        private void DpForEnterOnEntityUpdated(object sender, IEntityBase e)
        {
            if (SensorEnter == null) return;

            if (e is not IEntityS88 s88Entity) return;

            try
            {
                if (SensorEnter.DataProvider.Type.HasFlag(DataProviderType.Z21))
                {
                    //
                    // Module:Pin
                    //
                    var parts = SensorEnter.ModulePin.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        Logging.Log.Error($"Invalid sensor adressing: {SensorEnter.ModulePin} (must be 'Module:Pin')");
                        return;
                    }

                    var module = int.Parse(parts[0].Trim());
                    var pin = int.Parse(parts[1].Trim());

                    Logging.Log.Debug(
                        $"SensorEnter@{module}::{pin}  ->  Port({s88Entity.Port}):={s88Entity.BinaryState}");
                    if (s88Entity.Port == module)
                    {
                        var binIdx = s88Entity.BinaryState.Length - pin;
                        SensorEnter.State = s88Entity.BinaryState[binIdx] == '1';
                        Logging.Log.Debug($"   State: {SensorEnter.State}");
                    }
                }
                else
                {
                    //
                    // Index
                    //
                    var res = S88Math.HandleInput(SensorEnter.Pin);
                    Logging.Log.Debug(
                        $"SensorEnter@{res.OutputPort}::{res.OutputPin}  ->  Port({s88Entity.Port}):={s88Entity.BinaryState}");
                    if (s88Entity.Port == res.OutputPort)
                    {
                        var binIdx = s88Entity.BinaryState.Length - res.OutputPin;
                        SensorEnter.State = s88Entity.BinaryState[binIdx] == '1';
                        Logging.Log.Debug($"   State: {SensorEnter.State}");
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        private void DpForInOnEntityUpdated(object sender, IEntityBase e)
        {
            if (SensorIn == null) return;

            if (e is not IEntityS88 s88Entity) return;

            try
            {
                if (SensorIn.DataProvider.Type.HasFlag(DataProviderType.Z21))
                {
                    //
                    // Module:Pin
                    //
                    var parts = SensorIn.ModulePin.Split(':', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length != 2)
                    {
                        Logging.Log.Error($"Invalid sensor adressing: {SensorIn.ModulePin} (must be 'Module:Pin')");
                        return;
                    }

                    var module = int.Parse(parts[0].Trim());
                    var pin = int.Parse(parts[1].Trim());

                    Logging.Log.Debug(
                        $"SensorIn@{module}::{pin}  ->  Port({s88Entity.Port}):={s88Entity.BinaryState}");
                    if (s88Entity.Port == module)
                    {
                        var binIdx = s88Entity.BinaryState.Length - pin;
                        SensorIn.State = s88Entity.BinaryState[binIdx] == '1';
                        Logging.Log.Debug($"   State: {SensorIn.State}");
                    }
                }
                else
                {
                    //
                    // Index
                    //
                    var res = S88Math.HandleInput(SensorIn.Pin);
                    Logging.Log.Debug(
                        $"SensorIn@{res.OutputPort}::{res.OutputPin}  ->  Port({s88Entity.Port}):={s88Entity.BinaryState}");
                    if (s88Entity.Port == res.OutputPort)
                    {
                        var binIdx = s88Entity.BinaryState.Length - res.OutputPin;
                        SensorIn.State = s88Entity.BinaryState[binIdx] == '1';
                        Logging.Log.Debug($"   State: {SensorIn.State}");
                    }
                }
            }
            catch
            {
                // ignore
            }
        }

        #endregion

        #region StateMachine

        public RouteStates State { get; set; }

        public RouteStates GoToState(RouteStates state)
        {
            State = state;

            return state;
        }

        public RouteStates NextState()
        {
            switch (State)
            {
                case RouteStates.Idle: State = RouteStates.WaitForAccessories; break;
                case RouteStates.WaitForAccessories: State = RouteStates.AccessoriesReady; break;
                case RouteStates.AccessoriesReady: State = RouteStates.DelayStart; break;
                case RouteStates.DelayStart: State = RouteStates.Accelerate; break;
                case RouteStates.Accelerate: State = RouteStates.Cruise; break;
                case RouteStates.Cruise: State = RouteStates.WaitForSensorEnter; break;
                case RouteStates.WaitForSensorEnter: State = RouteStates.SensorEnter; break;
                case RouteStates.SensorEnter: State = RouteStates.Deaccelerate; break;
                case RouteStates.Deaccelerate: State = RouteStates.WaitForSensorIn; break;
                case RouteStates.WaitForSensorIn: State = RouteStates.WaitForSensorIn; break;
                case RouteStates.SensorIn: State = RouteStates.Stop; break;
                case RouteStates.Stop: State = RouteStates.Idle; break;
            }

            return State;
        }

        #endregion

        #region Accelerate / Deaccelerate

        public DateTime StartAfterDelay { get; set; } = DateTime.Now;

        public RouteTiming Timing { get; set; } = new();

        /// <summary>
        /// List of `speedstep` and walltimes when it has been applied.
        /// </summary>
        public Dictionary<DateTime, int> Accelerate { get; set; } = new();

        /// <summary>
        /// List of `speedstep` and walltimes when it has been applied.
        /// </summary>
        public Dictionary<DateTime, int> Deaccelerate { get; set; } = new();

        public void PrepareAccelerateLookup(int start, int stop, int step, TimeSpan delay, DateTime dtStart)
        {
            for (var i = 0; start < stop; start += step, ++i)
            {
                var dt = dtStart + (i * delay);
                Accelerate.Add(dt, start);
            }
        }

        public void PrepareDeaccelerateLookup(int start, int stop, int step, TimeSpan delay, DateTime dtStart)
        {
            for (var i = 0; start >= stop; start -= step, ++i)
            {
                var dt = dtStart + (i * delay);
                Deaccelerate.Add(dt, start);
            }
        }

        public int GetAccelerateSpeedFor()
        {
            var now = DateTime.Now;
            foreach (var it in Accelerate)
            {
                if (now > it.Key) continue;
                return it.Value;
            }

            return -1;
        }

        public int GetDeaccelerateSpeedFor()
        {
            var now = DateTime.Now;
            foreach (var it in Deaccelerate)
            {
                if (now > it.Key) continue;
                return it.Value;
            }

            return -1;
        }

        #endregion
    }
}
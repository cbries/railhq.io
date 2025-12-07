// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus.pods;
using libAutomaticModus.Running;
using libShared;
using libShared.Entities;
using libUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using IAccessory = libShared.Entities.IAccessory;
using ILocomotive = libMetamodel.Settings.ILocomotive;
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable MergeIntoPattern

#pragma warning disable CS0414 // Field is assigned but its value is never used

namespace libAutomaticModus
{
    public class AutomaticRunner : IAutomaticRunner
    {
        public const int MonitorDelayStagesMs = 100; // milliseconds
        public const int MonitorDelayRoutesMs = 25; // milliseconds

        public const int RoutingDelaySelectNewRouteSecs = 1; // seconds

        public const string StateStraightName = "straight";
        public const string StateTurnName = "turn";

        // This value defines a walltime for a route.
        // When it is reached the route has timeout out
        // because of missing set for switches and signals.
        public const int AccessoriesSetTimeoutSeconds = 60;

        private readonly IDataExchange _dataExchange;
        private readonly string _uid;
        private readonly AutomaticRunnerCommands _arCmds;

        public AutomaticRunner(
            IDataExchange dataExchange,
            string uid
            )
        {
            _dataExchange = dataExchange;
            _uid = uid;

            _arCmds = new AutomaticRunnerCommands(this, _dataExchange, _uid);
        }

        private Task _backgroundTask;
        private Task _backgroundTaskStage;
        private CancellationTokenSource _cancellationTokenSource;

        private AutomaticData _automaticData;

        #region IAutomaticRunner

        public event EventHandler<string> StatusUpdated;
        public event EventHandler<ActionTriggeredData> ActionTriggered;

        public event EventHandler RoutingFinalized;
        public event EventHandler StagingFinalized;

        public bool IsSimulationMode { get; set; }
        public int RunningRoutes => RoutesUsed.Count;

        public DateTime Started { get; private set; } = DateTime.MaxValue;
        public DateTime Stopped { get; private set; } = DateTime.MaxValue;

        private bool _started = false;
        private bool _stopForce = false;

        public bool Start(AutomaticData preparedAutomaticData)
        {
            if (_started) return true;
            _started = true;
            _stopForce = false;

            _automaticData = preparedAutomaticData;
            _automaticData.IsSimulationMode = IsSimulationMode;

            _arCmds.ApplyAutomaticData(_automaticData);

            Started = DateTime.Now;

            _cancellationTokenSource = new CancellationTokenSource();

            if (_backgroundTask == null)
            {
                _backgroundTask = Task.Run(() => MonitorRoute(_cancellationTokenSource.Token));
                _backgroundTask.ContinueWith(async _ => await CleanupAutomode());
                OnStatusUpdated("Automode started");
            }

            if (_backgroundTaskStage == null)
            {
                _backgroundTaskStage = Task.Run(() => MonitorStage(_cancellationTokenSource.Token));
                _backgroundTaskStage.ContinueWith(async _ => await CleanupStageMode());
                OnStatusUpdated("Staging started");
            }

            return true;
        }

        public async Task<bool> Restore()
        {
            if (RunningRoutes == 0) return true;
            if (_arCmds == null) return false;

            foreach (var it in RoutesUsed)
            {
                if (it == null) continue;

                await _arCmds.SetHighlight(it.RouteName, true, true);

                var locEntity = it.Locomotive.Entity;
                await _arCmds.SetLocomotiveForBlockFinal(locEntity, it.TargetBlock, true);
            }

            return true;
        }

        private void StopRoutingTask()
        {
            if (_backgroundTask == null || _backgroundTask.IsCompleted)
            {
                try
                {
                    _backgroundTask?.Dispose();
                }
                catch
                {
                    // ignore
                }

                _backgroundTask = null;

                return;
            }

            _cancellationTokenSource.Cancel();
            _backgroundTask.Wait();
            _backgroundTask.Dispose();
            _backgroundTask = null;
        }

        private void StopStagingTask()
        {
            if (_backgroundTaskStage == null || _backgroundTaskStage.IsCompleted)
            {
                try
                {
                    _backgroundTaskStage?.Dispose();
                }
                catch
                {
                    // ignore
                }

                _backgroundTaskStage = null;

                return;
            }

            _cancellationTokenSource.Cancel();
            _backgroundTaskStage.Wait();
            _backgroundTaskStage.Dispose();
            _backgroundTaskStage = null;
        }

        public bool Stop()
        {
            StopRoutingTask();
            StopStagingTask();

            OnStatusUpdated("Automode will stop...");

            _started = false;

            return true;
        }

        private void ClearEventHandlers()
        {
            StatusUpdated = null;
            ActionTriggered = null;
        }

        public async Task<bool> StopForce()
        {
            ClearEventHandlers();

            _started = false;
            _stopForce = true;

            try
            {
                if (RunningRoutes > 0)
                {
                    foreach (var itRouteData in RoutesUsed)
                    {
                        //
                        // stop Trains
                        //
                        var driverName = itRouteData.Locomotive.Entity.DriverName;
                        var objectId = itRouteData.Locomotive.Entity.ObjectId;
                        _arCmds.ProcessSpeed(driverName, objectId, 0, -1);

                        //
                        // release locomotives
                        //
                        OnActionTriggered(itRouteData.LocomotiveReleaseCommand, _dataExchange);

                        //
                        // remove any automode highlights
                        //
                        var routeName = itRouteData.RouteName;
                        await _arCmds.SetHighlight(routeName, false);

                        var targetBlock = itRouteData.TargetBlock;
                        await _arCmds.ResetLocomotiveForBlockFinal(targetBlock);

                        //
                        // free occupied routes
                        //
                        var res = _automaticData.Settings.SetRouteOccupied(itRouteData.RouteName, false);
                        if (res)
                            await _dataExchange.SendSettingsToClients(_uid);

                        itRouteData.Cleanup();
                    }

                    RoutesUsed.Clear();
                }

            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            try
            {
                if (_automaticData.Settings != null)
                    await _automaticData.Settings.Save();
            }
            catch
            {
                // ignore
            }

            return true;
        }

        public bool IsStarted()
        {
            if (_backgroundTask != null && !_backgroundTask.IsCompleted)
                return true;
            return false;
        }

        #endregion

        private Routes RoutesUsed { get; } = new();
        private StageRoutes StageRoutesUsed { get; } = new();

        private bool _hasStageCanceled = false;
        private bool _isStageStopping = false;

        private bool _hasAutoModeCanceled = false;
        private bool _isAutoModeStopping = false;

        private bool AreAccessoryValidForRoute(RouteData selectedRoute, ref List<string> errorList)
        {
            if (selectedRoute == null) return false;

            try
            {
                var routeName = selectedRoute.RouteName;
                var route = _automaticData.RouteList.GetByName(routeName);

                var switches = route.Switches;
                foreach (var itSwitch in switches)
                {
                    // check if we can ask for the current state
                    // if not, the accessory is not correctly configured
                    var switchPlanItem = _automaticData.Planfield[$"{itSwitch.x}x{itSwitch.y}"];
                    var acc = _automaticData.Settings.FindAccessoryByPlanId(switchPlanItem.Identifier);
                    if (acc == null)
                    {
                        errorList.Add($"Keine Einstellungen für {switchPlanItem?.Identifier} vorhanden.");
                        continue;
                    }

                    var entityState = GetStateOfAccessory(acc);
                    if (string.IsNullOrEmpty(entityState))
                    {
                        errorList.Add($"Fehlende Statusinformationen für {switchPlanItem.Identifier}. Ist die Adressierung korrekt?");
                        continue;
                    }
                }
            }
            catch (Exception ex)
            {
                errorList.Add(ex.Message);
            }

            return !(errorList.Count > 0);
        }

        /// <summary>
        /// Used before auto mode is started, and when it has been finished.
        /// </summary>
        private async Task ResetAllOccupyRouteStates()
        {
            var routesNames = new List<string>();
            foreach (var itRoute in _automaticData.Settings.Routes)
                routesNames.Add(itRoute.Key.Name);
            foreach (var itRouteName in routesNames)
                _automaticData.Settings.SetRouteOccupied(itRouteName, false);
            await _automaticData.Settings.Save();
            await _dataExchange.SendSettingsToClients(_uid);
        }

        private async Task CleanupAutomode()
        {
            Logging.Log.Info("Automode has been stoppped, cleaning up...");
            _dataExchange.QueueDebugMessage(_uid, "Automode has been stoppped, cleaning up...");

            Stopped = DateTime.Now;

            //
            // Automode has stopped because of any failure, or is
            // stopped by user request, or is cancelled on any reason.
            //
            // We have to wait for all running locomotives to reach their 
            // finaly destination, in worst case scenarios we should
            // stop all trains immediatelly and <stop> the control stations.
            //

            var noOfRunningLocs = RoutesUsed.Count;

            var sb = new StringBuilder();
            sb.AppendLine($"Automode: still running {noOfRunningLocs} loc(s)");
            foreach (var itLoc in RoutesUsed)
                sb.AppendLine($"  [] {itLoc.Locomotive.Entity.DisplayName}");
            Logging.Log.Debug(sb);

            while (RoutesUsed.Count > 0)
            {
                //
                // This code exist two times!!!
                // On any change, change both!!!
                //
                #region !!! duplicate code !!!

                var finalizedRoutes = new Routes();
                foreach (var itProcessRoute in RoutesUsed)
                    if (await ProcessRoute(itProcessRoute))
                        finalizedRoutes.Add(itProcessRoute);

                finalizedRoutes.GetAll().ForEach(it =>
                {
                    OnActionTriggered(it.LocomotiveReleaseCommand, _dataExchange);
                    var res = _automaticData.Settings.SetRouteOccupied(it.RouteName, false);
                    if (res)
                    {
                        _automaticData.Settings.Save();
                        _dataExchange.SendSettingsToClients(_uid);
                    }
                    RoutesUsed.Remove(it);
                    it.Cleanup();
                });

                finalizedRoutes.Clear();

                #endregion

                await Task.Delay(MonitorDelayRoutesMs);
            }

            //
            // reset all routes, remove any occupy state
            //
            await ResetAllOccupyRouteStates();

            RoutingFinalized?.Invoke(this, EventArgs.Empty);

            IsSimulationMode = false;
        }

        private async Task CleanupStageMode()
        {
            Logging.Log.Info("Stageing has been stoppped, cleaning up...");
            _dataExchange.QueueDebugMessage(_uid, "Stageing has been stoppped, cleaning up...");

            while (StageRoutesUsed.Count > 0)
            {
                //
                // This code exist two times!!!
                // On any change, change both!!!
                //

                #region !!! duplicate code !!!

                var finalizedStageRoutes = new StageRoutes();
                foreach (var itProcessRoute in StageRoutesUsed)
                    if (await ProcessStage(itProcessRoute))
                        finalizedStageRoutes.Add(itProcessRoute);

                finalizedStageRoutes.ForEach(it =>
                {
                    OnActionTriggered(it.LocomotiveReleaseCommand, _dataExchange);
                    StageRoutesUsed.Remove(it);
                    it.Cleanup();
                });

                finalizedStageRoutes.Clear();

                await Task.Delay(MonitorDelayStagesMs);

                #endregion
            }

            StagingFinalized?.Invoke(this, EventArgs.Empty);
        }

        public int CurrentRunning => RoutesUsed.Count;
        public TimeSpan DelayBetweenAccelerateSteps = TimeSpan.FromSeconds(1);
        public TimeSpan DelayBetweenDeaccelerateSteps = TimeSpan.FromSeconds(1);

        private async Task MonitorStage(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    var allAvailableStageRoutes = _automaticData.GetAllAvailableStageRoutes(StageRoutesUsed);

                    //
                    // the order of taking does not matter
                    // we just make it a little random for more fun
                    //
                    var selectedRoute = allAvailableStageRoutes.GetRandomItem();
                    if (selectedRoute != null)
                    {
                        await _arCmds.SetHighlight(selectedRoute.RouteName, true);

                        await _arCmds.SetLocomotiveForBlockFinal(selectedRoute.Locomotive.Entity, selectedRoute.TargetBlock);

                        // TODO Do we need a occupy state/info?
                        // ...

                        //
                        // request the control for the locomotive
                        //
                        OnActionTriggered(selectedRoute.LocomotiveRequestCommand, _dataExchange);

                        StageRoutesUsed.Add(selectedRoute);
                    }

                    // 
                    // process any stage individually
                    //

                    #region !!! duplicate code !!!

                    var finalizedStageRoutes = new StageRoutes();
                    foreach (var itProcessRoute in StageRoutesUsed)
                        if (await ProcessStage(itProcessRoute))
                            finalizedStageRoutes.Add(itProcessRoute);

                    finalizedStageRoutes.ForEach(it =>
                    {
                        OnActionTriggered(it.LocomotiveReleaseCommand, _dataExchange);
                        StageRoutesUsed.Remove(it);
                        it.Cleanup();
                    });

                    finalizedStageRoutes.Clear();

                    await Task.Delay(MonitorDelayStagesMs, token);

                    #endregion
                }
            }
            catch (TaskCanceledException)
            {
                Logging.Log.Debug("Stage monitor canceled");

                _hasStageCanceled = true;
                _isStageStopping = true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, "Stage monitor failed");

                _hasStageCanceled = false;
                _isStageStopping = true;
            }
        }

        private DateTime _lastDtOfRouteSelect = DateTime.MinValue;
        private DateTime _nextDtOfRouteSelect => _lastDtOfRouteSelect + TimeSpan.FromSeconds(RoutingDelaySelectNewRouteSecs);
        public bool IsRouteSelectAllowed => _nextDtOfRouteSelect < DateTime.Now;

        private async Task MonitorRoute(CancellationToken token)
        {
            try
            {
                //
                // reset all routes, remove any occupy state
                //
                await ResetAllOccupyRouteStates();

                while (!token.IsCancellationRequested)
                {
                    //
                    // check if one or more locomotives are allowed to cruise
                    //
                    if (CurrentRunning < int.MaxValue && IsRouteSelectAllowed)
                    {
                        var allAvailableRoutesWithLocs = _automaticData.GetAllAvailableRoutes(RoutesUsed);

                        // set next time for route selection
                        _lastDtOfRouteSelect = DateTime.Now;

                        //#if DEBUG
                        foreach (var it in allAvailableRoutesWithLocs)
                        {
                            try
                            {
                                var sb = new StringBuilder();
                                if (it.IsValidLocomotive)
                                    sb.Append($"{it.Locomotive?.Entity?.DisplayName ?? "Unknown"}, ");
                                else
                                    sb.Append($"Start block has an assignment; but no hardware!");
                                sb.Append($"{it.RouteName}, ");
                                sb.Append($"{it.TargetBlock}, ");
                                sb.Append($"flip LocImage: {it.TargetBlockFlip}, ");
                                sb.Append($"{it.TargetEnterSide}, ");
                                sb.Append($"Enter: {it.SensorEnter.DataProvider?.Name}({it.SensorEnter?.Pin})");
                                sb.Append($"In: {it.SensorIn.DataProvider?.Name}({it.SensorIn?.Pin})");
                                //sb.Append($" x: {string.Join(", ", it.RoutesCrossing)} ");
                                //Logging.Log.Info($"## {sb}");
                                _dataExchange.QueueDebugMessage(_uid, $"## {sb}");
                            }
                            catch
                            {
                                // ignore
                            }
                        }
                        //#endif
                        var selectedRoute = allAvailableRoutesWithLocs.GetRandomRoute();

                        //
                        // if selected route has ghost locomotive
                        // we will ignore this selection
                        //
                        if (selectedRoute != null && !selectedRoute.IsValidLocomotive)
                        {
                            _dataExchange.QueueDebugMessage(_uid,
                                $"Ausgewählte Route '{selectedRoute.RouteName}' besitzt hat einen Geisterzug und wird ignoriert.");

                            selectedRoute = null;
                        }

                        if (selectedRoute != null)
                        {
                            // check route if it is correctly configured
                            // when accessory (switches) not set correctly
                            // the selected route would not make sense
                            var errorList = new List<string>();
                            var accAreValid = AreAccessoryValidForRoute(selectedRoute, ref errorList);
                            if (!accAreValid)
                            {
                                _dataExchange.QueueDebugMessage(_uid,
                                    $"Ausgewählte Route '{selectedRoute.RouteName}' ist nicht korrekt konfiguriert. Ein oder mehr Weichen haben keine ordentliche Konfiguration oder Planzuordnung.");

                                if (errorList.Count > 0)
                                {
                                    _dataExchange.QueueDebugMessage(_uid, $"Details: {string.Join(", ", errorList)}");
                                }

                                // ignore selected route
                                // one or more switches are not configured
                                selectedRoute = null;
                            }
                        }

                        if (selectedRoute != null)
                        {
                            //
                            // send route highlight command to webclients
                            //
                            await _arCmds.SetHighlight(selectedRoute.RouteName, true);

                            //
                            // trigger all relevant switches for the route
                            //

                            OnActionTriggered(new ActionPrepareSwitches
                            {
                                Argument = "route",
                                RouteName = selectedRoute.RouteName
                            }, _dataExchange);

                            //
                            // trigger all relevant signals for the route
                            //
                            OnActionTriggered(new ActionPrepareSignals
                            {
                                SourceBlock = selectedRoute.SourceBlock,
                                SourceLeavingSide = selectedRoute.SourceLeavingSide.ToString()
                            }, _dataExchange);

                            //
                            // flag the target block to visualize a coming locomotive
                            //
                            await _arCmds.SetLocomotiveForBlockFinal(selectedRoute.Locomotive.Entity, selectedRoute.TargetBlock);

                            //
                            // set the occupied state to the route
                            //
                            var reasonName = $"{selectedRoute.Locomotive.Entity.DisplayName} "
                                             + $"[{selectedRoute.Locomotive.DataProvider.Name}::{selectedRoute.Locomotive.Entity.ObjectId}]";
                            var res = _automaticData.Settings.SetRouteOccupied(selectedRoute.RouteName, true, $"Occupied by {reasonName}");
                            if (res) await _automaticData.Settings.Save();
                            await _dataExchange.SendSettingsToClients(_uid);

                            //
                            // request the control for the locomotive
                            //
                            OnActionTriggered(
                                selectedRoute.LocomotiveRequestCommand,
                                _dataExchange);

                            // 
                            // apply forward/backward in commuting
                            //
                            if (selectedRoute.IsCommuting)
                            {
                                var driverName = selectedRoute.Locomotive.Entity.DriverName;
                                var objectId = selectedRoute.Locomotive.Entity.ObjectId;

                                _arCmds.ChangeDirection(driverName, objectId, selectedRoute.Locomotive);
                            }

                            RoutesUsed.Add(selectedRoute);
                        }
                    }

                    //
                    // This code exist two times!!!
                    // On any change, change both!!!
                    //
                    #region !!! duplicate code !!!

                    var finalizedRoutes = new Routes();
                    foreach (var itProcessRoute in RoutesUsed)
                    {
                        if (await ProcessRoute(itProcessRoute))
                        {
                            finalizedRoutes.Add(itProcessRoute);
                        }
                        else
                        {
                            // check route state for error/timeout
                            if (itProcessRoute.State == RouteStates.AccessoriesTimeout)
                            {
                                // nothing else to do
                                // when accessories timeouted, then no locomotive started to cruise
                                itProcessRoute.Cancel();

                                // the route visualization must be reverted
                                await _arCmds.FinalizeRoute(itProcessRoute);
                                await _arCmds.KeepAssignment(itProcessRoute);

                                finalizedRoutes.Add(itProcessRoute);

                                var routeName = itProcessRoute.RouteName;
                                _dataExchange.QueueDebugMessage(_uid, $"Die Route {routeName} hat nach {AccessoriesSetTimeoutSeconds} Sekunden ein Zeitlimit überschritten, während auf das Zubehör gewartet wurde. Mindestens eine Weiche oder ein Signal hat nicht den gewünschten Zustand erreicht.", DebugMessageT.Routes);
                            }
                        }
                    }

                    finalizedRoutes.GetAll().ForEach(it =>
                    {
                        OnActionTriggered(it.LocomotiveReleaseCommand);
                        var res = _automaticData.Settings.SetRouteOccupied(it.RouteName, false);
                        if (res)
                        {
                            _automaticData.Settings.Save();
                            _dataExchange.SendSettingsToClients(_uid);
                        }
                        RoutesUsed.Remove(it);
                        it.Cleanup();
                    });

                    finalizedRoutes.Clear();

                    #endregion

                    await Task.Delay(MonitorDelayRoutesMs, token);
                }
            }
            catch (TaskCanceledException)
            {
                Logging.Log.Debug("Automode cancelled");

                _hasAutoModeCanceled = true;
                _isAutoModeStopping = true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, "Automode failed");

                _hasAutoModeCanceled = false;
                _isAutoModeStopping = true;
            }
        }

        public string GetStateOfAccessory(libMetamodel.Settings.IAccessory acc)
        {
            if (acc == null) return string.Empty;

            var driverName = acc.AccessoryDriver;
            var accessoryIdentifier = acc.AccessoryIdentifier;

            if (string.IsNullOrEmpty(driverName)) return string.Empty;
            if (string.IsNullOrEmpty(accessoryIdentifier)) return string.Empty;

            var dp = _automaticData.GetDataProviderByName(driverName);
            if (dp == null) return string.Empty;

            var entities = dp.Entities as IReadOnlyCollection<IEntity>;
            var accEntity = entities?.FirstOrDefault(it =>
                it.Type == EntityType.Accessory
                && it.Name0.Equals(accessoryIdentifier))
                    as IAccessory;

            if (accEntity == null) return string.Empty;

            return accEntity.State;
        }

#if DEBUG
        // for tests only, to reduce output
        private RouteStates _recentState = RouteStates.Idle;
#endif

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stageData"></param>
        /// <returns>true when the route is finalized and freed for other locomotives</returns>
        private async Task<bool> ProcessStage(StageData stageData)
        {
            if (stageData == null) return true;
            if (_stopForce) return true;

            try
            {

                var driverName = stageData.Locomotive.Entity.DriverName;
                var objectId = stageData.Locomotive.Entity.ObjectId;
                var locomotiveEntity = _automaticData.Settings.FindLocomotiveBy(driverName, objectId);

                // in case any of these states are recognized we will immediately go threw the related state
                if (stageData.State < StageStates.SensorIn && stageData.SensorIn.State)
                    stageData.State = StageStates.SensorIn;
                else if (stageData.State < StageStates.SensorEnter && stageData.SensorEnter.State)
                    stageData.State = StageStates.SensorEnter;

                var currentSpeedstep = stageData.Locomotive.Entity.Speedstep;

                switch (stageData.State)
                {
                    case StageStates.Idle:
                        {
                            stageData.NextState();
                            stageData.Timing.Started = DateTime.Now;

                            var targetSpeed = GetStagingSpeed(locomotiveEntity);

                            _arCmds.ProcessSpeed(driverName, objectId, targetSpeed, currentSpeedstep);
                        }
                        break;

                    case StageStates.Cruise:
                        {
                            if (stageData.Timing.CruiseStart == DateTime.MinValue)
                                stageData.Timing.CruiseStart = DateTime.Now;

                            var targetSpeed = GetStagingSpeed(locomotiveEntity);

                            _arCmds.ProcessSpeed(driverName, objectId, targetSpeed, currentSpeedstep);
                        }
                        break;

                    case StageStates.WaitForSensorEnter:
                        {
                            if (stageData.SensorEnter.State)
                                stageData.NextState();
                        }
                        break;

                    case StageStates.SensorEnter:
                        {
                            stageData.NextState();
                            stageData.Timing.Enter = DateTime.Now;

                            var targetSpeed = GetStagingSpeed(locomotiveEntity);

                            _arCmds.ProcessSpeed(driverName, objectId, targetSpeed, currentSpeedstep);
                        }
                        break;

                    case StageStates.WaitForSensorIn:
                        {
                            var r = stageData.GetDeaccelerateSpeedFor();
                            if (r < 0)
                                stageData.NextState();

                            _arCmds.ProcessSpeed(driverName, objectId, r, currentSpeedstep);
                        }
                        break;

                    case StageStates.SensorIn:
                        {
                            if (stageData.SensorIn.State)
                                stageData.NextState();
                        }
                        break;

                    case StageStates.Stop:
                        {
                            // force stop again
                            _arCmds.ProcessSpeed(driverName, objectId, 0, currentSpeedstep);

                            await _arCmds.FinalizeRoute(stageData);
                            await _arCmds.ReassignLocomotive(stageData);

                            // Stop -> Idle
                            stageData.NextState();
                            stageData.Timing.Ended = DateTime.Now;
                        }

                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return true;
        }

        private int GetStagingSpeed(ILocomotive locomotiveEntity)
        {
            var targetSpeed = locomotiveEntity.Speed.Staging;
            if (targetSpeed <= 0) targetSpeed = locomotiveEntity.Speed.Minimum;
            if (targetSpeed <= 0) targetSpeed = 1;
            return targetSpeed;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routeData"></param>
        /// <returns>true when the route is finalized and freed for other locomotives</returns>
        private async Task<bool> ProcessRoute(RouteData routeData)
        {
            if (_stopForce) return true;
            if (routeData == null) return true;

            try
            {
                var driverName = routeData.Locomotive.Entity.DriverName;
                var objectId = routeData.Locomotive.Entity.ObjectId;
                var locomotiveSettings = _automaticData.Settings.FindLocomotiveBy(driverName, objectId);

                // in case any of these states are recognized we will immediately go threw the related state
                if (routeData.State < RouteStates.SensorIn && routeData.SensorIn.State)
                    routeData.State = RouteStates.SensorIn;
                else if (routeData.State < RouteStates.SensorEnter && routeData.SensorEnter.State)
                    routeData.State = RouteStates.SensorEnter;

                var currentSpeedstep = routeData.Locomotive.Entity.Speedstep;

                switch (routeData.State)
                {
                    case RouteStates.Idle:
                        {
                            routeData.NextState();
                            routeData.Timing.Started = DateTime.Now;
                        }
                        break;

                    case RouteStates.WaitForAccessories:
                        {
                            var routeName = routeData.RouteName;

                            if (DateTime.Now > (routeData.Timing.Started +
                                                TimeSpan.FromSeconds(AccessoriesSetTimeoutSeconds)))
                            {
                                // timeout during waiting for target accessory states
                                routeData.GoToState(RouteStates.AccessoriesTimeout);

                                return false;
                            }

                            var route = _automaticData.RouteList.GetByName(routeName);
                            var switches = route.Switches;
                            foreach (var itSwitch in switches)
                            {
                                // the state we need
                                var targetState = itSwitch.Switch.State;

                                // query state it has
                                var switchPlanItem = _automaticData.Planfield[$"{itSwitch.x}x{itSwitch.y}"];
                                var acc = _automaticData.Settings.FindAccessoryByPlanId(switchPlanItem.Identifier);
                                var entityState = GetStateOfAccessory(acc);

                                if (int.TryParse(entityState, out var entityStateInt))
                                {
                                    //
                                    // Invertierung ist aktuell nur für einfache Schaltartikel erlaubt.
                                    // Wir können einfach zwischen 0 und 1 wechseln.
                                    //
                                    if (acc.Invert)
                                    {
                                        entityStateInt = entityStateInt == 0 ? 1 : 0;
                                    }
                                }

                                var entityStateHumanReadable = string.Empty;
                                if (string.IsNullOrEmpty(entityState) || entityStateInt == -1)
                                {
                                    entityStateHumanReadable = StateStraightName;
                                }
                                else if (entityStateInt == 0)
                                {
                                    entityStateHumanReadable = StateStraightName;
                                }
                                else if (entityStateInt == 1)
                                {
                                    entityStateHumanReadable = StateTurnName;
                                }

                                if (!targetState.Equals(entityStateHumanReadable, StringComparison.OrdinalIgnoreCase)
                                    &&
                                    !targetState.StartsWith(entityStateHumanReadable, StringComparison.OrdinalIgnoreCase))
                                {
                                    // state does not equal, route not ready
                                    return false;
                                }
                            }

                            var signals = route.Signals;
                            foreach (var itSignal in signals)
                            {
                                // the state we need
                                //var targetState = itSignal. ???

                                // TODO we do not know the needed target state for the route

                            }

                            routeData.NextState();
                        }
                        break;

                    case RouteStates.AccessoriesReady:
                        {
                            // Ok, when accessories ready, the locomotive can start to cruise.
                            // Move to next state...
                            routeData.NextState();

                            routeData.StartAfterDelay = DateTime.Now + TimeSpan.FromSeconds(routeData.SourceBlockDelay);
                        }
                        break;

                    case RouteStates.DelayStart:
                        {
                            // Wir warten bis das Delay zur Abfahrt worüber ist.
                            if (DateTime.Now < routeData.StartAfterDelay) return false;

                            routeData.NextState();

                            _arCmds.ProcessSpeed(driverName, objectId, locomotiveSettings.Speed.Minimum, currentSpeedstep);

                            routeData.PrepareAccelerateLookup(
                                locomotiveSettings.Speed.Minimum,
                                locomotiveSettings.Speed.Traveling,
                                locomotiveSettings.Speed.Stepping,
                                DelayBetweenAccelerateSteps,
                                DateTime.Now);
                        }
                        break;

                    case RouteStates.Accelerate:
                        {
                            if (!locomotiveSettings.DoAutoAccelerate)
                            {
                                routeData.NextState();
                            }
                            else
                            {
                                var r = routeData.GetAccelerateSpeedFor();
                                if (r < 0)
                                    routeData.NextState();

                                _arCmds.ProcessSpeed(driverName, objectId, r, currentSpeedstep);
                            }
                        }
                        break;

                    case RouteStates.Cruise:
                        {
                            if (routeData.Timing.CruiseStart == DateTime.MinValue)
                                routeData.Timing.CruiseStart = DateTime.Now;

                            _arCmds.ProcessSpeed(driverName, objectId, locomotiveSettings.Speed.Traveling, currentSpeedstep);

                            routeData.NextState();
                        }
                        break;

                    case RouteStates.WaitForSensorEnter:
                        {
                            if (routeData.SensorEnter.State)
                                routeData.NextState();
                        }
                        break;

                    case RouteStates.SensorEnter:
                        {
                            routeData.NextState();
                            routeData.Timing.Enter = DateTime.Now;

                            var deaccelerateStartSpeed = currentSpeedstep;

                            if (currentSpeedstep > locomotiveSettings.Speed.Entering)
                            {
                                var dpZ21 = routeData.Dps.FirstOrDefault(it => it.Name == locomotiveSettings.DriverName);
                                var locEntities = (dpZ21?.Entities as IReadOnlyCollection<IEntity>)?.Where(it => it.Type == EntityType.Locomotive);
                                var locEntity = locEntities?.FirstOrDefault(it => it.ObjectId == objectId) as libShared.Entities.ILocomotive;

                                if (locEntity == null)
                                {
                                    Logging.Log.Error($"Locomotive entity for automode not available: {driverName}::{objectId}");

                                    routeData.State = RouteStates.Stop;
                                }
                                else
                                {
                                    OnActionTriggered(new ActionSpeedstep
                                    {
                                        DriverName = driverName,
                                        ObjectId = objectId,
                                        Speed = locomotiveSettings.Speed.Entering,
                                        MaxSpeedSteps = locEntity.Protocol,
                                        Direction = (int)locEntity.Direction
                                    });
                                }

                                deaccelerateStartSpeed = locomotiveSettings.Speed.Entering;
                            }

                            routeData.PrepareDeaccelerateLookup(
                                deaccelerateStartSpeed,
                                locomotiveSettings.Speed.Minimum,
                                locomotiveSettings.Speed.Stepping,
                                DelayBetweenDeaccelerateSteps,
                                DateTime.Now);
                        }
                        break;

                    case RouteStates.Deaccelerate:
                        {
                            if (!locomotiveSettings.DoAutoDeaccelerate)
                            {
                                routeData.NextState();
                            }
                            else
                            {
                                var r = routeData.GetDeaccelerateSpeedFor();
                                if (r < 0)
                                    routeData.NextState();

                                _arCmds.ProcessSpeed(driverName, objectId, r, currentSpeedstep);
                            }
                        }
                        break;

                    case RouteStates.WaitForSensorIn:
                        {
                            if (routeData.SensorIn.State)
                                routeData.NextState();
                        }
                        break;

                    case RouteStates.SensorIn:
                        {
                            routeData.NextState();
                            routeData.Timing.In = DateTime.Now;

                            _arCmds.ProcessSpeed(driverName, objectId, 0, currentSpeedstep);
                        }
                        break;

                    case RouteStates.Stop:
                        {
                            // force stop again
                            _arCmds.ProcessSpeed(driverName, objectId, 0, currentSpeedstep);

                            await _arCmds.FinalizeRoute(routeData);
                            await _arCmds.ReassignLocomotive(routeData);

                            // Stop -> Idle
                            routeData.NextState();
                            routeData.Timing.Ended = DateTime.Now;
                        }

                        return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return true;
        }


        protected virtual void OnStatusUpdated(string message)
        {
            StatusUpdated?.Invoke(this, message);
        }

        public virtual void OnActionTriggered(IActionData actionData, IDataExchange dataExchange = null)
        {
            ActionTriggered?.Invoke(this, new ActionTriggeredData
            {
                Data = actionData,
                DataExchange = dataExchange
            });
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus.Running;
using libMetamodel.Settings;
using libShared;
using libShared.DataProvider;
using libTrackplan.Analyzer;
using libTrackplan.Plan;
using libTrackplan.Route;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using ILocEntity = libShared.Entities.ILocomotive;

namespace libAutomaticModus
{
    public class AutomaticData
    {
        private IDataExchange DataExchange { get; }
        private string Uid { get; }
        public IReadOnlyList<IDataProvider> DataProviders { get; }
        public libTrackplan.Route.RouteList RouteList { get; }
        public ISettings Settings { get; }
        public ConcurrentDictionary<string, PlanItem> Planfield { get; private set; }

        internal bool IsSimulationMode { get; set; }

        public AutomaticData(
            IDataExchange dataExchange,
            string uid,
            IReadOnlyList<IDataProvider> dataProviders,
            libTrackplan.Route.RouteList routeList,
            ISettings settings,
            ConcurrentDictionary<string, PlanItem> planfield)
        {
            DataExchange = dataExchange;
            Uid = uid;
            DataProviders = dataProviders;
            RouteList = routeList;
            Settings = settings;
            Planfield = planfield;

            // prepare automatic
            CreateInternalListOfLocomotives();
        }

        #region Locomotive Inventary

        internal ListOfLocomotives LocomotivesInventary { get; private set; }

        internal Locomotive GetLocomotiveInventary(string driverName, int objectId)
        {
            if (string.IsNullOrEmpty(driverName)) return null;
            if (objectId < 0) return null;
            foreach (var it in LocomotivesInventary)
            {
                if (!it.DataProvider.Name.Equals(driverName)) continue;
                if (it.Entity.ObjectId != objectId) continue;
                return it;
            }
            return null;
        }

        private void CreateInternalListOfLocomotives()
        {
            LocomotivesInventary = [];

            foreach (var dp in DataProviders)
            {
                foreach (var dpEntity in dp.Entities)
                {
                    if (dpEntity is not ILocEntity entity) continue;

                    var locInstance = new Locomotive
                    {
                        DataProvider = dp,
                        Entity = entity
                    };

                    LocomotivesInventary.Add(locInstance);
                }
            }
        }

        #endregion

        internal IDataProvider GetDataProviderByName(string driverName)
        {
            if (string.IsNullOrEmpty(driverName)) return null;
            foreach (var it in DataProviders)
                if (it.Name.Equals(driverName))
                    return it;
            return null;
        }

        private bool IsAnyLocomotiveAssignedTo(string blockIdentifier, List<libMetamodel.Settings.Locomotive> locomotives)
        {
            if (string.IsNullOrEmpty(blockIdentifier)) return false;
            if (locomotives == null) return false;
            if (locomotives.Count == 0) return false;

            foreach (var it in locomotives)
            {
                if (string.IsNullOrEmpty(it.AssignedToBlock)) continue;
                if (it.AssignedToBlock.Equals(blockIdentifier))
                    return true;
            }

            return false;
        }

        private bool IsBlockLongEnough(string blockIdentifier, int locLength, out int blockLength)
        {
            blockLength = 0;
            if (string.IsNullOrEmpty(blockIdentifier)) return false;
            var targetBlock = Settings.FindBlockByName(blockIdentifier);
            if (targetBlock == null)
            {
                targetBlock = Settings.FindStagingBlockByName(blockIdentifier);

                if(targetBlock == null)
                    return false;
            }
            blockLength = targetBlock.Length;
            return targetBlock.Length >= locLength;
        }

        private bool IsBlockEnabled(string blockIdentifier)
        {
            if (string.IsNullOrEmpty(blockIdentifier)) return false;
            var targetBlock = Settings.FindBlockByName(blockIdentifier);
            if (targetBlock == null)
            {
                targetBlock = Settings.FindStagingBlockByName(blockIdentifier);

                if (targetBlock == null)
                    return false;
            }
            return targetBlock.IsEnabled;
        }

        private bool HasBlockAnyForwardingRoutes(string blockIdentifier, SideMarker enterSide)
        {
            if (string.IsNullOrEmpty(blockIdentifier)) return false;
            if (enterSide == SideMarker.None) return false;
            var leaveMarker = enterSide == SideMarker.Plus ? SideMarker.Minus : SideMarker.Plus;
            var possibleRoutes = RouteList.GetRoutesWithFromBlock(blockIdentifier, leaveMarker);
            return possibleRoutes.Count > 0;
        }

        /// <summary>
        /// Queries all available routes which are:
        /// (a) not occupied by any other locomotive which is cruising on a crossing route
        /// (b) where the final block is not occupied by any locomotive
        /// (c) ...
        /// </summary>
        /// <returns></returns>
        public Routes GetAllAvailableRoutes(Routes alreadyTakenRoutes)
        {
            var res = new Routes();

            //
            // a list of locomtives which are assigned to any block
            //
            var assignedLocKeys = Settings.Locomotives
                .Where(itLoc => !string.IsNullOrEmpty(itLoc.Key.AssignedToBlock))
                .Select(itLoc => itLoc.Key)
                .ToList();

            foreach (var assignedLoc in assignedLocKeys)
            {
                //
                // search for all routes from the block where the locomotive is assigned to
                //
                var currentBlock = assignedLoc.AssignedToBlock;
                var sideMarkerEnter = assignedLoc.EnterSide;
                var leaveMarker = sideMarkerEnter == LocomotiveEnterSide.Plus
                    ? SideMarker.Minus
                    : sideMarkerEnter == LocomotiveEnterSide.Minus
                        ? SideMarker.Plus
                        : SideMarker.None;

                // 
                // Is block in a stage?
                // If yes, is the staging block at the end of the queue (leaving side)?
                // In case the assignment is a leaving block of a staging
                // we alter the currentBlock value to the Stage name (the owner of the block itself).
                //
                var stagingBlock = Settings.FindStagingBlockByName(currentBlock);
                if (Settings.IsLeavingStageBlock(stagingBlock, out var stagingStart))
                    currentBlock = stagingStart.Identifier;

                var possibleRoutes = RouteList.GetRoutesWithFromBlock(currentBlock, leaveMarker);

                //
                // if possibleRoutes has zero routes
                //  [ ] check if block is a commuting block
                //  [ ] and if locomotive is allowed to commute
                //
                if (possibleRoutes.Count <= 0)
                {
                    var driverName = assignedLoc.DriverName;
                    var objectId = assignedLoc.ObjectId;

                    var locSettings = Settings.Locomotives.Keys.FirstOrDefault(it =>
                        it.DriverName == driverName && it.ObjectId == objectId);
                    if (locSettings != null && !locSettings.IsCommuter)
                    {
                        // error: commuting is not allowed for the locomotive
                        DataExchange.QueueDebugMessage(Uid, $"{locSettings.Selector}: no route found leaving {currentBlock}{leaveMarker.GetBlockMarker()}, commuting not allowed for this locomotive", DebugMessageT.Routes);
                        continue;
                    }

                    var oppositeLeaveMarker = leaveMarker.GetOpposite();
                    var blockOpposite = Settings.FindBlockByName(currentBlock + oppositeLeaveMarker.GetBlockMarker());
                    var blockOppositeExtras = blockOpposite as IBlockExtras;
                    if (blockOppositeExtras != null 
                        && oppositeLeaveMarker.IsPlus() 
                        && !blockOppositeExtras.IsCommuterAllowedPlus)
                        continue;
                    if(blockOppositeExtras != null
                       && oppositeLeaveMarker.IsMinus()
                       && !blockOppositeExtras.IsCommuterAllowedMinus)
                        continue;

                    possibleRoutes = RouteList.GetRoutesWithFromBlock(currentBlock, oppositeLeaveMarker);
                    possibleRoutes.CommutingRoutes = true;
                }

                foreach (var itRoute in possibleRoutes)
                {
                    // when target is a staging, we need access to the first internal block
                    // because the datamodels differs a little, and we have
                    // different access to the sensor information
                    Block stagingBlockFirst = null;

                    //
                    // check for any switch in maintenance mode
                    // if any is maintained the route is not usable
                    //
                    var gotoNextRoute = false;
                    foreach (var it in itRoute.Switches)
                    {
                        var x = it.x;
                        var y = it.y;

                        var planfieldItem = Planfield[$"{x}x{y}"];
                        if (planfieldItem == null) continue;

                        var sw = Settings.FindAccessoryByPlanId(planfieldItem.Identifier);
                        if (sw != null)
                        {
                            if (sw.IsMaintenaceEnabled)
                            {
                                // reason: Route can not be used because switch '{planfieldItem.Identifier}' is in maintenance mode.

                                gotoNextRoute = true;

                                break;
                            }
                        }
                    }
                    if (gotoNextRoute) continue;

                    RouteBlock targetBlock = null;
                    var searchingStage = false;

                    //
                    // check if the route targets a stage
                    // if yes, validate the first block of the stage
                    //    - is it free?
                    //      if yes, take it as the correct targetBlock
                    //      if not, continue, route is not usable
                    //
                    var targetBlockName = string.Empty;
                    if (itRoute.Blocks.Count >= 2)
                    {
                        targetBlockName = itRoute.Blocks[1]?.identifier ?? string.Empty;
                    }
                    else
                    {
                        //
                        // Dieser Teil sollte eine absolute Ausnahme sein und
                        // eigentlich nie ausgeführt werden. Per Definition
                        // MUSS jeder Route einen StartBlock/StartStage und
                        // EndBlock/EndStage haben. Eine Fahrt ist nur zwischen
                        // Blocks/Stages möglich. Wenn also eine Seite fehlt,
                        // dann ist während der Analyse schon etwas schief
                        // gelaufen -- wir versuchen dann über den Routenamen
                        // eine passende Information zu filtern, das ist
                        // aber mehr oder weniger nur eine krude Annahme.
                        //
                        // Workaround: finde den TargetBlock-Name basierend auf dem RouteName
                        // "name": "BK_2[+]_BK_Wendel_Innen[+]"
                        // Trennung bei ']_':
                        //   vorne Source
                        //   hinten Target

                        searchingStage = true;

                        var idx = itRoute.Name.IndexOf("]_", StringComparison.OrdinalIgnoreCase);
                        if (idx != -1)
                        {
                            //var vorne = itRoute.Name.Substring(0, idx - 2);
                            var hinten = itRoute.Name.Substring(idx + 2);

                            targetBlockName = hinten
                                .Trim()
                                .Replace("[+]", string.Empty)
                                .Replace("[-]", string.Empty);
                        }
                    }

                    var stage = Settings.FindStagingByName(targetBlockName);
                    if (stage?.Blocks != null)
                    {
                        //
                        // Wenn eine Stage keine Blöcke besitzt,
                        // dann kann diese Stage auch nicht angefahren werden.
                        //
                        if (stage.Blocks.Count <= 0) continue;

                        var firstBlockOfStage = stage.Blocks[0];
                        var idBlockOfStage = firstBlockOfStage.Identifier;
                        if (string.IsNullOrEmpty(idBlockOfStage)) continue;
                        // we have to check if any locomotive is assigned to this stage block
                        var isTargetStageBlockOccupied = IsAnyLocomotiveAssignedTo(idBlockOfStage, assignedLocKeys);
                        if (isTargetStageBlockOccupied) continue;

                        targetBlock = new RouteBlock
                        {
                            identifier = idBlockOfStage,
                            side = SideMarker.Plus,
                            start = itRoute.Blocks[1].start,
                            x = itRoute.Blocks[1].x,
                            y = itRoute.Blocks[1].y
                        };

                        // used to query sensors in later steps
                        stagingBlockFirst = firstBlockOfStage;
                    }

                    //
                    // an dieser Stelle haben wir wohl alles versucht
                    // wenn `searchingStage` auf true steht, dann waren
                    // vorher schon zu wenige Blocks bei der Route
                    // vorhanden und die nachfolgenden Codezeilen
                    // würden schon wegen NullPointerExceptions
                    // nicht funktionieren
                    //
                    if (targetBlock == null && searchingStage)
                        continue;

                    if (targetBlock == null)
                        targetBlock = itRoute.Blocks[1];
                    var targetBlockIdentifier = targetBlock.identifier;

                    var sourceBlock = Settings.FindBlockByName(itRoute.Blocks[0].identifier);
                    var sourceBlockDelay = 1;
                    var signalsToRedDelay = 15;
                    if (sourceBlock != null)
                    {
                        sourceBlockDelay = sourceBlock.StartDelay;
                        signalsToRedDelay = sourceBlock.SignalsToRedDelay;
                    }
                    var sourceBlockIdentifier = itRoute.Blocks[0].identifier;

                    //
                    // check if target block is occupied by any locomotive
                    //
                    var isTargetOccupied = IsAnyLocomotiveAssignedTo(targetBlock.identifier, assignedLocKeys);
                    if (isTargetOccupied) continue;
                    
                    //
                    // check if target block is reserved by any running route which has the same destination
                    //
                    if (alreadyTakenRoutes != null && alreadyTakenRoutes.Count > 0)
                    {
                        var isReserved = false;
                        foreach (var it in alreadyTakenRoutes)
                        {
                            if (it.TargetBlock.Equals(targetBlockIdentifier))
                            {
                                isReserved = true;
                                break;
                            }
                        }

                        if (isReserved) continue;
                    }

                    //
                    // check if route is already occupied by any other route which is taken for traveling
                    //
                    var takenRoutes = alreadyTakenRoutes.Count;
                    // if (takenRoutes > 0)
                    //     ;
                    var crossingRoutes = RouteList.GetCrossingRoutesOf(itRoute);
                    if (crossingRoutes.Count > 0)
                    {
                        var isAnyRouteOccupied = false;
                        foreach (var itCrossingRoute in crossingRoutes)
                        {
                            //
                            // do not check the root route
                            //
                            if (itCrossingRoute.Name.Equals(itRoute.Name)) continue;

                            var routeSettings = Settings.Routes.FirstOrDefault(it => it.Key.Name.Equals(itCrossingRoute.Name));
                            if (routeSettings.Key == null) continue;
                            isAnyRouteOccupied = routeSettings.Key.IsOccupied;
                            if (isAnyRouteOccupied) break;
                        }

                        if (isAnyRouteOccupied) continue;
                    }

                    //
                    // get the two assigned sensors of the final block
                    //
                    string sensorEnterName;
                    string sensorInName;
                    if (stagingBlockFirst == null)
                    {
                        var sensorsBlockMapping = Settings.BlockSensors.FirstOrDefault(it => it.Key.Identifier.Equals(targetBlock.Caption));
                        sensorEnterName = sensorsBlockMapping.Key?.SensorEnter ?? string.Empty;
                        sensorInName = sensorsBlockMapping.Key?.SensorIn ?? string.Empty;
                    }
                    else
                    {
                        // take the information from the staging block
                        sensorEnterName = stagingBlockFirst?.SensorEnter ?? string.Empty;
                        sensorInName = stagingBlockFirst?.SensorIn ?? string.Empty;
                    }

                    //
                    // get the information about the `driver` and `pin` (addressing) of the sensors
                    //
                    var sensorEnterDriver = Settings.Sensors.FirstOrDefault(it => it.Key.Name.Equals(sensorEnterName));
                    var sensorInDriver = Settings.Sensors.FirstOrDefault(it => it.Key.Name.Equals(sensorInName));

                    //
                    // query the locomotive entity assigned to the start block
                    //
                    var driverName = assignedLoc.DriverName;
                    var objectId = assignedLoc.ObjectId;
                    var locInventar = GetLocomotiveInventary(driverName, objectId);

                    //
                    // check if the locomotive is enabled
                    //
                    var locSettings = Settings.Locomotives.Keys.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId);
                    if (locSettings == null)
                    {
                        // TODO add warning; if we have no settings how can the auto mode run for this loc?!
                        continue;
                    }
                    if (!locSettings.IsEnabled) continue;
                    if (DateTime.Now < locSettings.EarlistTimeForNextTrip) continue;

                    // 
                    // check if the target length is long enough
                    //
                    var locLength = locSettings.Length;
                    if (locLength <= 0)
                    {
                        // error:=Locomotive {driverName}::{objectId} has no length, i.e. zero or less, and can not be used.
                        DataExchange.QueueDebugMessage(Uid, $"{locSettings.Selector}: no length set for locomotive, i.e. zero or less, and can not be used", DebugMessageT.Routes);
                        continue;
                    }

                    var checkLength0 = IsBlockLongEnough(targetBlock.Caption, locLength, out var blockLength);
                    if(!checkLength0)
                    {
                        // error:=Target block {targetBlockIdentifier} is not long enough for {driverName}::{objectId}. Block is {blockLength}, Locomotive is {locLength}. 
                        DataExchange.QueueDebugMessage(Uid, $"{locSettings.Selector}: target {targetBlockIdentifier} is not long enough, Block is {blockLength}, Locomotive is {locLength}", DebugMessageT.Routes);
                        continue;
                    }

                    //
                    // Is target block enabled?
                    //
                    if (!IsBlockEnabled(targetBlock.Caption))
                    {
                        // error:=Target block {targetBlockIdentifier} is not enabled and will not be used. 
                        DataExchange.QueueDebugMessage(Uid, $"{locSettings.Selector}: Target block {targetBlockIdentifier} is not enabled and will not be used", DebugMessageT.Routes);
                        continue;
                    }

                    //
                    // The following check is more complex.
                    // When target has no outgoing route, it should/must allow commuting.
                    // In this case only locomotives with commuter flag are allowed to enter.
                    //
                    // Das hier muss nur getestet werden, wenn das Ziel ein Block ist;
                    // denn wenn wir eine Stage erreichen, dann wird alles innerhalb der
                    // Stage durch eine eigene Logik behandelt.
                    //
                    if (stagingBlockFirst == null) // Ziel ist eine Stage, wenn ein Block einer Stage gesetzt ist.
                    {
                        var targetEnterSide = targetBlock.side;
                        var hasOutgoingRoute = HasBlockAnyForwardingRoutes(targetBlockIdentifier, targetEnterSide);
                        if (!hasOutgoingRoute)
                        {
                            // when locomotive is not allowed for commuting, go ahead...
                            if (!locSettings.IsCommuter)
                            {
                                DataExchange.QueueDebugMessage(Uid,
                                    $"{locSettings.Selector}: locomotive does not support commuting",
                                    DebugMessageT.Routes);
                                continue;
                            }

                            var targetBlockSettings = Settings.FindBlockByName(targetBlock.Caption) as IBlockExtras;
                            if (targetBlockSettings == null) continue;
                            //
                            // we support distinguish [+] and [-] for commuting
                            // 
                            if (targetEnterSide == SideMarker.Plus)
                                if (!targetBlockSettings.IsCommuterAllowedPlus)
                                {
                                    // error: Target block does not support entering on PLUS [+] side for commuting.
                                    DataExchange.QueueDebugMessage(Uid,
                                        $"{locSettings.Selector}: Target block {targetBlockIdentifier} does not support entering on PLUS [+] side for commuting",
                                        DebugMessageT.Routes);
                                    continue;
                                }

                            if (targetEnterSide == SideMarker.Minus)
                                if (!targetBlockSettings.IsCommuterAllowedMinus)
                                {
                                    // error: Target block does not support entering on MINUS [-] side for commuting.
                                    DataExchange.QueueDebugMessage(Uid,
                                        $"{locSettings.Selector}: Target block {targetBlockIdentifier} does not support entering on MINUS [-] side for commuting",
                                        DebugMessageT.Routes);
                                    continue;
                                }
                        }
                    }


                    // Prüfe leave / enter Seite der Route
                    // und schau ob die Lok am Ziel
                    // virtuell für die Ansich gedreht
                    // werden muss.
                    bool targetBlockFlip;
                    if (true)
                    {
                        var bStart = itRoute.Blocks[0];
                        var bTarget = itRoute.Blocks[1];
                        targetBlockFlip = bStart.side == bTarget.side;
                    }

                    //
                    // create route info
                    //
                    var routeInfo = new RouteData(DataProviders)
                    {
                        Locomotive = locInventar,
                        IsCommuting = possibleRoutes.CommutingRoutes,
                        RouteName = itRoute.Name,
                        SignalsToRedDelay = signalsToRedDelay,
                        SourceBlockDelay = sourceBlockDelay,
                        SourceBlock = sourceBlockIdentifier,
                        SourceLeavingSide = itRoute.Blocks[0].side == SideMarker.Plus
                            ? LocomotiveEnterSide.Plus
                            : LocomotiveEnterSide.Minus,
                        TargetBlock = targetBlockIdentifier,
                        TargetBlockFlip = targetBlockFlip,
                        TargetEnterSide = targetBlock.side == SideMarker.Plus
                            ? LocomotiveEnterSide.Plus
                            : LocomotiveEnterSide.Minus,
                        IsSimulationMode = IsSimulationMode
                    };

                    const string z21Identifier = "z21";

                    if (sensorEnterDriver.Key != null)
                    {
                        routeInfo.SensorEnter.DataProvider = GetDataProviderByName(sensorEnterDriver.Key.Provider);

                        if (sensorEnterDriver.Key.Provider.Equals(z21Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            // Module:Pin
                            routeInfo.SensorEnter.ModulePin = sensorEnterDriver.Key.Address;
                        }
                        else
                        {
                            // Index
                            routeInfo.SensorEnter.Pin = int.Parse(sensorEnterDriver.Key.Address);
                        }
                    }

                    if (sensorInDriver.Key != null)
                    {
                        routeInfo.SensorIn.DataProvider = GetDataProviderByName(sensorInDriver.Key.Provider);
                        
                        if (sensorInDriver.Key.Provider.Equals(z21Identifier, StringComparison.OrdinalIgnoreCase))
                        {
                            // Module:Pin
                            routeInfo.SensorIn.ModulePin = sensorInDriver.Key.Address;
                        }
                        else
                        {
                            // Index
                            routeInfo.SensorIn.Pin = int.Parse(sensorInDriver.Key.Address);
                        }
                    }

                    //
                    // add crossing route names to route info
                    //
                    foreach (var itRouteCross in crossingRoutes.Where(itRouteCross => !itRouteCross.Name.Equals(itRoute.Name)))
                        routeInfo.RoutesCrossing.Add(itRouteCross.Name);

                    routeInfo.PrepareSensorMonitoring();

                    res.Add(routeInfo);
                }
            }

            return res;
        }

        /// <summary>
        /// Queries all locomotives assigned to any block of `stage`.
        /// </summary>
        /// <param name="stage"></param>
        /// <returns></returns>
        private List<libMetamodel.Settings.Locomotive> GetLocomotivesInStage(Staging stage)
        {
            var assignedLocKeys = Settings.Locomotives
                .Where(itLoc => !string.IsNullOrEmpty(itLoc.Key.AssignedToBlock))
                .Select(itLoc => itLoc.Key)
                .ToList();
            var assignedToStages = new List<libMetamodel.Settings.Locomotive>();
            var stageBlockIdentifiers = stage.BlockIdentifiers;
            foreach (var it in assignedLocKeys)
            {
                if (string.IsNullOrEmpty(it.AssignedToBlock)) continue;
                if (stageBlockIdentifiers.Contains(it.AssignedToBlock))
                    assignedToStages.Add(it);
            }

            return assignedToStages;
        }

        /// <summary>
        /// Iterates over all available stages and combines the assigned locomotives
        /// which are assigned to any block of these stages.
        /// </summary>
        /// <returns></returns>
        private List<libMetamodel.Settings.Locomotive> GetLocomotivesInStages()
        {
            var resAll = new List<libMetamodel.Settings.Locomotive>();

            foreach (var it in Settings.Stagings)
            {
                var inStage = GetLocomotivesInStage(it.Key);
                resAll.AddRange(inStage);
            }

            return resAll;
        }

        public struct StageIdentifier
        {
            public StageIdentifier(string data)
            {
                try
                {
                    if (!string.IsNullOrEmpty(data))
                    {
                        var idx = data.LastIndexOf("_", StringComparison.OrdinalIgnoreCase);
                        ParentId = data.Substring(0, idx).TrimEnd('_');
                        if (int.TryParse(data.Substring(idx).Trim('_'), out var idc))
                            StageIdx = idc;
                    }
                }
                catch
                {
                    // ignore
                }
            }

            public string ParentId { get; set; } = string.Empty;
            public int StageIdx { get; set; } = 0;
        }

        public StageRoutes GetAllAvailableStageRoutes(StageRoutes alreadyTakenRoutes)
        {
            var res = new StageRoutes();

            var assignedLocs = GetLocomotivesInStages();
            var blocksWithAssignments = assignedLocs.Select(it => it.AssignedToBlock).ToList();

            foreach (var assignedLoc in assignedLocs)
            {
                //
                // check the earlist time to run of the loc
                //
                var driverName = assignedLoc.DriverName;
                var objectId = assignedLoc.ObjectId;
                var locSettings = Settings.Locomotives.Keys.FirstOrDefault(it => it.DriverName == driverName && it.ObjectId == objectId);
                if (locSettings == null)
                {
                    // TODO add warning; if we have no settings how can the auto mode run for this loc?!
                    continue;
                }
                if (DateTime.Now < locSettings.EarlistTimeForNextTrip) continue;

                // the format is: "parentId_stageIdx"
                // e.g. "AB_5_0", "AB_5_1", ...
                // i.e. "AB_5" := parentId, 1 := stageIdx
                // stageIdx are 0-based
                var currentBlock = assignedLoc.AssignedToBlock;
                var stageIdentifier = new StageIdentifier(currentBlock);
                
                // (a) check if the assignedLoc is at the last position
                //     if yes, do not move in this business loic
                //     if no, we need to check if the next stage is free
                // (b)    if yes, the locomotive can cruise
                //        if not, the locomotive will stand still in the current stage

                var parentStaging = Settings.FindStagingByName(stageIdentifier.ParentId);
                var noOfStages = parentStaging.Blocks.Count;

                // (a)
                if (stageIdentifier.StageIdx >= noOfStages - 1) continue;

                // (b)
                var nextBlockInStage = $"{stageIdentifier.ParentId}_{stageIdentifier.StageIdx + 1}";
                if (blocksWithAssignments.Contains(nextBlockInStage)) continue;

                //
                // check if target block exists
                //
                var targetBlock = Settings.FindStagingBlockByName(nextBlockInStage);
                if (targetBlock == null) continue;

                //
                // check if target block is reserved by any running route which has the same destination
                //
                if (alreadyTakenRoutes != null && alreadyTakenRoutes.Count > 0)
                {
                    var isReserved = false;

                    foreach (var it in alreadyTakenRoutes)
                    {
                        if (it.TargetBlock.Equals(nextBlockInStage))
                        {
                            isReserved = true;
                            break;
                        }
                    }

                    if (isReserved) continue;
                }

                //
                // get the two assigned sensors of the target block
                //
                var sensorEnterName = targetBlock.SensorEnter ?? string.Empty;
                var sensorInName = targetBlock.SensorIn ?? string.Empty;

                //
                // get the information about the `driver` and `pin` (addressing) of the sensors
                //
                var sensorEnterDriver = Settings.Sensors.FirstOrDefault(it => it.Key.Name.Equals(sensorEnterName));
                var sensorInDriver = Settings.Sensors.FirstOrDefault(it => it.Key.Name.Equals(sensorInName));

                //
                // query the locomotive entity assigned to the start block
                //
                var locInventar = GetLocomotiveInventary(driverName, objectId);

                //
                // create stage info
                //
                var stageInfo = new StageData(DataProviders)
                {
                    Locomotive = locInventar,
                    TargetBlock = nextBlockInStage,
                    TargetEnterSide = LocomotiveEnterSide.Plus, // not relevant, but used as default
                    IsSimulationMode = IsSimulationMode
                };

                if (sensorEnterDriver.Key != null)
                {
                    stageInfo.SensorEnter.DataProvider = GetDataProviderByName(sensorEnterDriver.Key.Provider);
                    stageInfo.SensorEnter.Pin = int.Parse(sensorEnterDriver.Key.Address);
                }

                if (sensorInDriver.Key != null)
                {
                    stageInfo.SensorIn.DataProvider = GetDataProviderByName(sensorInDriver.Key.Provider);
                    stageInfo.SensorIn.Pin = int.Parse(sensorInDriver.Key.Address);
                }

                stageInfo.PrepareSensorMonitoring();

                res.Add(stageInfo);
            }

            return res;
        }
    }
}

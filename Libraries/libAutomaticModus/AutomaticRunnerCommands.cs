// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus.pods;
using libAutomaticModus.Running;
using libMetamodel.Settings;
using libShared;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;

namespace libAutomaticModus
{
    internal class AutomaticRunnerCommands(
        AutomaticRunner ctx,
        IDataExchange dataExchange,
        string uid)
    {
        private AutomaticData _automaticData;

        public void ApplyAutomaticData(AutomaticData data)
        {
            _automaticData = data;
        }

        public void ProcessSpeed(string driverName, int objectId, int targetSpeed, int currentSpeed)
        {
            if (targetSpeed == currentSpeed) return;
            if (targetSpeed < 0) return;

            ctx.OnActionTriggered(new ActionSpeedstep
            {
                DriverName = driverName,
                ObjectId = objectId,
                Speed = targetSpeed
            });
        }

        public void ChangeDirection(string driverName, int objectId, Locomotive locomotive)
        {
            var direction = locomotive.Entity.Direction == LocomotiveDirection.Forward
                ? (int)LocomotiveDirection.Backward // change to Backwward
                : (int)LocomotiveDirection.Forward; // change to Forward

            ctx.OnActionTriggered(new ActionDirection
            {
                DriverName = driverName,
                ObjectId = objectId,
                Direction = direction
            });
        }

        /// <summary>
        /// This method must be called when a route selection has been canceled.
        /// For example when accesories became not ready after several seconds.
        /// </summary>
        /// <param name="routeData"></param>
        /// <returns></returns>
        public async Task KeepAssignment(RouteData routeData)
        {
            var driverName = routeData.Locomotive.DataProvider.Name;
            var objectId = routeData.Locomotive.Entity.ObjectId;

            var sourceBlock = routeData.SourceBlock;

            // assign locomotive back to source block
            _automaticData.Settings.AssignLocomotiveToBlock(driverName, objectId, sourceBlock, routeData.Locomotive.Entity);
            var settingsLoc = _automaticData.Settings.FindLocomotiveBy(driverName, objectId);

            //
            // set a walltime for the locomotive
            // it is not allowed to cruise again before the walltime is reached
            //
            var waitSeconds = settingsLoc.MinimumBlockWait;
            settingsLoc.EarlistTimeForNextTrip = DateTime.Now + TimeSpan.FromSeconds(waitSeconds);

            var waitSecondsAfterError = settingsLoc.MinimumBlockWaitAfterError;
            settingsLoc.EarlistNextTimeAfterError = DateTime.Now + TimeSpan.FromSeconds(waitSecondsAfterError);

            Logging.Log.Info($"Next earlist run: {settingsLoc.EarlistTimeForNextTrip}");
            Logging.Log.Info($"Next earlist run after error: {settingsLoc.EarlistNextTimeAfterError}");

            await _automaticData.Settings.Save();
            await dataExchange.SendSettingsToClients(uid);
        }

        /// <summary>
        /// In this method the locomotive of the route is reassigned,
        /// i.e. when the final Block is reached the locomotive
        /// will be assigned to the final Block.
        /// In addition the new enter side is saved.
        /// </summary>
        /// <param name="routeData"></param>
        /// <returns></returns>
        public async Task ReassignLocomotive(RouteData routeData)
        {
            var driverName = routeData.Locomotive.DataProvider.Name;
            var objectId = routeData.Locomotive.Entity.ObjectId;

            var targetBlock = routeData.TargetBlock;
            var targetBlockEnter = routeData.TargetEnterSide;

            //
            // the target block is reached, the target block gets the recently cruised locomotive assigned
            // in addition apply the enter side information which is needed to find the next route
            //
            _automaticData.Settings.AssignLocomotiveToBlock(driverName, objectId, targetBlock, routeData.Locomotive.Entity);
            var settingsLoc = _automaticData.Settings.FindLocomotiveBy(driverName, objectId);
            if (targetBlockEnter == LocomotiveEnterSide.Plus)
            {
                settingsLoc.EnterSide = LocomotiveEnterSide.Plus;
            }
            else if (targetBlockEnter == LocomotiveEnterSide.Minus)
            {
                settingsLoc.EnterSide = LocomotiveEnterSide.Minus;
            }

            //
            // flip visualization in case
            //
            if (routeData.TargetBlockFlip)
            {
                var currentOrientation = settingsLoc.Orientation;
                var previousOrientation = settingsLoc.OrientationPrevious;
                var doFlip = currentOrientation.Equals("right", StringComparison.OrdinalIgnoreCase);
                if (doFlip)
                {
                    if (previousOrientation.Equals(currentOrientation))
                        doFlip = false;
                }

                if (doFlip)
                {
                    settingsLoc.OrientationPrevious = settingsLoc.Orientation;
                    settingsLoc.Orientation = GetFlippedOrientation(settingsLoc.Orientation);
                }
                else
                {
                    settingsLoc.Orientation = settingsLoc.OrientationPrevious;
                    settingsLoc.OrientationPrevious = "none";
                }
            }

            //
            // set a walltime for the locomotive
            // it is not allowed to cruise again before the walltime is reached
            //
            var waitSeconds = settingsLoc.MinimumBlockWait;
            settingsLoc.EarlistTimeForNextTrip = DateTime.Now + TimeSpan.FromSeconds(waitSeconds);

            var waitSecondsAfterError = settingsLoc.MinimumBlockWaitAfterError;
            settingsLoc.EarlistNextTimeAfterError = DateTime.Now + TimeSpan.FromSeconds(waitSecondsAfterError);

            Logging.Log.Info($"Next earlist run: {settingsLoc.EarlistTimeForNextTrip}");

            await _automaticData.Settings.Save();
            await dataExchange.SendSettingsToClients(uid);
        }

        private static string GetFlippedOrientation(string orientation)
        {
            if (string.IsNullOrEmpty(orientation)) return "none";
            if (orientation.Equals("left", StringComparison.OrdinalIgnoreCase)) return "right";
            return "left";
        }

        public async Task FinalizeRoute(RouteData routeData)
        {
            await SetHighlight(routeData.RouteName, false);
            await ResetLocomotiveForBlockFinal(routeData.TargetBlock);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="routeName"></param>
        /// <param name="state"></param>
        /// <param name="afterPlanfieldLoad">Used to delay route visualization during restore (i.e. after page reload).
        /// The planfield controls need images to provide route visualization. In case the restore
        /// ist called to early there will be no image to alter.</param>
        /// <returns></returns>
        public async Task SetHighlight(
            string routeName,
            bool state,
            bool afterPlanfieldLoad = false)
        {
            if (string.IsNullOrEmpty(routeName)) return;

            if (state)
            {
                var routeItem = _automaticData.RouteList.GetByName(routeName);
                var routeObject = JObject.FromObject(routeItem);
                if (afterPlanfieldLoad)
                    routeObject["afterPlanfieldLoad"] = true;
                var data = new JObject
                {
                    { "command", "automode" },
                    { "argument", "visualizeRoute" },
                    { "argumentValue", routeObject }
                };
                await dataExchange.SendObjectToAllClients(uid, data);
            }
            else
            {
                var routeItem = _automaticData.RouteList.GetByName(routeName);
                var data = new JObject
                {
                    { "command", "automode" },
                    { "argument", "unvisualizeRoute" },
                    { "argumentValue", JObject.FromObject(routeItem) }
                };
                await dataExchange.SendObjectToAllClients(uid, data);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="locomotive"></param>
        /// <param name="blockName"></param>
        /// <param name="afterPlanfieldLoad">Used to delay route visualization during restore (i.e. after page reload).
        /// The planfield controls need images to provide route visualization. In case the restore
        /// ist called to early there will be no image to alter.</param>
        /// <returns></returns>
        public async Task SetLocomotiveForBlockFinal(
            libShared.Entities.ILocomotive locomotive,
            string blockName,
            bool afterPlanfieldLoad = false)
        {
            var locObject = new JObject
            {
                { "blockIdentifier", blockName },
                { "locomotiveEntity", JObject.FromObject(locomotive) }
            };

            if (afterPlanfieldLoad)
                locObject["afterPlanfieldLoad"] = true;

            var data = new JObject
            {
                {"command", "automode"},
                {"argument", "setDestination"},
                {"argumentValue", locObject}
            };
            await dataExchange.SendObjectToAllClients(uid, data);
        }

        public async Task ResetLocomotiveForBlockFinal(string blockName)
        {
            if (string.IsNullOrEmpty(blockName)) return;

            var data = new JObject
            {
                {"command", "automode"},
                {"argument", "resetDestination"},
                {"argumentValue", blockName}
            };
            await dataExchange.SendObjectToAllClients(uid, data);
        }

    }
}

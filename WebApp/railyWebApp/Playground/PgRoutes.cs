// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libTrackplan.Analyzer;
using libUserspace;
using libUtilities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using libEsuEcos.Entities;
using libShared;
// ReSharper disable RedundantAssignment

namespace railyWebApp.Playground
{

    public class PgRoutes
    {
        public class PrepareRouteResult
        {
            public bool Result { get; internal set; }
            public List<string> ErrorMessages { get; internal set; } = new();
        }

        /// <summary>
        /// This method prepares a route and triggers all switches to the right state
        /// to let the selected locomotive cruise to its final destination.
        /// </summary>
        /// <returns></returns>
        internal static async Task<PrepareRouteResult> PrepareRoute(
            string routeIdentifier,
            Workspace userWorkspace,
            WebSocket requestor = null,
            IDataExchange dataExchange = null)
        {
            var resInstance = new PrepareRouteResult();

            var route = userWorkspace.Metamodel.GetRoute(routeIdentifier);
            if (route == null)
            {
                resInstance.Result = false;
                resInstance.ErrorMessages.Add($"Keine passende Route '{routeIdentifier}' gefunden.");
                return resInstance;
            }

            foreach (var itSwitch in route.Switches)
            {
                //
                // am Anfang holen wir uns die Settings
                //
                var switchPlanItem = userWorkspace.Metamodel.Planfield.Get(itSwitch.x, itSwitch.y);
                var switchPlanIdentifier = switchPlanItem.Identifier;
                var accInfo = userWorkspace.Metamodel.Settings.Accessories.FirstOrDefault(it => it.Key.PlanfieldControlIdentifier.Equals(switchPlanIdentifier)).Key;
                if (accInfo == null) continue;

                var doCommandInvert = accInfo.Invert;

                var targetState = itSwitch.Switch.State;
                var targetStateIndexArr = itSwitch.Switch.GetStateIndex();
                if (targetStateIndexArr[0] == -1) continue;

                var dpAcc = DataProviderManager.GetDataProviderByName(userWorkspace.Uid, accInfo.AccessoryDriver);
                if (dpAcc == null) continue;

                var entities = dpAcc.Entities as IReadOnlyCollection<IEntity>;
                var accEntity = entities?.FirstOrDefault(it =>
                    it.Type == EntityType.Accessory
                    && it.Name0.Equals(accInfo.AccessoryIdentifier, StringComparison.OrdinalIgnoreCase));

                int targetStateIndex = 0;
                if (targetStateIndexArr.Length == 1)
                {
                    //
                    // normales Schaltverhalten
                    //
                    if (doCommandInvert == false)
                    {
                        targetStateIndex = targetStateIndexArr[0];
                    }
                    else
                    {
                        //
                        // Schaltverhalten umkehren, invertieren
                        // Stand Mai, 2025
                        // Wir erlauben das Invertieren nur von einfachen Weichen,
                        // Signalen, Schaltern, also mit zwei Status; daher
                        // ist das Invertieren ein einfaches Prüfen von 0 und 1.
                        //
                        targetStateIndex = targetStateIndexArr[0] == 0 ? 1 : 0;

                        // store new state
                        if (dpAcc is IEntityInverter dbInverter)
                            dbInverter.SetAccessoryState(accEntity, targetStateIndex);
                    }
                }
                else
                {
                    var binary = $"{targetStateIndexArr[0]}{targetStateIndexArr[1]}";
                    targetStateIndex = Convert.ToInt32(binary, 2);
                }

                //
                // provide log/debug information
                //
                string debugMsg;
                if (accEntity is IAccessory acc)
                {
                    debugMsg = $"Entity: {acc.DisplayName} -> {acc.ObjectId}, {acc.State} change to {targetState}";
                    Logging.Log.Debug(debugMsg);
                }
                else
                {
                    debugMsg = $"Entity: {accEntity?.DisplayName ?? "invalid"} -> {accEntity?.ObjectId ?? -1}, change to {targetState}";
                    Logging.Log.Debug(debugMsg);
                }

                dataExchange?.QueueDebugMessage(userWorkspace.Uid, debugMsg, DebugMessageT.Accessories);

                var r = await __changeAccessory(dpAcc, accEntity, targetStateIndex, dataExchange, userWorkspace.Uid);
                if (!r)
                    resInstance.ErrorMessages.Add($"- Fehler beim Setzen der Weiche '{switchPlanIdentifier}'.");
            }

            return resInstance;
        }

        internal static async Task<bool> __changeAccessory(
            IDataProvider dp,
            IEntity accEntity,
            int targetStateIndex,
            IDataExchange dataExchange,
            string uid)
        {
            //
            // ESU ECoS
            //
            if (dp.Type.HasFlag(DataProviderType.ECoS50210)
                || dp.Type.HasFlag(DataProviderType.Z21))
            {
                var cmd = new JObject
                {
                    {"driverName", dp.Name},
                    {"objectId", accEntity?.ObjectId ?? -1},
                    {"command", "update"},
                    {"argument", "targetState"},
                    {"argumentValue", targetStateIndex}
                };

                await PgHelper.SendToGateway(uid, cmd);
            }
            //
            // Demo
            // 
            else if (dp.Type.HasFlag(DataProviderType.Demo))
            {
                if (accEntity is Accessory accDemo)
                {
                    var res = await PgDemo.HandleAccessory(accDemo, targetStateIndex, null, dataExchange, uid);

                    return res;

                }
            }

            return true;
        }

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
                case "routing" when PgHelper.IsArgument(cmddata, "function", "analyzeRoutes"):
                    {
                        Logging.Log.Debug($"Analyze routes...");

                        var themeData = userWorkspace.Theme;
                        var planfield = userWorkspace.Metamodel.Planfield;

                        var analyzer = new Analyze(planfield, themeData);
                        var analyzerResult = analyzer.Execute(async void (step, maxSteps) =>
                        {
                            try
                            {
                                ShowProgress(step, maxSteps, "Analyzing routes...");
                                await SendProgressToClients(uid, step, maxSteps, "Analyzing routes...");
                            }
                            catch (Exception ex)
                            {
                                Logging.ExceptionLog(ex);
                            }
                        });

                        Logging.Log.Debug($"Found {analyzerResult.NumberOfRoutes} routes.");
                        await PgHelper.SendDebugToClient(uid, $"Found {analyzerResult.NumberOfRoutes} routes.");

                        var res = await userWorkspace.Metamodel.ApplyRoutes(analyzerResult);
                        if (res)
                        {
                            var data0 = new JObject
                            {
                                ["command"] = "update",
                                ["routes"] = userWorkspace.Metamodel.Routes
                            };

                            var clients = ConnectionManager.GetConnection(uid);
                            var wsBrowsers = clients.BrowserSockets;
                            foreach (var ws in wsBrowsers)
                                await WebSocketModule.DataExchange.SendWs(ws, data0);
                        }

                        return true;
                    }

                case "routing" when PgHelper.IsArgument(cmddata, "check"):
                    {
                        var argumentValue = cmddata.GetString("argumentValue");
                        var jsonValue = JObject.Parse(argumentValue);
                        var routeIdentifier = jsonValue.GetString("name");

                        var prepareResult = await PrepareRoute(routeIdentifier, userWorkspace, requestor, dataExchange);
                        if (prepareResult != null && prepareResult.ErrorMessages.Any())
                            Logging.Log.Debug($"Check of route '{routeIdentifier}' failed: {string.Join(", ", prepareResult.ErrorMessages)}");
                    }
                    return true;
            }

            return false;
        }

        private static async Task SendProgressToClients(string uid, int step, int maxStep, string msg)
        {
            await PgHelper.SendDebugToClient(uid, $"{msg} {(int)(step / (float)maxStep * 100.0)}%");
        }

        private static void ShowProgress(int step, int maxStep, string msg)
        {
            Logging.Log.Debug($"{msg} {(int)(step / (float)maxStep * 100.0)}%");
        }
    }
}

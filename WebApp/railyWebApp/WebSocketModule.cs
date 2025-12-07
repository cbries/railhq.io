// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus;
using libShared;
using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libUserspace;
using libUtilities;
using libZ21;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebApp.Controller.Automation.EventStream;
using railyWebApp.DataProvider;
using railyWebApp.EntityLogger;
using railyWebApp.Playground;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable UsePatternMatching
// ReSharper disable RedundantAssignment
// ReSharper disable RedundantDefaultMemberInitializer
// ReSharper disable UnusedMember.Local
// ReSharper disable FunctionNeverReturns
// ReSharper disable InlineOutVariableDeclaration
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Local

namespace railyWebApp
{
    public class WebSocketModule
    {
        //
        // Websocket can be tested with:
        //   wscat -c ws://localhost:5001/ws/browser
        //   wscat -c ws://localhost:5001/ws/controller
        //

        public static SupabaseService Supabase { get; set; }

        /// <summary>
        /// This URI is used by Desktop WebBrowsers like Chrome or Vivaldi.
        /// Via this WebSocket connection the full set of data is transfered.
        /// </summary>
        public static string WsBrowserUri = "/ws/browser";

        /// <summary>
        /// This URI is used by handhelds and small devices with displays to
        /// allow customers to control locomotives directly.
        /// </summary>
        public static string WsRemoteControlUri = "/ws/remotecontrol";

        /// <summary>
        /// This URI is used by Gateway clients on customer side.
        /// Only state information by the control stations are transfered,
        /// and commands to control the remote model railyway.
        /// </summary>
        public static string WsControllerUri = "/ws/controller";

        internal static readonly IDataExchange DataExchange = new DataExchange();

        #region BaseDir for Workspaces

        private static string _baseDirWorkspaces;

        public static string BaseDirWorkspaces
        {
            get
            {
                if (!string.IsNullOrEmpty(_baseDirWorkspaces)) return _baseDirWorkspaces;
                var resDir = libUtilities.Filesystem.GetResourcesPath();
                _baseDirWorkspaces = Path.Combine(resDir, Workspace.SubdirWorkspace);
                return _baseDirWorkspaces;
            }
        }

        #endregion

        internal static async Task HandleWsRequest(HttpContext context, bool isBrowser, bool isHandheld)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                var socket = await context.WebSockets.AcceptWebSocketAsync();
                Logging.Log.Debug($"New connection: {context.Connection.RemoteIpAddress}");
                await HandleWebSocketAsync(socket, context, isBrowser, isHandheld);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private static Dictionary<string, string> GetHttpParameter(HttpContext ctx)
        {
            var res = new Dictionary<string, string>();
            var query = ctx.Request.Query;
            foreach (var it in query)
            {
                if (res.ContainsKey(it.Key)) continue;
                if (StringValues.IsNullOrEmpty(it.Value)) continue;
                res.Add(it.Key, it.Value);
            }
            return res;
        }

        private static async Task<string> ReadFullMessageAsync(WebSocket socket, byte[] buffer)
        {
            if (socket.State != WebSocketState.Open)
                return string.Empty;

            using MemoryStream ms = new MemoryStream();

            try
            {
                WebSocketReceiveResult result;

                do
                {
                    if (socket.State != WebSocketState.Open)
                        break; // Abbrechen, wenn der WebSocket nicht mehr offen ist

                    result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.Count > 0)
                        ms.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);
            }
            catch (WebSocketException)
            {
                // ignore
            }
            catch (Exception)
            {
                // ignore
            }

            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private static async Task HandleWebSocketAsync(WebSocket socket, HttpContext ctx, bool isBrowser, bool isHandheld)
        {
            const int noOfMegabytes = 2;
            var buffer = new byte[noOfMegabytes * 1024 * 1024];
            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text)
            {
                //
                // At this point we verify the user of the new WebSocket connection.
                // Two scenarios are possible:
                // 1) the user provides "username" and "password", if available these values are used to check against Supbase-Auth
                // 2) the user is redirected by the management server, in this case a session is filled with Supabase-AUTH-token
                //
                // HINWEIS: Diese Anwendung ist für den lokalen Netzwerkbetrieb konzipiert.
                // Bei fehlenden Anmeldedaten wird ein anonymer lokaler Benutzer verwendet.
                //

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);

                var authenticationToken = string.Empty;
                var resWsToken = HasValidToken(message, out var uid, out var email, out authenticationToken, out var authenticationError1);
                if (!resWsToken)
                {
                    var resSession = HasValidSession(ctx, out uid, out email, out authenticationToken, out var authenticationError2);
                    if (!resSession)
                    {
                        var res = HasValidUid(message, out uid, out email, out authenticationToken, out var authenticationError3);
                        if (!res)
                        {
                            // Simplified auth for local network usage:
                            // Instead of rejecting unauthenticated connections, use anonymous local user
                            uid = Controller.Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;
                            email = Controller.Automation.RailhqAuthenticationFilter.AuthUserName;
                            authenticationToken = "local-token";
                            Logging.Log.Info($"WebSocket: Using anonymous local user for unauthenticated connection");
                        }
                    }
                }

                Logging.Log.Info($"New connection: {uid} ({email}) (IsBrowser: {isBrowser}, IsHandheld: {isHandheld})");

                if (!isBrowser)
                {
                    // Note: System notifications were previously stored in Supabase.
                    // This functionality has been removed as we no longer use Supabase.
                    Logging.Log.Info($"New Gateway connection: {uid} ({email})");
                }

                var clientConnection = ConnectionManager.AddConnection(uid, socket, isBrowser);
                var clientDataProviders = DataProviderManager.Apply(uid);

                //
                // inform the customer/controller about a valid login
                // IMPORTANT: at this point because `uid` is linked to the previously asssigned connection manager instance
                //
                if (true) // scoping
                {
                    var gatewayCommand = new JObject
                    {
                        {"command", "auth"},
                        {"argument", "ok"},
                        {"argumentValue", authenticationToken}
                    };

                    await PgHelper.SendToGateway(uid, gatewayCommand);
                }

                //
                // an dieser Stelle kennen wir alle Predefined-Lokomotiven und -Schaltartikel
                // für die z21 können wir die Kommandos zum Abfragen der Lokomotiven 
                // erstellen und an das Gateway schicken, dieser gibt das dann einfach 
                // an die z21 weiter; es ist auch sinnvoll dies erst zu tun, wenn der
                // Anwender authentifiziert ist
                // 
                if (DataProviderManager.GetDataProvider(uid, DataProviderType.Z21).FirstOrDefault()
                    is libZ21.DataProvider.DataProvider dpZ21FirstInit)
                {
                    var entities = dpZ21FirstInit.Entities as IReadOnlyCollection<IEntity>;
                    if (entities != null && entities.Count > 0)
                    {
                        var payload = new Payload();
                        foreach (var it in entities)
                        {
                            if (it.Type != EntityType.Locomotive) continue;
                            if (it.ObjectId < 0) continue;
                            var bytes = Z21.GetLocomotiveInfoCommand(it.ObjectId);
                            payload.AddBytes(bytes);
                        }
                        var gatewayCommand = BaseCommands.GetRelayCommand(libZ21.Globals.Z21Identifier, payload);
                        await PgHelper.SendToGateway(uid, gatewayCommand);
                    }
                }

                if (!Globals.UserWorkspaces.TryGetValue(uid, out _))
                {
                    try
                    {
                        var workspace = new Workspace(uid, ctx, Supabase);
                        Globals.UserWorkspaces.TryAdd(uid, workspace);
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex, "TODO workspace handling");
                    }
                }

                DataConsumer dataConsumer = null;

                if (!Globals.RegisteredDataConsumer.TryGetValue(uid, out var dataConsumerRefresh))
                {
                    try
                    {
                        dataConsumer = new DataConsumer();
                        dataConsumer.LocomotiveUpdated += (_, locomotive) =>
                        {
                            LocomotiveUpdate.LocomotiveUpdateHandler(uid, locomotive, DataExchange);
                        };
                        dataConsumer.AccessoryUpdated += (_, accessory) =>
                        {
                            AccessoryUpdate.LocomotiveUpdateHandler(uid, accessory, DataExchange);
                        };
                        dataConsumer.Subscribe(clientDataProviders, clientConnection);
                        Globals.RegisteredDataConsumer.TryAdd(uid, dataConsumer);
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex, "TODO validate multi-thread access");
                    }
                }
                else
                {
                    dataConsumerRefresh.UnSubscribe(clientDataProviders);
                    dataConsumerRefresh.Subscribe(clientDataProviders, clientConnection);
                    dataConsumer = dataConsumerRefresh;
                }

                // Ziel: Zugriff auf den HqEventService im aktuellen HTTP-Kontext.
                // Dieser Service dient als zentrales Event-Broadcast-System für alle Sensorereignisse 
                // und kann z. B. an ein Frontend weitergeleitet werden (z. B. via Server-Sent Events).
                var eventService = ctx.RequestServices.GetService(typeof(IHqEventService)) as IHqEventService;
                if (dataConsumer != null)
                    dataConsumer.ApplyEventService(uid, eventService);

                //
                // Playground: provide data for browser 
                //
                // THIS IS JUST THE INITIALIZATION
                // THE REAL UPDATES ARE PROVIDED WITH `dataConsumer.Subscribe(..)` (see above!)
                //
                if (isBrowser)
                {
                    // (1) provide locomotives
                    // (2) provide theme data           
                    // (3) provide planfield
                    // (4) provide accessories
                    // (5) provide sensor state
                    // (6) provide routes

                    var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
                    if (!wsres)
                    {
                        // TODO handle error

                        return;
                    }

                    // Aktuelles State-Objekt zurücksetzen,
                    // damit neue Clients über den aktuellen
                    // Status informiert werden, u.a. über
                    // den Automodus
                    userWorkspace?.ResetRecentStateObject();

                    // 
                    // init automatic
                    //
                    userWorkspace.AutomaticRunner ??= new AutomaticRunner(
                        DataExchange,
                        uid);
                    //
                    // restore previous automode runs
                    //
                    await userWorkspace.AutomaticRunner.Restore();

                    var userWorkspaceRequest = "demo";
                    var pars = GetHttpParameter(ctx);
                    if (pars.TryGetValue("workspace", out var par))
                        userWorkspaceRequest = par.CleanString();

                    // in case a recent workspace is already loaded
                    // and a new workspace name is provided
                    // we will close the recent workspace
                    // closing is only allowed for Browser, not for handhelds
                    if (!userWorkspace.Name.Equals(userWorkspaceRequest, StringComparison.Ordinal))
                    {
                        // in case the loaded workspace differs from the requested workspace
                        // we will cancel the connection and inform the customer
                        if (isHandheld)
                        {
                            var errMsg = $"({userWorkspaceRequest})";
                            await DataExchange.SendFatal(socket, Globals.HTTP_403_FORBIDDEN, errMsg, "invalid workspace");
                            return;
                        }

                        if (userWorkspace.IsLoaded && userWorkspace.Unload())
                        {
                            // inform all web clients that the current workspace has changed
                            // on client-side we have to inform the customer about the change
                            // maybe we should reload automatically on client-side
                            // in addition the unloaded project should stop to work
                            // all trains must stop, etc.
                            // large topic to think about

                            var errMsg = $"The workspace changed from {userWorkspace.Name} to {userWorkspaceRequest}. Your session is not valid anymore. Please check your desktop browser for any information. This session will be closed now.";
                            Logging.Log.Info(errMsg);
                            var data1 = new JObject
                            {
                                ["command"] = "fatal",
                                ["info"] = errMsg
                            };
                            await DataExchange.SendObjectToAllClients(uid, data1, [socket]);
                            await DataExchange.CloseClients(uid, "workspace changed", [socket]);
                        }
                    }

                    const string cmdInitialization = "initialization";

                    //
                    // create base Workspace directory / database when not existing
                    //
                    var workspaceInfos = await libUserspace.Info.Workspaces.GetInfo(BaseDirWorkspaces, uid);
                    var workspaceNames = libUserspace.Info.Workspaces.GetWorkspaceNames(workspaceInfos);
                    if (!workspaceNames.Contains(userWorkspaceRequest))
                    {
                        var res1 = await userWorkspace.CreateDefaultWorkspace(uid, userWorkspaceRequest);
                        if (!res1)
                        {
                            // TODO handle error during workspace creation
                        }
                        else
                        {
                            // update workspace list
                            var wsInfoInstance = new libUserspace.Info.Workspaces(BaseDirWorkspaces);
                            await wsInfoInstance.Update(uid);
                        }
                    }

                    var overviewDataproviders = DataProviderManager.GetOverviewOfDataProviders(uid);

                    var workspaceIsLoaded = userWorkspace.IsLoaded;
                    if (workspaceIsLoaded)
                    {
                        //userWorkspace.TriggerStateUpdateToClients();
                    }
                    else
                    {
                        var loadres = await userWorkspace.LoadOrCreate(
                            userWorkspaceRequest,
                            uid,
                            overviewDataproviders);
                        if (!loadres)
                        {
                            var m = $"Workspace({userWorkspaceRequest}) does not load.";
                            Logging.Log.Warn(m);
                            DataExchange?.QueueDebugMessage(uid, m);

                            return;
                        }

                        userWorkspace.StateUpdated += async (_, jsonStateObject) =>
                        {
                            await DataExchange.SendObjectToAllClients(uid, new JObject
                            {
                                ["command"] = "updateState",
                                ["stateData"] = jsonStateObject
                            });
                        };
                        userWorkspace.StartStateInformer();
                        userWorkspace.TriggerStateUpdateToClients();
                    }

                    //
                    // (1) locomotive (see __handleOnMessage())
                    //         jsonData.command := initialization    (the real ECoS data, first connection)
                    //         jsonData.command := update            (the real ECoS data)
                    // (4) accessories
                    // (5) sensor states (only in `update`)
                    //
                    if (true)
                    {
                        var dpEcos = DataProviderManager.GetDataProvider(uid, DataProviderType.ECoS50210);
                        var dpEcos0 = dpEcos.FirstOrDefault() ?? EmptyDataProvider.Empty;

                        var dpZ21 = DataProviderManager.GetDataProvider(uid, DataProviderType.Z21);
                        var dpZ210 = dpZ21.FirstOrDefault() ?? EmptyDataProvider.Empty;

                        var dpDemo = DataProviderManager.GetDataProvider(uid, DataProviderType.Demo);
                        var dpDemo0 = dpDemo.FirstOrDefault() ?? EmptyDataProvider.Empty;

                        var obj0 = PgDpHelper.__getDataFromDp(dpEcos0);
                        var obj1 = PgDpHelper.__getDataFromDp(dpZ210);
                        var obj2 = PgDemo.__getDemoData(dpDemo0);
                        obj2.Merge(obj0, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Union
                        });
                        obj2.Merge(obj1, new JsonMergeSettings
                        {
                            MergeArrayHandling = MergeArrayHandling.Union
                        });

                        var locomotivesData = new JObject
                        {
                            ["command"] = cmdInitialization,
                            ["railyData"] = obj2
                        };

                        await DataExchange.SendWs(socket, locomotivesData);
                    }

                    //
                    // Handheld browser do not need theme information in the moment.
                    //
                    if (!isHandheld)
                    {
                        var themeData = userWorkspace?.Theme?.Data;
                        var jsonObjTheme = new JObject
                            {
                                { "command", cmdInitialization },
                                { "themeName", themeData?.ThemeName ?? "RailwayEssentials" },
                                { "themeData", JArray.FromObject(themeData?.ThemeItems) }
                            };

                        await DataExchange.SendWs(socket, jsonObjTheme);
                    }

                    //
                    // At this point we distinguish between different kind of web clients.
                    // Handheld browser will get a small set of meta information.
                    // Desktop browser will get ALL meta information.
                    //
                    var data = new JObject
                    {
                        ["command"] = cmdInitialization,
                        ["workspace"] = new JObject
                        {
                            ["name"] = userWorkspace.Name
                        },
                        ["systemInfo"] = userWorkspace.Metamodel.SystemInfo
                    };

                    if (!isHandheld)
                    {
                        data["planfield"] = JObject.FromObject(userWorkspace.Metamodel.Planfield);
                        data["routes"] = userWorkspace.Metamodel.Routes;
                    }

                    await DataExchange.SendWs(socket, data);

                    //
                    // send settings like locomotive assignments after
                    // the planfield data, otherwise it could happen that
                    // the assignment are not shown
                    //
                    data = new JObject
                    {
                        ["command"] = cmdInitialization,
                        ["settings"] = JObject.FromObject(userWorkspace.Metamodel.Settings)
                    };
                    await DataExchange.SendWs(socket, data);

                    //
                    // test environment
                    // testing route visualization
                    // testing target block assignment
                    // 
#if DEBUG
                    //                    __startTestingRoutines(uid, userWorkspace);
#endif
                }
            }

            while (result.CloseStatus == null)
            {
                if (socket.State == WebSocketState.Closed) break;
                if (socket.State == WebSocketState.CloseReceived) break;
                if (socket.State == WebSocketState.CloseSent) break;
                if (socket.State == WebSocketState.Aborted) break;

                var message = await ReadFullMessageAsync(socket, buffer);
                if (string.IsNullOrEmpty(message)) continue;

                var uid = ConnectionManager.GetUidOf(socket);
                if (string.IsNullOrEmpty(uid)) continue;

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    if (!string.IsNullOrEmpty(uid))
                        ConnectionManager.RemoveSocket(socket);

                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                // 
                // handle Ping messages
                //
                if (HandlePing(message, out var response))
                {
                    try
                    {
                        if (!string.IsNullOrEmpty(response))
                            await DataExchange.SendWs(socket, response);
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex);
                    }

                    continue;
                }

                //
                // Check for debug messages...
                //
                if (HasValidDebugMessage(message, out var debugMessage))
                {
                    try
                    {
                        await DataExchange.SendObjectToAllClients(uid, JObject.FromObject(debugMessage));
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex);
                    }

                    continue;
                }

                //
                // update data providers
                //
                if (HasValidRequest(message, out var request))
                {
                    try
                    {
                        var extensionName = request.ExtensionName;

                        //
                        // Part for all browser instances.
                        // This region handles incoming commands/settings, etc. which
                        // are used to control and configure the modellrailway.
                        // Most (nearly all commands) should be generally, to avoid
                        // specific implementation on browser side.
                        //
                        if (extensionName.Equals("webClient", StringComparison.Ordinal))
                        {
                            var resWebClient = await HandleWebRequestAsync(uid, request, socket, DataExchange);
                            if (resWebClient) continue;
                        }

                        // Wir müssen den DataProvidern mitgeben, ob sich 
                        // der Anwender im Simulationsmodus befindet.
                        // Dann kann der jeweilige Datenprovider 
                        // entscheiden ob die empfangenden "echten"
                        // Daten verwendet werden sollen.
                        var customerActivatedSimulation = false;
                        var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
                        if (wsres && userWorkspace != null)
                            customerActivatedSimulation = userWorkspace.SimulationEnabled;

                        //
                        // This call is responsible to handle any request.
                        // It checks the request type and calls the 
                        // responsible DataTypeProvider itself.
                        //
                        var res = DataProviderManager.Update(uid, request, customerActivatedSimulation);
                        if (!res)
                        {
                            Logging.Log.Debug($"Unknown extension: {extensionName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logging.ExceptionLog(ex);
                    }
                }

                // ...
            }

            if (socket.State != WebSocketState.Closed)
            {
                // Falls CloseStatus einen Wert hat, verwenden wir diesen.
                if (result.CloseStatus.HasValue)
                {
                    await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
                }
                else
                {
                    // Falls kein CloseStatus gesetzt ist, verwenden wir einen normalen Schließstatus.
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, result.CloseStatusDescription, CancellationToken.None);
                }
            }

        }

        public static async Task<bool> HandleWebRequestAsync(
            string uid,
            Request request,
            WebSocket requestor = null,
            IDataExchange dataExchange = null
            )
        {
            var resWebClient = await PgTrackdata.HandleWebClientRequests(uid, request, requestor, DataExchange);
            if (resWebClient)
            {
                await DataExchange.SendSettingsToClients(uid);
                return true;
            }

            resWebClient = await PgSystem.HandleWebClientRequests(uid, request, requestor);
            if (resWebClient) return true;

            resWebClient = await PgControlStation.HandleWebClientRequests(uid, request, requestor);
            if (resWebClient) return true;

            // handle "demo" dataprovider 
            resWebClient = await PgDemo.HandleWebClientRequests2(uid, request, requestor, DataExchange);
            if (resWebClient) return true;
            resWebClient = await PgDemo.HandleWebClientRequests(uid, request, requestor, DataExchange);
            if (resWebClient) return true;

            resWebClient = await PgRoutes.HandleWebClientRequests(uid, request, requestor, DataExchange);
            if (resWebClient) return true;

            resWebClient = await PgAutoMode.HandleWebClientRequests(uid, request, requestor, DataExchange);
            if (resWebClient)
            {
                await DataExchange.SendSettingsToClients(uid);
                return true;
            }

            resWebClient = await PgSettings.HandleWebClientRequests(uid, request);
            if (resWebClient)
            {
                await DataExchange.SendSettingsToClients(uid);
                return true;
            }

            resWebClient = await PgInventar.HandleWebClientRequests(uid, request);
            if (resWebClient)
            {
                await DataExchange.SendSettingsToClients(uid);
                return true;
            }

            return false;
        }

        private static bool HasValidSession(
            HttpContext ctx,
            out string uid,
            out string email,
            out string authenticationToken,
            out string authenticationError
        )
        {
            uid = string.Empty;
            email = string.Empty;
            authenticationToken = string.Empty;
            authenticationError = string.Empty;

            if (Supabase == null)
            {
                authenticationError = "authentication service not available";
                return false;
            }

            if (ctx?.Session == null)
            {
                authenticationError = "http context or session not available";
                return false;
            }

            var session = ctx.Session;
            var sharedAuthToken = string.Empty;

            if (session.TryGetValue(SessionGlobals.AuthUserSession, out var var1))
                sharedAuthToken = Encoding.UTF8.GetString(var1);

            if (string.IsNullOrEmpty(sharedAuthToken))
                return false;
            
            var user = Supabase.Validate(sharedAuthToken).Result;
            if (user != null)
            {
                try
                {
                    uid = user.Id;
                    email = user.Email;
                    authenticationToken = sharedAuthToken;
                    return true;
                }
                catch (Exception ex)
                {
                    authenticationError = ex.Message;
                }
            }

            return false;
        }

        private static bool HasValidToken(
            string initialWsMessage,
            out string uid,
            out string email,
            out string authenticationToken,
            out string authenticationError)
        {
            uid = string.Empty;
            email = string.Empty;
            authenticationToken = string.Empty;
            authenticationError = string.Empty;

            if (string.IsNullOrEmpty(initialWsMessage)) return false;

            try
            {
                var jsonObj = JObject.Parse(initialWsMessage);
                var method = jsonObj.GetString("type");
                var token = jsonObj.GetString("token");

                if (!method.Equals("auth", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(token))
                {
                    authenticationError = "invalid authentication format";
                    return false;
                }

                var user = Supabase.Validate(token).Result;
                if (user != null)
                {
                    try
                    {
                        uid = user.Id;
                        email = user.Email;
                        authenticationToken = token;
                        return true;
                    }
                    catch (Exception ex)
                    {
                        authenticationError = ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                authenticationError = ex.Message;
            }

            return false;
        }

        private static bool HasValidUid(
            string initialWsMessage,
            out string uid,
            out string email,
            out string authenticationToken,
            out string authenticationError)
        {
            uid = string.Empty;
            email = string.Empty;
            authenticationToken = string.Empty;
            authenticationError = string.Empty;

            if (string.IsNullOrEmpty(initialWsMessage)) return false;
            if (HasValidRequest(initialWsMessage, out var request))
            {
                try
                {
                    var jsonObject = request.Data.Payload as JObject;

                    var username = jsonObject?.GetString("username");
                    var password = jsonObject?.GetString("password");

                    if (string.IsNullOrEmpty(username))
                    {
                        authenticationError = "username not set";
                        return false;
                    }

                    if (string.IsNullOrEmpty(password))
                    {
                        authenticationError = "password not set";
                        return false;
                    }

                    if (Supabase == null)
                    {
                        authenticationError = "authentication service not available";
                        return false;
                    }

                    var session = Supabase.Login(username, password).Result;
                    if (session?.User == null)
                    {
                        authenticationError = "authentication failed";
                        return false;
                    }

                    uid = session.User.Id;
                    email = session.User.Email;
                    authenticationToken = session.AccessToken;

                    return true;
                }
                catch (Exception ex)
                {
                    authenticationError = ex.Message;
                    Logging.ExceptionLog(ex);
                }
            }

            return false;
        }

        private static bool HasValidDebugMessage(string jsonMessage, out DebugMessage debugMessage)
        {
            debugMessage = new DebugMessage();

            if (string.IsNullOrEmpty(jsonMessage)) return false;

            try
            {
                // fast test
                var o = JObject.Parse(jsonMessage);
                if (o["extensionName"] != null)
                    return false;
            }
            catch
            {
                // ignore
            }

            try
            {
                var req = JsonConvert.DeserializeObject<DebugMessage>(jsonMessage);
                if (req == null) return false;
                if (req.Messages.Count == 0) return false;
                debugMessage = req;
                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, $"Invalid debug message");
            }

            return false;
        }

        private static bool HandlePing(string jsonMessage, out string response)
        {
            response = string.Empty;

            if (string.IsNullOrEmpty(jsonMessage)) return false;

            try
            {
                using var doc = JsonDocument.Parse(jsonMessage);

                var root = doc.RootElement;

                if (root.TryGetProperty("c", out var cProp) && cProp.GetString() == "ping")
                {
                    long? tValue = null;
                    if (root.TryGetProperty("t", out var tProp) && tProp.TryGetInt64(out var t))
                    {
                        tValue = t;
                    }

                    // Neues JSON zusammensetzen
                    var responseObj = new Dictionary<string, object>
                    {
                        ["c"] = "ping",
                        ["tt"] = true
                    };

                    if (tValue.HasValue)
                    {
                        responseObj["t"] = tValue.Value;
                    }

                    response = System.Text.Json.JsonSerializer.Serialize(responseObj);

                    return true;
                }
            }
            catch
            {
                // ignore
            }

            return false;
        }

        private static bool HasValidRequest(string jsonMessage, out Request request)
        {
            request = new Request();

            if (string.IsNullOrEmpty(jsonMessage)) return false;

            try
            {
                var req = JsonConvert.DeserializeObject<Request>(jsonMessage);
                if (req == null) return false;
                request = req;
                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, $"Invalid Request by {request.ExtensionName}");
            }

            return false;
        }
    }
}

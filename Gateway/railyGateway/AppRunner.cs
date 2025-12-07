// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos;
using libInterop;
using libShared.ExchangeProtocol;
using libUtilities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyGateway.Cfg;
using railyGateway.WebApp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using libShared.MessageProtocol;
using libZ21;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

namespace railyGateway
{
    internal class AppRunner : IRailyHostCallbacks
    {
        public event EventHandler Authenticated;

        #region IRailyHostCallbacks

        /// <summary>
        /// Dieser Handler sendet die Nachrichten von den Extensions weiter an den `railhq.io - Controller`.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        public void MessageReceived(IRailyExtension sender, string message)
        {
            if (!State.IsOk())
            {
                Logging.Log.Debug($"Controller connection NOT established. Message from '{sender.Name}' not forwarded: {message.Trim()}");
                return;
            }

            var prefix = $"<received from {sender.Name}>";

            if (!message.IsJsonObject())
            {
                Logging.Log.Debug($"{prefix} but no JSON object received.");
            }
            else
            {
                var jsonObject = message.ToJsonObject();
                var extensionName = sender.Name;
                var req = RequestFactory.CreateRequest(
                    extensionName,
                    jsonObject
                );
                var reqJson = JsonConvert.SerializeObject(req);
                ServerConnection?.SendMessage(reqJson);
            }
        }

        public void ErrorRaised(IRailyExtension sender, string errorMessage)
        {
            var prefix = $"ErrorRaised ({sender.Name})";

            if (!errorMessage.IsJsonObject())
            {
                Logging.Log.Debug($"{prefix} but no JSON object received.");
            }
            else
            {
                var jsonObject = errorMessage.ToJsonObject();
                Logging.Log.Info($"{prefix} -> {jsonObject}");
            }
        }

        public bool IsConnectionToControllerEstablished()
        {
            return State.IsOk();
        }

        #endregion

        public WebSocketClient ServerConnection { get; private set; }

        private ICfgService _cfgService;
        private bool _isStopped;

        private AccessoryInitializer _accessoryInitializer;

        public static GatewayState State { get; private set; } = new();

        private async Task InitServerConnection(ICfgService cfgService)
        {
            if (cfgService != null)
                _cfgService = cfgService;

            if (!IsControllerConnectionEnabled()) return;

            ServerConnection = new WebSocketClient(cfgService);
            ServerConnection.ConnectFailed += ServerConnectionOnConnectFailed;
            ServerConnection.ConnectionEstablished += ServerConnectionOnConnectionEstablished;
            ServerConnection.MessageReceived += ServerConnectionOnMessageReceived;

            await Task.Run(async () =>
            {
                _isStopped = false;

                await CheckReconnect();
            });
        }

        private void ServerConnectionOnConnectFailed(object sender, EventArgs e)
        {
            _isStopped = false;

            _ = CheckReconnect();
        }

        private bool IsControllerConnectionEnabled()
        {
            if (_cfgService == null) return false;
            var cfgObject = _cfgService.GetConfig();
            if (!cfgObject.Connection.IsEnabled)
            {
                Logging.Log.Info("Connection to railhq.io server is disabled.");
                return false;
            }

            return true;
        }

        private async Task CheckReconnect()
        {
            if (!IsControllerConnectionEnabled()) return;

            if (State.IsAuthIssue())
            {
                Logging.Log.Fatal($"Please verify the username and password on the configuration page. The recent connection encountered authentication issues, and the automatic reconnection has been aborted.");

                return;
            }

            const int delaySec = 10;
            var controllerHost = _cfgService?.GetConfig()?.Connection?.Host ?? string.Empty;
            var controllerTimeout = _cfgService?.GetConfig()?.Connection?.TimeoutSeconds ?? 5;

            while (!_isStopped && !State.IsAuthIssue())
            {
                if (string.IsNullOrEmpty(controllerHost))
                {
                    Logging.Log.Info($"Invalid railhq.io Controller configuration. No hostname/ip set.");
                    await Task.Delay(TimeSpan.FromSeconds(delaySec));
                }

                var isPingOk = NetworkInfo.IsPingOk(_cfgService?.GetConfig()?.Connection?.Host, true);
                if (isPingOk)
                {
                    Logging.Log.Info($"Ping ok, try to connect to {controllerHost}.");
                    break;
                }

                Logging.Log.Info($"Ping to {controllerHost} failed. Try to ping again in {delaySec} seconds.");

                if (!IsControllerConnectionEnabled()) return;

                controllerHost = _cfgService?.GetConfig()?.Connection?.Host ?? string.Empty;
                controllerTimeout = _cfgService?.GetConfig()?.Connection?.TimeoutSeconds ?? 5;

                await Task.Delay(TimeSpan.FromSeconds(delaySec));
            }

            _isStopped = false;

            System.Diagnostics.Debug.WriteLine($"Try to connect to Controller at {ServerConnection.ServerUri}...");

            await ServerConnection.ConnectAsync(TimeSpan.FromSeconds(controllerTimeout));
        }

        private void ServerConnectionOnConnectionClosed(object sender, EventArgs e)
        {
            ServerConnection.ConnectionClosed -= ServerConnectionOnConnectionClosed;

            var wsClient = sender as WebSocketClient;
            var targetUri = wsClient?.ServerUri ?? "unknown";
            Logging.Log.Info($"Connection to {targetUri} closed.");

            State.Reset();

            //
            // Wenn eine Verbindung abbricht, dann sollen alle Züge und
            // Aktionen beendet werden, so dass es zu keinem Schaden kommen sollte.
            //
            StopExtensions();

            //
            // Wenn eine Verbindung zum Controller abgebrochen wurde/beendet wurde, 
            // dann soll daraufhin kontinuierlich versucht werden sich 
            // neu zum Controller zu verbinden. 
            //
            _isStopped = false;
            _ = CheckReconnect();
        }

        private void ServerConnectionOnConnectionEstablished(object sender, EventArgs e)
        {
            ServerConnection.ConnectionClosed += ServerConnectionOnConnectionClosed;

            State.IsConnected = true;

            SendAuthentication(sender as IWebSocketClient);
        }

        public void SendAuthentication(IWebSocketClient wsToController)
        {
            if (!State.IsConnected) return;

            var cfg = wsToController?.CfgService?.GetConfig();
            var username = cfg?.Connection.Username ?? string.Empty;
            var password = cfg?.Connection?.Password ?? string.Empty;

            // 
            // Prüfe ob von aussen Benutzerdaten gesetzt sind, so dass
            // diese für die Authetifizierung genutzt werden.
            //
            var envUsername = Environment.GetEnvironmentVariable("RAILHQ_USERNAME");
            var envPassword = Environment.GetEnvironmentVariable("RAILHQ_PASSWORD");
            if (!string.IsNullOrWhiteSpace(envUsername) && !string.IsNullOrWhiteSpace(envPassword))
            {
                username = envUsername;
                password = envPassword;
            }

            Logging.Log.Info($"Sending authentication for {username}.");

            var req = RequestFactory.CreateRequest(
                "gateway",
                new JObject
                {
                    {"username", username},
                    {"password", password}
                }
            );
            var reqJson = JsonConvert.SerializeObject(req);

            var wsClient = wsToController as WebSocketClient;
            wsClient?.SendMessage(reqJson);
        }

        private void ServerConnectionOnMessageReceived(object sender, string msgFromController)
        {
            if (string.IsNullOrEmpty(msgFromController)) return;

            try
            {
                // TODO
                // Check where the received object ids exist, i.e. which command station.
                // Creates correct control commands and send them to the correct command station.
                // Currently we just support ESU ECoS which makes the distinguish very easy.
                // TODO

                var jsonObject = JsonConvert.DeserializeObject<MessageFromServer>(msgFromController);
                var cmd = jsonObject?.Command?.Trim();
                if (cmd == null)
                    throw new Exception("Invalid command received");

                //
                // The controller must send `initialize` to enable the message routing between 
                // the gateway and controller, otherwise messages from the hardware
                // will tried to be send but will fail consequently, it this 
                // will be as waste of resources.
                //
                if (cmd.Equals("initialize", StringComparison.OrdinalIgnoreCase))
                {
                    var argumentValue = jsonObject.ArgumentValue as JObject;
                    var initViews = argumentValue?.GetBool("initViews") ?? true;
                    var initAccessories = argumentValue?.GetBool("initAccessories") ?? false;

                    var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
                    foreach (var itService in extensions)
                    {
                        if (itService == null) continue;
                        Logging.Log.Info($"Enabling {itService.Name} for use!");

                        itService.EnableHandling();
                        if (initViews)
                            itService.InitViews();
                    }

                    // 
                    // init all accessories
                    // Sollte im Hintergrund laufen, als Task/Thread.
                    // Die jeweiligen Schaltvorgänge werden dann automatisch an die Clients gesendet.
                    //
                    if (initAccessories)
                    {
                        #region ecos, z21

                        var ecosArr = argumentValue[libEsuEcos.Globals.EsuEcosIdentifier] as JArray;
                        var entities = ecosArr?.ToObject<List<AccessoryInitEntity>>();

                        var z21Arr = argumentValue[libZ21.Globals.Z21Identifier] as JArray;
                        var z21Entities = z21Arr?.ToObject<List<AccessoryInitEntity>>();

                        if (z21Entities != null)
                            entities?.AddRange(z21Entities);

                        var extensions2 = Services.ServiceProvider.GetServices<IRailyExtension>();
                        _accessoryInitializer ??= new AccessoryInitializer(
                            extensions2.ToList(),
                            entities,
                            s =>
                        {
                            var msg = new DebugMessage { Messages = [s.Trim()] };
                            var obj = JsonConvert.SerializeObject(msg, Formatting.None);
                            ServerConnection?.SendMessage(obj);
                        });

                        #endregion

                        if (!_accessoryInitializer.IsRunning())
                            _accessoryInitializer.Start();
                    }

                    return;
                }

                //
                // emergency stop, for example when the customer leaves the planfield
                //
                if (cmd.Equals("emergencyStop", StringComparison.OrdinalIgnoreCase))
                {
                    StopExtensions();

                    return;
                }

                //
                // The controller informs us about the login state.
                // We receive the available authentication token.
                // TODO let us think about to share the token, if needed
                //
                if (cmd.Equals("auth", StringComparison.OrdinalIgnoreCase))
                {
                    State.AuthenticateState = jsonObject.Argument;
                    State.AuthenticateToken = jsonObject.ArgumentValue.ToString();
                    State.IsAuthenticated = true;
                    Logging.Log.Info($"Authentication is {State.AuthenticateState}");
                    Authenticated?.Invoke(this, EventArgs.Empty);
                    return;
                }

                if (cmd.Equals("fatal", StringComparison.OrdinalIgnoreCase))
                {
                    var errorInfo = JsonConvert.DeserializeObject<ErrorFromServer>(msgFromController);
                    if (errorInfo.Info.Code == GatewayState.HTTP_401_UNAUTHORIZED)
                    {
                        State.RecentErrorCode = GatewayState.HTTP_401_UNAUTHORIZED;

                        // Wenn wir uns falsch am Controller anmelden, dann 
                        // kann auch ruhig die Verbindung schließen.
                        Logging.Log.Info($"Connection to Controller: {errorInfo.Info.Message}");
                        ServerConnection.Close();
                        return;
                    }
                }

                var driverName = jsonObject.DriverName;
                if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier, StringComparison.OrdinalIgnoreCase))
                    if (!HandleEcosForwardCommands(jsonObject))
                        Logging.Log.Error($"Message from Controller: {msgFromController}");

                if (driverName.Equals(libZ21.Globals.Z21Identifier, StringComparison.OrdinalIgnoreCase))
                    if (!HandleZ21ForwardCommands(jsonObject))
                        Logging.Log.Error($"Message from Controller: {msgFromController}");
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
                Logging.Log.Error($"{msgFromController}");
            }
        }

        /// <summary>
        /// Leitet die Kommandos des Controllers an die z21 weiter!
        /// </summary>
        /// <param name="msgFromController"></param>
        /// <returns></returns>
        private bool HandleZ21ForwardCommands(MessageFromServer msgFromController)
        {
            var cmds = new List<byte[]>();
            var cmd = msgFromController.Command?.Trim();

            if (cmd == null) return false;
            if (!cmd.StartsWith("update", StringComparison.Ordinal)
                && !cmd.StartsWith("relay", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            switch (msgFromController.Argument)
            {
                case "relay":
                    {
                        var s = msgFromController.ArgumentValue.ToString();
                        var payload = JsonConvert.DeserializeObject<Payload>(s);
                        var bytesCmds = payload.GetEncodedCommands();
                        cmds.AddRange(bytesCmds);
                    }
                    break;

                case "speedstep": // locomotive
                    {
                        var o = msgFromController.ArgumentValue as JObject;
                        var speed = o.GetInt("speed", -1);

                        // DCC14, DCC28, DCC128, MM14, MM28
                        var maxSpeedStep = o.GetString("maxSpeedSteps");

                        // 0 | 1
                        var direction = o.GetInt("direction");

                        var bytes = Z21.SetLocomotiveDriveCommand(
                            msgFromController.ObjectId,
                            speed,
                            direction == 1 ? 0x00 : 0x80,
                            LokFahrstufen.ProtocolNameToMaxSpeedsteps(maxSpeedStep)
                        );

                        cmds.Add(bytes);
                    }
                    break;

                case "function": // locomotive
                    {
                        var array = msgFromController.ArgumentValue as JArray;
                        var fncIdx = (array?[0]?.Value<int>()) ?? -1;
                        var fncState = (array?[1]?.Value<int>()) ?? 0;

                        var bytes = Z21.SetLocomotveFunctionCommand(
                            msgFromController.ObjectId,
                            (byte)fncState,
                            (byte)fncIdx
                        );

                        cmds.Add(bytes);
                    }
                    break;

                case "direction": // locomotive
                    {
                        var o = msgFromController.ArgumentValue as JObject;
                        // DCC14, DCC28, DCC128, MM14, MM28
                        var maxSpeedStep = o.GetString("maxSpeedSteps");
                        // 0 | 1
                        var direction = o.GetInt("direction");

                        var bytes = Z21.SetLocomotiveDriveCommand(
                            msgFromController.ObjectId,
                            0,
                            direction == 1 ? 0x00 : 0x80,
                            LokFahrstufen.ProtocolNameToMaxSpeedsteps(maxSpeedStep)
                        );

                        cmds.Add(bytes);
                    }
                    break;

                case "targetState": // accessory
                    {
                        var sIdx = msgFromController.ArgumentValue?.ToString();
                        if (string.IsNullOrEmpty(sIdx)) return false;
                        var addrIndex = int.Parse(sIdx);
                        var bytes = Z21.LAN_X_SET_TURNOUT_Command(
                            msgFromController.ObjectId,
                            addrIndex == 1 ? true : false,
                            true,
                            true);
                        cmds.Add(bytes);
                    }
                    break;
            }

            var z21Ext = Services.Get(libZ21.Globals.Z21Identifier);
            if (z21Ext != null)
            {
                var payload = z21Ext.CreatePayload();
                cmds.ForEach(it => payload.AddBytes(it));
                var jsonPayload = JsonConvert.SerializeObject(payload);
                z21Ext.ProvideMessageToExtension(jsonPayload);
            }

            return true;
        }

        /// <summary>
        /// Leitet die Kommandos des Controllers an die ECoS weiter!
        /// </summary>
        /// <param name="msgFromController"></param>
        /// <returns></returns>
        private bool HandleEcosForwardCommands(MessageFromServer msgFromController)
        {
            var cmds = new List<string>();
            var cmd = msgFromController.Command.Trim();

            var noForce = cmd.IndexOf("NoForce", StringComparison.OrdinalIgnoreCase) != -1;
            if (!msgFromController.Command.StartsWith("update", StringComparison.Ordinal)) return false;

            switch (msgFromController.Argument)
            {
                case "request":
                    {
                        cmds.Add($"request({msgFromController.ObjectId}, control, force)");
                    }
                    break;

                case "release":
                    {
                        cmds.Add($"release({msgFromController.ObjectId}, control)");
                    }
                    break;

                case "speedstep": // locomotive
                    {
                        var o = msgFromController.ArgumentValue as JObject;
                        var messageSpeedstep = o?.ToObject<MessageSpeedstep>();
                        if (messageSpeedstep != null)
                        {
                            if (!noForce)
                                cmds.Add($"request({msgFromController.ObjectId}, control, force)");
                            cmds.Add($"set({msgFromController.ObjectId}, speedstep[{messageSpeedstep.Speed}])");
                            if (!noForce)
                                cmds.Add($"release({msgFromController.ObjectId}, control)");
                            cmds.Add($"get({msgFromController.ObjectId}, speed, speedstep)");
                        }
                    }
                    break;

                case "function": // locomotive
                    {
                        if (msgFromController.ArgumentValue is JArray { Count: 2 } arrValue)
                        {
                            cmds.Add($"request({msgFromController.ObjectId}, control, force)");
                            cmds.Add($"set({msgFromController.ObjectId}, func[{arrValue[0]}, {arrValue[1]}])");
                            cmds.Add($"release({msgFromController.ObjectId}, control)");
                            cmds.Add($"get({msgFromController.ObjectId}, func)");
                        }
                    }
                    break;

                case "direction": // locomotive
                    {
                        if (!noForce)
                            cmds.Add($"request({msgFromController.ObjectId}, control, force)");
                        cmds.Add($"set({msgFromController.ObjectId}, dir[{msgFromController.ArgumentValue}])");
                        if (!noForce)
                            cmds.Add($"release({msgFromController.ObjectId}, control)");
                        cmds.Add($"get({msgFromController.ObjectId}, dir)");
                    }
                    break;

                case "targetState": // accessory
                    {
                        //cmds.Add($"request({jsonObject.ObjectId}, control, force)");
                        //cmds.Add($"set({jsonObject.ObjectId}, state[{jsonObject.ArgumentValue}])");
                        //cmds.Add($"release({jsonObject.ObjectId}, control)");
                        //cmds.Add($"get({jsonObject.ObjectId}, state)");

                        var cmdList = CommandFactory.CreateAccessoryTargetState(
                            msgFromController.ObjectId,
                            msgFromController.ArgumentValue.ToString());

                        cmds.AddRange(cmdList);
                    }
                    break;

                case "power": // ECoS power: GO | STOP | SHUTDOWN
                    {
                        cmds.Add($"request({msgFromController.ObjectId}, control, force)");
                        cmds.Add($"set({msgFromController.ObjectId}, status[{msgFromController.ArgumentValue}])");
                        cmds.Add($"release({msgFromController.ObjectId}, control)");
                        cmds.Add($"get({msgFromController.ObjectId}, status)");
                    }
                    break;
            }

            var ecosExt = Services.Get(libEsuEcos.Globals.EsuEcosIdentifier);
            if (ecosExt != null)
            {
                var payload = ecosExt.CreatePayload();
                payload.AddCommands(cmds);
                var jsonPayload = JsonConvert.SerializeObject(payload);
                ecosExt.ProvideMessageToExtension(jsonPayload);
            }

            return true;
        }

        public List<Task> RunningExtensions { get; private set; } = new();

        private void InitExtensions(ICfgService cfgService)
        {
            var cfgCnt = cfgService.GetCfgContent();
            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
                ext?.Initialize(this, cfgCnt);

            Authenticated -= OnAuthenticated;
            Authenticated += OnAuthenticated;
        }

        private void OnAuthenticated(object sender, EventArgs e)
        {
            Logging.Log.Debug($"Gateway is authenticated");
            Logging.Log.Debug($"Trigger extensions for state update...");

            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
                ext?.InitViews();
        }

        //
        // emergency stop for all extensions because connection to Controller is lost
        //
        private void StopExtensions()
        {
            _isStopped = true;

            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
                ext?.Stop();
        }

        private async Task<WebApplication> InitLocalWebserver(string[] args, CfgService cfgService)
        {
            var cfgInstance = cfgService.GetConfig();

            var internetIpAddress = string.Empty;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                internetIpAddress = NetworkInfo.GetLocalIpForInternetAccess();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                var successfulPings = NetworkInfo.GetLocalIpAddressesWithSuccessfulPing();
                internetIpAddress = successfulPings.FirstOrDefault();
            }

            QrCodeGenerator.GenerateHostQrCode(internetIpAddress, cfgInstance.LocalHost.ListenPort);

            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSingleton<ICfgService>(cfgService);
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins(
                            $"{Globals.HttpProtocol}://{cfgInstance.Connection.Host}:{cfgInstance.Connection.Port}",
                            $"http://{QrCodeGenerator.InternetIpAddress}:{cfgInstance.LocalHost.ListenPort}",
                            $"{Globals.HttpProtocol}://127.0.0.1:{cfgInstance.Connection.Port}",
                            $"http://127.0.0.1:{cfgInstance.LocalHost.ListenPort}",
                            $"{Globals.HttpProtocol}://localhost:{cfgInstance.Connection.Port}",
                            $"http://localhost:{cfgInstance.LocalHost.ListenPort}")
                        .AllowAnyHeader()
                        .AllowAnyMethod();
                    //policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                });
            });
            var urls = $"http://{cfgInstance.LocalHost.ListenDevice}:{cfgInstance.LocalHost.ListenPort}";
            builder.WebHost.UseUrls(urls);
            var app = builder.Build();
            app.UseCors();
            if (app.Environment.IsDevelopment())
                app.UseDeveloperExceptionPage();

            // Mapping Get & Post

            #region HomeModule

            app.MapGet("/{*filepath}", async context =>
            {
                await HomeModule.HandleFiles(context);
            });

            #endregion

            #region WebSocketModule

            app.MapGet(WebSocketModule.WsUri, async context =>
            {
                await WebSocketModule.HandleWsRequest(context, HandleConfigChangeCallback, HandleQueryStateCallback);
            });

            #endregion

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromMinutes(600)
            });

            return app;
        }

        private JObject GetState()
        {
            var obj = new JObject
            {
                ["name"] = "Gateway",
                ["address"] = ServerConnection?.ServerUri ?? "unknown",
                ["connected"] = ServerConnection?.IsConnected ?? false,
                ["recentLog"] = ListAppender.GetLog()
            };

            return obj;
        }

        private JObject HandleQueryStateCallback()
        {
            var obj = new JObject();

            var arrExtensions = new JArray();
            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
                arrExtensions.Add(ext.GetState());
            obj["extensions"] = arrExtensions;
            obj["gateway"] = GetState();
            obj["command"] = "state";
            return obj;
        }

        private async Task HandleConfigChangeCallback(ICfgService cfgService)
        {
            var cfgCnt = cfgService.GetCfgContent();
            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
            {
                // stop extension, reload cfg, start extension of cfg allows it
                if (ext.IsRunning() && ext.IsCfgChanged(cfgCnt))
                {
                    ext.Stop();
                    await ext.ShutdownAsync();
                    await Task.Delay(500);
                    ext.Initialize(this, cfgCnt);
                    await ext.RunAsync();
                }
                else if (ext.IsCfgChanged(cfgCnt))
                {
                    await ext.ShutdownAsync();
                    await Task.Delay(500);
                    ext.Initialize(this, cfgCnt);
                    await ext.RunAsync();
                }
            }

            //
            // Wenn wir eine neue Konfiguration erhalten, dann kann
            // man es sicher einmal probieren sich wieder zum
            // Controller zu verbinden und zu authentifizieren.
            //
            if (!State.IsOk())
            {
                State.Reset();

                State.RecentErrorCode = -1;

                _ = CheckReconnect();
            }
        }

        public async Task RunAsync(string[] args)
        {
            Logging.Log.Info("railhq.io Gateway is started and running");
            var cfg = new CfgService();
            var webApp = await InitLocalWebserver(args, cfg);
            await webApp.StartAsync(CancellationToken.None);
            InitExtensions(cfg);
            var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
            foreach (var ext in extensions)
                RunningExtensions.Add(ext.RunAsync());
            await InitServerConnection(cfg);
            await webApp.WaitForShutdownAsync();
        }
    }
}

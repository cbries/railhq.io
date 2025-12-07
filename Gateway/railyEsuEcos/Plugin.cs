using System;
using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using System.Threading.Tasks;
using libEsuEcos;
using libEsuEcos.Blocks;
using libInterop;
using libUtilities;
using railyEsuEcos.Commands;
using railyEsuEcos.Network;
using Newtonsoft.Json.Linq;

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

// ReSharper disable UnusedMember.Global

namespace railyEsuEcos
{
    public class Plugin : IRailyExtension
    {
        #region IRailyExtension

        public IRailyHostCallbacks Host { get; private set; }
        public Version Version => Version.Parse(Globals.VersionS);
        public string Name => Globals.Name;
        public string DisplayName => Globals.DisplayName;
        public string Copyright => Globals.Copyright;
        public string Description => Globals.Description;
        public RailyExtensionType Type => RailyExtensionType.Controller | RailyExtensionType.Feedback;

        #endregion

        internal Cfg.Cfg Cfg { get; private set; } = null!;

        private ConnectorFaster _tcpEcosClient;

        private void LoadConfiguration(string cfgContent)
        {
            if (string.IsNullOrEmpty(cfgContent))
                throw new Exception("no configuration provided");

            Cfg = JsonConvert.DeserializeObject<Cfg.Cfg>(cfgContent) ?? new Cfg.Cfg();
            if (Cfg == null)
                throw new Exception("invalid configuration");
        }

        public Plugin()
        {
            Logging.Log.Debug($"*** Construct {Globals.Name}");
        }

        public JObject GetState()
        {
            var obj = new JObject
            {
                ["name"] = Name,
                ["running"] = IsRunning()
            };

            if (_tcpEcosClient != null)
            {
                obj["connected"] = _tcpEcosClient.IsConnected();
            }
            else
            {
                obj["connected"] = false;
            }

            return obj;
        }

        private bool _isInitialized;

        public void Initialize(IRailyHostCallbacks host, string cfgContent)
        {
            if (_isInitialized) return;

            _isInitialized = true;

            LoadConfiguration(cfgContent);

            if (!Cfg.CfgTargetEcos.IsEnabled)
            {
                Logging.Log.Info($"{Globals.Name} is not enabled and will not be started.");
                return;
            }

            Host = host;

            InitEsu();
        }

        private bool HasCfgChanged(Cfg.Cfg cfgNew)
        {
            var newEcos = cfgNew.CfgTargetEcos;
            var currentEcos = Cfg.CfgTargetEcos;

            if (newEcos.IsEnabled != currentEcos.IsEnabled) return true;
            if (newEcos.DelaySecondsReconnect != currentEcos.DelaySecondsReconnect) return true;
            if (!newEcos.Ip.Equals(currentEcos.Ip, StringComparison.OrdinalIgnoreCase)) return true;
            if (newEcos.TargetPort != currentEcos.TargetPort) return true;
            if (!newEcos.TargetIp.Equals(currentEcos.TargetIp)) return true;

            return false;
        }

        public bool IsCfgChanged(string cfgContent)
        {
            var cfgNew = JsonConvert.DeserializeObject<Cfg.Cfg>(cfgContent) ?? new Cfg.Cfg();
            var hasChanged = HasCfgChanged(cfgNew);
            if (!hasChanged) return false;

            Logging.Log.Info($"[{Globals.Name}] Configuration changed, reload extension required.");

            return true;
        }

        /// <summary>
        /// Beim Einschalten vom Handling wird geprüft ob eine Verbindung zur
        /// ECoS besteht, wenn nein, dann wird versucht eine Verbindung herzustellen.
        /// Sobald eine Verbindung hergestellt wurde, werden die initialen Befehle gesendet.
        ///
        /// Sollte aber schon eine Verbindung bestehen, so werden die initialen Befehle
        /// sofort versendet und das Ergebnis entsprechend an den Controller weitergeleitet.
        /// </summary>
        public void EnableHandling()
        {
            if (!IsRunning())
            {
                _ = RunAsync();
            }
        }

        public void InitViews()
        {
            ExecuteInitialCommands();
            ExecuteRefreshOfAllStates();
        }

        public bool IsRunning()
        {
            if (_tcpEcosClient == null) return false;
            if (_tcpEcosClient.IsConnected()) return true;
            if (!_isReconnectLoopStopped) return true;
            return false;
        }

        public void Stop()
        {
            _isReconnectLoopStopped = true;
            _dtNextConnect = DateTime.MinValue;

            //
            // stop locomotives
            //
            var locStopCmds = new List<string>();
            foreach (var itLoc in ListOfLocomotiveIdsForEmergencyStop)
            {
                if (itLoc > 1)
                {
                    locStopCmds.Add($"request({itLoc}, control, force)");
                    locStopCmds.Add($"set({itLoc}, speedstep[0])");
                    locStopCmds.Add($"release({itLoc}, control)");
                }
            }
            var cmdListStopsLocs = string.Join(Globals.CommandLineTermination, locStopCmds);
            _tcpEcosClient?.Send(cmdListStopsLocs);

            //
            // power off
            //
            var cmds = new List<string>
            {
                $"request(1, control, force)",
                $"set(1, status[STOP])",
                $"release(1, control)"
            };
            var cmdList = string.Join(Globals.CommandLineTermination, cmds);
            _tcpEcosClient?.Send(cmdList);
        }

        public async Task ShutdownAsync()
        {
            Logging.Log.Info($"{Globals.Name} is shutting down!");

            _isInitialized = false;
            _isReconnectLoopStopped = true;
            _dtNextConnect = DateTime.MinValue;

            _tcpEcosClient?.Stop();

            if (_tcpEcosClient != null)
            {
                _tcpEcosClient.MessageReceived -= TcpEcosClientOnMessageReceived;
                _tcpEcosClient.Started -= TcpEcosClientOnStarted;
                _tcpEcosClient.Failed -= TcpEcosClientOnFailed;
            }

            _tcpEcosClient = null;
        }

        public IPayload CreatePayload()
        {
            return new Payload();
        }

        public async Task RunAsync()
        {
            if (!Cfg.CfgTargetEcos.IsEnabled) return;
            _isReconnectLoopStopped = false;
            await CheckReconnect();
        }

        /// <summary>
        /// Handles all messages which are received by the host.
        /// </summary>
        /// <param name="jsonMessage"></param>
        public void ProvideMessageToExtension(string jsonMessage)
        {
            Logging.Log.Debug($"{Globals.Name} received command: {jsonMessage}");

            // Ping/Pong
            //Host?.MessageReceived(this, jsonMessage);

            if (_tcpEcosClient != null && !_tcpEcosClient.IsConnected())
            {
                Logging.Log.Debug($"No connection to {Globals.Name} established. Commands not sent.");
                return;
            }

            try
            {
                var payload = JsonConvert.DeserializeObject<Payload>(jsonMessage);
                if (payload == null) throw new Exception("Invalid data format");
                if (payload.CommandBlocks.Count > 0)
                {
                    //
                    // blocks/commands for the ECoS are directly forwared
                    // currently their is no need to handle them individual
                    //
                    foreach (var blk in payload.CommandBlocks)
                    {
                        var blkLocal = blk;
                        if (!blkLocal.EndsWith(Globals.CommandLineTermination))
                            blkLocal += Globals.CommandLineTermination;

                        _tcpEcosClient?.Send(blkLocal);
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        private void ProvideMessageToHost(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return;
            Logging.Log.Debug($"MessageToHost: {jsonMessage.Inline()}");
            Host?.MessageReceived(this, jsonMessage);
        }

        private void ProvideMessageToHost(IPayload payload)
        {
            if (payload == null) return;
            if (payload.CommandBlocks.Count <= 0)
            {
                Trace.WriteLine("Nothing to send, command blocks empty.");

                return;
            }
            var json = JsonConvert.SerializeObject(payload);
            ProvideMessageToHost(json);
        }

        #region ESU ECoS

        private bool IsEsuConnected()
        {
            if (_tcpEcosClient != null && _tcpEcosClient.IsConnected())
                return true;

            return false;
        }

        public void TryOpen()
        {
            if (IsEsuConnected()) return;

            Logging.Log.Info($"[{Globals.Name}] Try open...");

            _isReconnectLoopStopped = true;
            _objectIdsWithView.Clear();
            _dtNextConnect = DateTime.MinValue;

            _tcpEcosClient?.Start();
        }

        private void InitEsu()
        {
            // do not allow to initialize two times
            if (_tcpEcosClient != null) return;

            _tcpEcosClient = new ConnectorFaster
            {
                IpAddress = Cfg.CfgTargetEcos.TargetIp.ToString(),
                Port = Cfg.CfgTargetEcos.TargetPort
            };

            _tcpEcosClient.MessageReceived += TcpEcosClientOnMessageReceived;
            _tcpEcosClient.Started += TcpEcosClientOnStarted;
            _tcpEcosClient.Failed += TcpEcosClientOnFailed;
        }

        private bool _checkReconnectStarted = false;
        private bool _isReconnectLoopStopped;
        private DateTime _dtNextConnect = DateTime.MinValue;

        private async Task WaitWithLoggingAsync(DateTime nextAttempt, string ecosAddr)
        {
            while (DateTime.Now < nextAttempt)
            {
                if (!Cfg.CfgTargetEcos.IsEnabled) return;

                if (IsEsuConnected())
                {
                    Logging.Log.Info($"[{Globals.Name}] Connected");
                    return;
                }

                var delta = nextAttempt - DateTime.Now;
                Logging.Log.Info($"[{Globals.Name}] Try to connect in {(int)delta.TotalSeconds} seconds to {ecosAddr}.");
                if (delta.TotalSeconds <= 5) break;
                await Task.Delay(TimeSpan.FromSeconds(5));
            }

            var finalWait = nextAttempt - DateTime.Now;
            if (finalWait > TimeSpan.Zero)
                await Task.Delay(finalWait);
        }

        private async Task CheckReconnect()
        {
            if (_checkReconnectStarted) return;

            _checkReconnectStarted = true;

            try
            {
                var ecosAddr = $"{Cfg.CfgTargetEcos.TargetIp}:{Cfg.CfgTargetEcos.TargetPort}";
                var cfgDelaySeconds = Cfg.CfgTargetEcos.DelaySecondsReconnect;
                await WaitWithLoggingAsync(_dtNextConnect, ecosAddr);

                if (!Cfg.CfgTargetEcos.IsEnabled) return;

                while (!_isReconnectLoopStopped)
                {
                    if (IsEsuConnected()) return;

                    var isPingOk = NetworkInfo.IsPingOk(Cfg.CfgTargetEcos.Ip, true);
                    if (isPingOk)
                    {
                        Logging.Log.Info($"[{Globals.Name}] Ping ok, try to connect to {ecosAddr}...");
                        break;
                    }

                    Logging.Log.Info($"[{Globals.Name}] Try to ping in {cfgDelaySeconds} seconds to {ecosAddr}.");
                    await Task.Delay(TimeSpan.FromSeconds(cfgDelaySeconds));

                    if (_isReconnectLoopStopped) return;
                }

                _isReconnectLoopStopped = false;

                // when already connected do not connect again
                if (IsEsuConnected())
                    return;

                _objectIdsWithView.Clear();
                _dtNextConnect = DateTime.Now + TimeSpan.FromSeconds(cfgDelaySeconds);

                // last change; should not be reached -- there must be a race condition somehow
                if (!Cfg.CfgTargetEcos.IsEnabled) return;

                _tcpEcosClient?.Start();
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
            finally
            {
                _checkReconnectStarted = false;
            }
        }

        private readonly List<string> _receivedLines = new();
        private DateTime _walltimeClearReceivedLines = DateTime.MinValue;

        private void ClearRecentReceivedLines()
        {
            if (_receivedLines.Count > 3)
            {
                if (DateTime.Now > _walltimeClearReceivedLines)
                {
                    _receivedLines.Clear();
                    _walltimeClearReceivedLines = DateTime.MinValue;
                }
                else
                {
                    _walltimeClearReceivedLines = DateTime.Now + TimeSpan.FromSeconds(5);
                }
            }
        }

        private void TcpEcosClientOnMessageReceived(object sender, MessageEventArgs eventargs)
        {
            var line = eventargs.Message?.Trim();
            if (string.IsNullOrEmpty(line)) return;

            //Trace.WriteLine($"ECoS [in] --> Cloud [out]: {line}");

            _receivedLines.Add(line.Trim());

            var blockInstances = BlockUtilities.ExtractBlocks(_receivedLines);
            if (blockInstances.Count > 0)
            {
                HandleBlocks(blockInstances);
                ClearRecentReceivedLines();
            }
        }

        private void HandleBlocks(IReadOnlyList<IBlock> blocks)
        {
            // avoid empty messages to railhq.io
            if (blocks.Count == 0) return;

            PreHandleReceivedBlocks(blocks);

            var payload = new Payload();
            payload.AddBlocks(blocks);

            ProvideMessageToHost(payload);
        }

        private List<int> ListOfLocomotiveIdsForEmergencyStop { get; } = new();

        private readonly List<int> _objectIdsWithView = new();

        /// <summary>
        /// This methods parses the blocks and checks
        /// for specific information, e.g. when a REPLY
        /// block for a list of locomotives is received
        /// we will register a view to any locomotive id.
        /// </summary>
        /// <param name="blocks"></param>
        private void PreHandleReceivedBlocks(IReadOnlyList<IBlock> blocks)
        {
            if (blocks == null) return;

            foreach (var blk in blocks)
            {
                if (!(blk is ReplyBlock)) continue;

                foreach (var itm in blk.ListEntries)
                {
                    var objId = itm.ObjectId;
                    if (objId < 0) continue;

                    if (_objectIdsWithView.Contains(objId)) continue;

                    _objectIdsWithView.Add(objId);

                    if (libEsuEcos.Globals.IsLocomotiveId(objId))
                    {
                        if (!ListOfLocomotiveIdsForEmergencyStop.Contains(objId))
                            ListOfLocomotiveIdsForEmergencyStop.Add(objId);
                    }

                    RefreshObjectStates(objId);
                }
            }
        }

        private void TcpEcosClientOnStarted(object sender, EventArgs e)
        {
            Logging.Log.Info($"Connected to ECoS, ready to receive commands from railhq.io!");

            ExecuteInitialCommands();
            ExecuteRefreshOfAllStates();
        }

        private void TcpEcosClientOnFailed(object sender, MessageEventArgs eventargs)
        {
            var m = eventargs.Exception?.GetExceptionMessages() ?? eventargs.Message;
            Logging.Log.Debug($"Ecos client handling failed: {m}");

            Task.Run(async () =>
            {
                await CheckReconnect();
            });
        }

        #endregion

        private void RefreshObjectStates(int objId)
        {
            var cmdView = CommandFactory.Create($"request({objId}, view)");

            _tcpEcosClient.Send(cmdView.ToString());

            if (libEsuEcos.Globals.IsLocomotiveId(objId))
            {
                // IMPORTANT: order is relevant, funcdesc BEFORE func
                var cmdFuncdesc = CommandFactory.Create($"get({objId}, funcdesc, func, dir, speed, speedstep)");
                var strCmd = cmdFuncdesc.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(strCmd))
                    _tcpEcosClient.Send(strCmd);
            }
            else if (libEsuEcos.Globals.IsAccessorySwitch(objId))
            {
                var cmdGetState = CommandFactory.Create($"get({objId}, state)");
                var strCmd = cmdGetState.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(strCmd))
                    _tcpEcosClient.Send(strCmd);
            }
        }

        private void ExecuteRefreshOfAllStates()
        {
            foreach (var objId in _objectIdsWithView)
            {
                var cmdView = CommandFactory.Create($"request({objId}, view)");

                _tcpEcosClient.Send(cmdView.ToString());

                RefreshObjectStates(objId);
            }
        }

        private void ExecuteInitialCommands()
        {
            var defaultRequests = new List<ICommand>
            {
                Globals.CommandGetInfo,
                Globals.CommandGetStatus,

                // Test: 2025-05-21 
                // Vielleicht ist es besser die Lokomotiven/Schaltartikel ganz am Anfang abzufragen, so dass
                // diese eher vom Controller verarbeitet werden können und schneller beim Anwender landen.
                CommandFactory.Create($"queryObjects({libEsuEcos.Globals.ID_EV_LokManager}, addr, name, protocol)"),
                CommandFactory.Create($"queryObjects({libEsuEcos.Globals.ID_EV_SchaltartikelManager}, addr, protocol, type, addrext, mode, symbol, name1, name2, name3, gates)"),

                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_ECoS}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Programmiergleis}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_LokManager}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_SchaltartikelManager}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Pendelzugsteueuerung}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Devicemanager}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Sniffer}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_FeedbackManager}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Booster}, view)"),
                CommandFactory.Create($"request({libEsuEcos.Globals.ID_EV_Booster}, ID_EV_Stellpult)"),
                CommandFactory.Create($"get({libEsuEcos.Globals.ID_EV_FeedbackManager}, state)"),
                //CommandFactory.Create($"get(100, state)"), // ids between 100 and 131 are S88 devices
                //CommandFactory.Create($"get(200, state)"), // Update: ObjectId >= 200 gehören zu ECoS-Detektoren
                CommandFactory.Create($"get({libEsuEcos.Globals.ID_EV_Devicemanager}, state)"),
                
                CommandFactory.Create($"queryObjects({libEsuEcos.Globals.ID_EV_FeedbackManager}, ports)"),
                CommandFactory.Create($"queryObjects({libEsuEcos.Globals.ID_EV_Devicemanager}, ports)")
            };

            foreach (var req in defaultRequests)
            {
                if (_tcpEcosClient != null && _tcpEcosClient.IsConnected())
                {
                    _tcpEcosClient.Send(req.ToString());
                }
                else
                {
                    // TODO
                }
            }
        }
    }
}

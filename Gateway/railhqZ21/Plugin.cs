
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

// ReSharper disable UnusedMember.Global

using libInterop;
using libUtilities;
using libZ21;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Timers;

namespace railhqZ21
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

        private Z21Connection _udpZ21Connection;

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

            if (_udpZ21Connection != null)
            {
                obj["connected"] = IsZ21Connected();
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

            if (!Cfg.CfgTargetZ21.IsEnabled)
            {
                Logging.Log.Info($"{Globals.Name} is not enabled and will not be started.");
                return;
            }

            Host = host;

            InitZ21();
        }

        private bool HasCfgChanged(Cfg.Cfg cfgNew)
        {
            var newZ21 = cfgNew.CfgTargetZ21;
            var currentZ21 = Cfg.CfgTargetZ21;

            if (newZ21.IsEnabled != currentZ21.IsEnabled) return true;
            if (newZ21.DelaySecondsReconnect != currentZ21.DelaySecondsReconnect) return true;
            if (!newZ21.Ip.Equals(currentZ21.Ip, StringComparison.OrdinalIgnoreCase)) return true;
            if (newZ21.TargetPort != currentZ21.TargetPort) return true;
            if (!newZ21.TargetIp.Equals(currentZ21.TargetIp)) return true;

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

        public void EnableHandling()
        {
            if (!IsRunning())
            {
                _ = RunAsync();
            }
        }

        private static Timer _heartbeatTimer;
        private const int _heartbeatIntervalSec = 15;

        public void InitViews()
        {
            // trigger state updates
            // query status, i.e. power state after connect
            var getStatusCommand = Z21.GetStatusCommand();
            _udpZ21Connection?.SendCommand(getStatusCommand, getStatusCommand.Length);

            #region Heartbeat

            if (_heartbeatTimer != null)
            {
                try
                {
                    _heartbeatTimer.Elapsed -= HeartbeatTimerOnElapsed;

                    _heartbeatTimer.Stop();
                    _heartbeatTimer.Dispose();
                }
                catch
                {
                    // ignore
                }

                _heartbeatTimer = null;
            }

            _heartbeatTimer = new Timer(_heartbeatIntervalSec * 1000);
            _heartbeatTimer.Elapsed += HeartbeatTimerOnElapsed;
            _heartbeatTimer.AutoReset = true;
            _heartbeatTimer.Enabled = true;

            #endregion
        }

        private void HeartbeatTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_udpZ21Connection is { Connected: false }) return;
            var temp = Broadcast.GetFlagConfig();
            var tempdata = temp.GetAsBytes();
            byte[] sendBytes = [0x08, 0x00, 0x50, 0x00, tempdata[0], tempdata[1], tempdata[2], tempdata[3]];
            _udpZ21Connection.SendCommand(sendBytes, 8);

            var cmd = Z21.GetHardwareInfoCommand();
            _udpZ21Connection?.SendCommand(cmd, cmd.Length);

            for (var groupIndex = 0; groupIndex < 20; ++groupIndex)
            {
                var cmdRmBus = Z21.RmbusGetdataCommand((byte)groupIndex);
                _udpZ21Connection?.SendCommand(cmdRmBus, cmdRmBus.Length);
            }

        }

        public bool IsRunning()
        {
            if (_udpZ21Connection == null) return false;
            if (_udpZ21Connection.Connected) return true;
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
            try
            {
                var emergencyBytes = Z21.EmergencyStop;
                _udpZ21Connection?.SendCommand(emergencyBytes, emergencyBytes.Length);
            }
            catch
            {
                // ignore
            }

            //
            // power off
            //
            try
            {
                var powerOffBytes = Z21.PowerOffBytes;
                _udpZ21Connection?.SendCommand(powerOffBytes, powerOffBytes.Length);
            }
            catch
            {
                // ignore
            }
        }

        public async Task ShutdownAsync()
        {
            Logging.Log.Info($"{Globals.Name} is shutting down!");

            _isInitialized = false;
            _isReconnectLoopStopped = true;
            _dtNextConnect = DateTime.MinValue;

            _udpZ21Connection?.Disconnect();
            _udpZ21Connection = null;
        }

        public IPayload CreatePayload()
        {
            return new libZ21.Payload();
        }

        public async Task RunAsync()
        {
            if (!Cfg.CfgTargetZ21.IsEnabled) return;
            _isReconnectLoopStopped = false;
            await CheckReconnect();
        }
        
        public void ProvideMessageToExtension(string jsonMessage)
        {
            Logging.Log.Debug($"{Globals.Name} received command: {jsonMessage}");

            if (!IsZ21Connected())
            {
                Logging.Log.Debug($"No connection to {Globals.Name} established. Commands not forwarded to gateway extension.");
                return;
            }

            try
            {
                var payload = JsonConvert.DeserializeObject<Payload>(jsonMessage);
                if (payload == null) throw new Exception("Invalid data format");
                if (payload.CommandBlocks.Count > 0)
                {
                    //
                    // blocks/commands for the Z21 are directly forwared
                    // currently their is no need to handle them individual
                    //
                    foreach (var byteCommand in payload.GetEncodedCommands())
                    {
                        if(byteCommand == null) continue;
                        if (byteCommand.Length == 0) continue;

                        _udpZ21Connection?.SendCommand(byteCommand, byteCommand.Length);
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

        #region Z21

        private bool IsZ21Connected()
        {
            if (_udpZ21Connection != null && _udpZ21Connection.Connected)
                return true;

            return false;
        }

        public void TryOpen()
        {
            if (IsZ21Connected()) return;

            Logging.Log.Info($"[{Globals.Name}] Try open...");

            _isReconnectLoopStopped = true;
            _dtNextConnect = DateTime.MinValue;

            _udpZ21Connection?.Connect();
        }

        private bool _checkReconnectStarted = false;
        private bool _isReconnectLoopStopped;
        private DateTime _dtNextConnect = DateTime.MinValue;

        private async Task WaitWithLoggingAsync(DateTime nextAttempt, string z21Addr)
        {
            while (DateTime.Now < nextAttempt)
            {
                if (!Cfg.CfgTargetZ21.IsEnabled) return;

                if (IsZ21Connected())
                {
                    Logging.Log.Info($"[{Globals.Name}] Connected");
                    return;
                }

                var delta = nextAttempt - DateTime.Now;
                Logging.Log.Info($"[{Globals.Name}] Try to connect in {(int)delta.TotalSeconds} seconds to {z21Addr}.");
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
                var z21Addr = $"{Cfg.CfgTargetZ21.TargetIp}:{Cfg.CfgTargetZ21.TargetPort}";
                var cfgDelaySeconds = Cfg.CfgTargetZ21.DelaySecondsReconnect;
                await WaitWithLoggingAsync(_dtNextConnect, z21Addr);

                if (!Cfg.CfgTargetZ21.IsEnabled) return;

                while (!_isReconnectLoopStopped)
                {
                    if (IsZ21Connected()) return;

                    var isPingOk = NetworkInfo.IsPingOk(Cfg.CfgTargetZ21.Ip, true);
                    if (isPingOk)
                    {
                        Logging.Log.Info($"[{Globals.Name}] Ping ok, try to connect to {z21Addr}...");
                        break;
                    }

                    Logging.Log.Info($"[{Globals.Name}] Try to ping in {cfgDelaySeconds} seconds to {z21Addr}.");
                    await Task.Delay(TimeSpan.FromSeconds(cfgDelaySeconds));

                    if (_isReconnectLoopStopped) return;
                }

                _isReconnectLoopStopped = false;

                // when already connected do not connect again
                if (IsZ21Connected())
                    return;

                _dtNextConnect = DateTime.Now + TimeSpan.FromSeconds(cfgDelaySeconds);

                // last change; should not be reached -- there must be a race condition somehow
                if (!Cfg.CfgTargetZ21.IsEnabled) return;

                _udpZ21Connection?.Connect();
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

        private void InitZ21()
        {
            // do not allow to initialize two times
            if (_udpZ21Connection != null) return;

            _udpZ21Connection = new Z21Connection
            {
                Z21Ip = Cfg.CfgTargetZ21.Ip,
                Z21Port = Cfg.CfgTargetZ21.TargetPort
            };

            _udpZ21Connection.OnConnected += Z21ConnectionOnConnected;
            _udpZ21Connection.OnStatusUpdated += Z21ConnectionOnStatusUpdated;
            _udpZ21Connection.OnReceived += Z21ConnectionOnReceived;
        }

        private void Z21ConnectionOnStatusUpdated(object sender, bool state, bool init)
        {
            Logging.Log.Debug($"State: {state}  Init: {init}");
        }

        private void Z21ConnectionOnConnected(object sender)
        {
            var z21Conn = sender as IZ21Connection;
            if (z21Conn == null) return;

            Logging.Log.Info($"Connected to z21 ({z21Conn.Z21Ip}:{z21Conn.Z21Port})!");

            InitViews();
        }

        private void Z21ConnectionOnReceived(object sender, byte[] data)
        {
            var z21Conn = sender as IZ21Connection;
            if (z21Conn == null) return;

            while (data.Length > 0)
            {
                var length = data[0] + data[1] * 256;
                var msgdata = data.Take(length).ToArray();

                var payload = new Payload();
                payload.AddBytes(msgdata);

                ProvideMessageToHost(payload);

                data = data.Skip(length).ToArray();
            }
        }

        #endregion

    }
}

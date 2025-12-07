using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using libInterop;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyHsi88Usb.HSI88USB;

namespace railyHsi88Usb
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
        public RailyExtensionType Type => RailyExtensionType.Feedback;

        #endregion

        internal Cfg.Cfg Cfg { get; private set; } = null!;

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
            return obj;
        }

        public void TryOpen()
        {
            // not used
        }

        private bool _isInitialized;

        public void Initialize(IRailyHostCallbacks host, string cfgContent)
        {
            if (_isInitialized) return;

            _isInitialized = true;

            LoadConfiguration(cfgContent);

            if (!Cfg.CfgHsi88.IsEnabled)
            {
                Logging.Log.Info($"{Globals.Name} is not enabled and will not be loaded.");
                return;
            }

            Host = host;

            InitHsi();
        }

        private bool HasCfgChanged(Cfg.Cfg cfgNew)
        {
            var newCfg = cfgNew.CfgHsi88;
            var currentCfg = Cfg.CfgHsi88;

            if (newCfg.IsEnabled != currentCfg.IsEnabled) return true;
            if (!newCfg.DevicePath.Equals(currentCfg.DevicePath, StringComparison.OrdinalIgnoreCase)) return true;
            if (newCfg.NumberLeft != currentCfg.NumberLeft) return true;
            if (newCfg.NumberMiddle != currentCfg.NumberMiddle) return true;
            if (newCfg.NumberRight != currentCfg.NumberRight) return true;
            if (newCfg.NumberMax != currentCfg.NumberMax) return true;

            var b0 = newCfg.CfgDebounce;
            var b1 = currentCfg.CfgDebounce;
            if (b0.CheckInterval != b1.CheckInterval) return true;
            if (b0.Off != b1.Off) return true;
            if (b0.On != b1.On) return true;

            var c0 = newCfg.CfgSimulation;
            var c1 = currentCfg.CfgSimulation;
            if (c0.IsEnabled != c1.IsEnabled) return true;
            if (c0.BetweenTicksMs != c1.BetweenTicksMs) return true;
            if (c0.SimulationMode != c1.SimulationMode) return true;

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
            // to be defined
        }

        public void InitViews()
        {
            // to be defined
        }

        public bool IsRunning()
        {
            if (_hsi88Device == null) return false;
            return _hsi88Device.IsRunnig();
        }

        public void Stop()
        {
            _hsi88Device?.Close();
        }

        public async Task ShutdownAsync()
        {
            Logging.Log.Debug($"{Globals.Name} is shutting down!");

            Stop();

            _hsi88Device.Failed -= Hsi88DeviceOnFailed;
            _hsi88Device.Opened -= Hsi88DeviceOnOpened;
            _hsi88Device.DataReceived -= Hsi88DeviceOnDataReceived;

            _hsi88Device = null;

            _hsiStates.Clear();

            _isInitialized = false;
            
            await Task.Delay(TimeSpan.FromMilliseconds(500));
        }

        public IPayload CreatePayload()
        {
            return null;
        }

        public async Task RunAsync()
        {
            if (!Cfg.CfgHsi88.IsEnabled) return;

            if (!_isInitialized)
            {
                Logging.Log.Info($"{Globals.Name} is not initialized and will not be started.");
                return;
            }

            // init hsi state cache
            // offset of `HsiStateData.PortIdOffset` because HSI-88-USB starts not with 0-based indeces
            for (var idx = 0; idx < HsiStateData.MaxNoOfDevices; ++idx)
            {
                var portId = HsiStateData.PortIdOffset + idx;

                _hsiStates[portId] = new HsiStateData(Cfg.CfgHsi88.CfgDebounce);
            }

            Logging.Log.Debug($"{Globals.Name} is starting!");

            if (Cfg.CfgHsi88.CfgSimulation.IsEnabled)
            {
                Logging.Log.Info("HSI-88 simulation mode is active!");

                await _hsi88Device.RunSimulationAsync();
            }
            else
            {
                await _hsi88Device.RunAsync();
            }
        }

        public void ProvideMessageToExtension(string jsonMessage)
        {
            Logging.Log.Debug($"{Globals.Name} received host message. "
                              + "The message handling is disabled because of the fact that the HSI-88-USB does not support any external control commands.");
        }

        private void ProvideMessageToHost(string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return;

            Host?.MessageReceived(this, jsonMessage);
        }

        #region HSI

        private DeviceInterface _hsi88Device = null!;

        private void InitHsi()
        {
            _hsi88Device = new DeviceInterface();
            _hsi88Device.Failed += Hsi88DeviceOnFailed;
            _hsi88Device.Opened += Hsi88DeviceOnOpened;
            _hsi88Device.DataReceived += Hsi88DeviceOnDataReceived;
            _hsi88Device.Init(Cfg.CfgHsi88, Cfg.CfgHsi88.CfgDebounce);
        }

        private void Hsi88DeviceOnOpened(object sender, EventArgs eventargs)
        {
            Logging.Log.Info("HSI-88-USB interface opened");
        }

        private void Hsi88DeviceOnFailed(object sender, EventArgs eventargs)
        {
            var evArgs = eventargs as DeviceInterfaceEventArgs;
            Logging.Log.Debug($"HSI-88-USB failed: {evArgs?.Message ?? "reason unknown"}");
        }

        private readonly ConcurrentDictionary<int, HsiStateData> _hsiStates = new();

        private bool _versionShown = false;

        private void Hsi88DeviceOnDataReceived(object sender, DeviceInterfaceData data)
        {
            if (!_versionShown && data.Data.StartsWith("V", StringComparison.OrdinalIgnoreCase))
            {
                _versionShown = true;
                Logging.Log.Debug($"HSI-88: {data.Data}");
                return;
            }

            foreach (var it in data.States)
            {
                var hsiDeviceId = it.Key;
                var hsiPort = _hsiStates[hsiDeviceId];
                var r = hsiPort.Update(it.Value, Cfg.CfgHsi88.CfgSimulation.IsEnabled);
                if (r)
                    ProvideMessageToHost(GetStateOfModule(hsiDeviceId).ToCleanString());
            }
        }

        private JObject GetStateOfModule(int portIndex)
        {
            var jsonEvent = new JObject
            {
                ["port"] = portIndex,
                ["state"] = new JObject
                {
                    ["hex"] = _hsiStates[portIndex].NativeHexData,
                    ["binary"] = _hsiStates[portIndex].NativeBinaryData
                }
            };

            var json = new JObject
            {
                ["event"] = jsonEvent,
                ["info"] = new JObject
                {
                    ["left"] = Cfg.CfgHsi88.NumberLeft,
                    ["middle"] = Cfg.CfgHsi88.NumberMiddle,
                    ["right"] = Cfg.CfgHsi88.NumberRight
                }
            };

            return json;
        }

        #endregion
    }
}

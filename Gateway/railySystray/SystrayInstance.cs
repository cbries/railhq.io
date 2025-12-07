// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

#if !MACOS

using System;
using H.NotifyIcon.Core;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using libUtilities;
using Newtonsoft.Json;
using railySystray.Resources;

#pragma warning disable CA1416

namespace railySystray
{
    public class SystrayInstance
    {
        // process name
        public static string RailHqName = "railhqGateway";
        // name of the execuable
        public static string RailHqExeName = RailHqName + ".exe";
        public static string RailHqCfgName = libInterop.Globals.AppConfigName;

        //
        // this file is created after the setup finished
        // and contains userdata, i.e.
        //  SetupText := 'username:' + UserNameValue + #13#10 +
        //      'password:' + PasswordValue + #13#10 +
        //      'host:' + HostNameValue + #13#10;
        //
        public static string RailHqSetupName = "railhqGateway.setup";
        public static int RailHqDefaultPort = 5001;

        public static SystrayRm Rm { get; private set; }

        private Process _gatewayProcess;
        private TrayIconWithContextMenu _trayIconInstance;
        private Icon _iconRed;
        private Icon _iconGreen;

        private string Language { get; set; } = string.Empty;

        private string _internetIp;
        private string _localListenDevice;
        private int _localListenPort;
        private string _cfgUrl;

        private string _railyHost;
        private int _railyPort;
        private string _railyUrl;

        private FileSystemWatcher _fileWatcher;

        private PopupMenuItem _pmStartGateway;
        private PopupMenuItem _pmStopGateway;
        private PopupMenuItem _pmRestartGateway;

        private void ApplySetupData()
        {
            var dirOfSetupFile = libShared.RailEnvironment.GetAppDirPath();
            var pathTo = Path.Combine(dirOfSetupFile, RailHqSetupName);
            try
            {
                if (!File.Exists(pathTo)) return;
                var lines = File.ReadAllLines(pathTo, Encoding.UTF8);
                var username = string.Empty;
                var password = string.Empty;
                var host = string.Empty;
                var port = 5001;
                foreach (var it in lines)
                {
                    if (string.IsNullOrEmpty(it)) continue;
                    if(it.IndexOf(':', StringComparison.OrdinalIgnoreCase) == -1) continue;
                    var parts = it.Split(':', StringSplitOptions.TrimEntries);

                    if (parts[0].StartsWith("username", StringComparison.OrdinalIgnoreCase))
                        username = parts[1].Trim();
                    else if (parts[0].StartsWith("password", StringComparison.OrdinalIgnoreCase))
                        password = parts[1].Trim();
                    else if (parts[0].StartsWith("host", StringComparison.OrdinalIgnoreCase))
                    {
                        var uri = new Uri(it.Replace("host:", string.Empty));
                        host = uri.Host;
                        port = uri.Port;
                    }
                }

                // apply data to cfg
                var pathToCfg = libShared.RailEnvironment.GetConfigDirPath();
                var json = File.ReadAllText(pathToCfg, Encoding.UTF8);
                var obj = JObject.Parse(json);
                if (obj["connection"] != null)
                {
                    var c = obj["connection"] as JObject;
                    if (c == null) c = new JObject();
                    c["host"] = host;
                    c["port"] = port;
                    c["username"] = username;
                    c["password"] = password;
                    obj["connection"] = c;
                }
                File.WriteAllText(pathToCfg, obj.ToString(Formatting.Indented));
            }
            catch
            {
                // ignore
            }
            finally
            {
                TryDelete(pathTo);
            }
        }

        private void TryDelete(string path)
        {
            try
            {
                if(File.Exists(path))
                    File.Delete(path);
            }
            catch
            {
                // ignore
            }
        }
        
        private void StartFileWatcher()
        {
            ApplySetupData();

            var pathToCfg = libShared.RailEnvironment.GetConfigDirPath();
            var dirOfCfg = Path.GetDirectoryName(pathToCfg);
            if (!File.Exists(pathToCfg))
            {
                // to be defined
            }
            else
            {
                UpdateCfgAddress(pathToCfg);

                if (!string.IsNullOrEmpty(dirOfCfg))
                {
                    if (_fileWatcher == null)
                    {
                        _fileWatcher = new FileSystemWatcher(dirOfCfg, RailHqCfgName);
                        _fileWatcher.Changed += FileWatcherOnChanged;
                        _fileWatcher.EnableRaisingEvents = true;
                    }
                }
            }
        }

        private void UpdateCfgAddress(string pathToCfg)
        {
            try
            {
                var jsonCnt = File.ReadAllText(pathToCfg, Encoding.UTF8);
                var jsonObj = JObject.Parse(jsonCnt);

                Language = jsonObj.GetString("language", string.Empty);

                #region Connection to railhq.io controller

                if (jsonObj["connection"] is JObject c)
                {
                    _railyHost = c.GetString("host", "railhq.io");
                    _railyPort = c.GetInt("port", RailHqDefaultPort);
                    _railyUrl = $"{Globals.HttpProtocol}://{_railyHost}/Auth/Redirect";
                }

                #endregion

                #region Access to local Dashboard

                if (jsonObj["localhost"] is JObject c1)
                {
                    _localListenDevice = c1.GetString("listenDevice");
                    if (_localListenDevice.Equals("internet", StringComparison.OrdinalIgnoreCase)
                        || _localListenDevice.Equals("auto", StringComparison.OrdinalIgnoreCase))
                        _localListenDevice = _internetIp;

                    _localListenPort = c1.GetInt("listenPort");
                }
                else
                {
                    _localListenDevice = "0.0.0.0";
                    _localListenPort = 8090;
                }

                _cfgUrl = $"{Globals.HttpProtocol}://{_localListenDevice}:{_localListenPort}";

                #endregion
            }
            catch
            {
                // ignore
            }
        }

        private void FileWatcherOnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Changed)
                UpdateCfgAddress(e.FullPath);
        }
        private void QueryCurrentInternetAddress()
        {
            var successfulPings = NetworkInfo.GetLocalIpAddressesWithSuccessfulPing();
            _internetIp = successfulPings.FirstOrDefault();
        }

        public static void StartBrowserWith(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        public void ShowMessage(string message, string title)
        {
            _trayIconInstance?.ShowNotification(
                title: title,
                message: message,
                icon: NotificationIcon.Info,
                sound: false
                );
        }

        public void ShowError(string message, string title)
        {
            _trayIconInstance?.ShowNotification(
                title: title,
                message: message,
                icon: NotificationIcon.Error,
                sound: false
                );
        }

        public void FindAndMonitorProcess()
        {
            var processes = Process.GetProcessesByName(RailHqName);
            if (processes.Any())
            {
                AllowStop();

                _gatewayProcess = processes.First();
                _gatewayProcess.EnableRaisingEvents = true;
                _gatewayProcess.OutputDataReceived += GatewayProcessOnOutputDataReceived;
                _gatewayProcess.Exited += GatewayProcessOnExited;

                ShowMessage(Rm.Get("AppFoundMessage"), Rm.Get("DialogTitle"));
            }
            else
            {
                AllowStart();
            }
        }

        public void StartGateway()
        {
            if (IsGatewayRunning()) return;

            var pathToExe = Path.Combine(libShared.RailEnvironment.GetAppDirPath(), RailHqExeName);

            var psi = new ProcessStartInfo
            {
                FileName = pathToExe,
                UseShellExecute = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Normal,
            };

            _gatewayProcess = Process.Start(psi);

            if (_gatewayProcess != null)
            {
                AllowStop();

                _gatewayProcess.EnableRaisingEvents = true;
                _gatewayProcess.OutputDataReceived += GatewayProcessOnOutputDataReceived;
                _gatewayProcess.Exited += GatewayProcessOnExited;
            }
            _trayIconInstance.UpdateIcon(_iconGreen.Handle);
        }

        private void GatewayProcessOnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
                Trace.WriteLine($"{Globals.AppName}: {e.Data.Trim()}");
        }

        private void GatewayProcessOnExited(object sender, EventArgs e)
        {
            ShowError(Rm.Get("ProcessStopped"), Rm.Get("DialogTitleError"));

            AllowStart();

            _trayIconInstance.UpdateIcon(_iconRed.Handle);
        }

        public void StopGateway()
        {
            if (IsGatewayRunning())
                _gatewayProcess.Kill();
            _trayIconInstance.UpdateIcon(_iconRed.Handle);
            _gatewayProcess = null;
        }

        public async Task RestartGateway()
        {
            StopGateway();
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            StartGateway();
        }

        private bool IsGatewayRunning()
        {
            return _gatewayProcess != null && !_gatewayProcess.HasExited;
        }

        private void AllowStop()
        {
            _pmStartGateway.Enabled = false;
            _pmStopGateway.Enabled = true;
            _pmRestartGateway.Enabled = true;

            try
            {
                _trayIconInstance.UpdateIcon(_iconGreen.Handle);
            }
            catch
            {
                // ignore
            }
        }

        private void AllowStart()
        {
            _pmStartGateway.Enabled = true;
            _pmStopGateway.Enabled = false;
            _pmRestartGateway.Enabled = false;

            try
            {
                _trayIconInstance.UpdateIcon(_iconRed.Handle);
            }
            catch
            {
                // ignore
            }
        }

        public void LoadAndRun()
        {
            QueryCurrentInternetAddress();
            StartFileWatcher();

            Rm = new SystrayRm(Language);

            using var iconRedStream = new MemoryStream(Icons.Red);
            using var iconGreenStream = new MemoryStream(Icons.Green);
            _iconGreen = new Icon(iconGreenStream);
            _iconRed = new Icon(iconRedStream);

            _trayIconInstance = new TrayIconWithContextMenu
            {
                ToolTip = Rm.Get("SystrayTooltipTitle"),
                Icon = _iconRed.Handle
            };

            _pmStartGateway = new PopupMenuItem(Rm.Get("StartGateway"), (_, _) => StartGateway());
            _pmStartGateway.Enabled = false;

            _pmStopGateway = new PopupMenuItem(Rm.Get("StopGateway"), (_, _) => StopGateway());
            _pmStopGateway.Enabled = false;

            _pmRestartGateway = new PopupMenuItem(Rm.Get("RestartGateway"), (_, _) => _ = RestartGateway());
            _pmRestartGateway.Enabled = false;


            _trayIconInstance.ContextMenu = new PopupMenu
            {
                Items =
                {
                    new PopupMenuItem(Rm.Get("LinkDispatcherPanel"), (_, _) => StartBrowserWith(_railyUrl)),
                    new PopupMenuItem(Rm.Get("LinkConfiguration"), (_, _) => StartBrowserWith(_cfgUrl)),
                    new PopupMenuSeparator(),
                    _pmStartGateway,
                    _pmStopGateway,
                    _pmRestartGateway,
                    new PopupMenuSeparator(),
                    new PopupMenuItem(Rm.Get("CmdExit"), (_, _) =>
                    {
                        StopGateway();

                        _iconRed.Dispose();
                        _iconGreen.Dispose();
                        _trayIconInstance.Dispose();

                        Environment.Exit(0);
                    }),
                },
            };

            _trayIconInstance.Created += TrayIconInstanceOnCreated;
            _trayIconInstance.Create();
        }

        private void TrayIconInstanceOnCreated(object sender, EventArgs e)
        {
            FindAndMonitorProcess();
        }
    }
}

#endif
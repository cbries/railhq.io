// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using libUtilities;
using Microsoft.Win32.SafeHandles;
using railyHsi88Usb.Cfg;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

namespace railyHsi88Usb.HSI88USB
{
    public delegate void DeviceInterfaceOpened(object sender, EventArgs eventArgs);

    public delegate void DeviceInterfaceFailed(object sender, EventArgs eventArgs);

    public delegate void DeviceInterfaceDataReceived(object sender, DeviceInterfaceData data);

    public class DeviceInterfaceEventArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }

    public class DeviceInterfaceData(string data)
    {
        public DateTime Dt { get; set; } = DateTime.Now;
        public string Data { get; set; } = data;

        /// <summary>
        /// When the information is based on "i00"-feedback
        /// the value is `true`, otherwise `false`.
        /// </summary>
        public bool EventData => Data.Length > 0 && Data[0] == 'i';

        /*
         * Sample data:
         * i01040506
         * i01022c05
         *
         *„i“ <Anzahl der Module, die gemeldet werden>
            <Modulnummer> <HighByte> <LowByte>
            <Modulnummer> <HighByte> <LowByte>
            <Modulnummer> <HighByte> <LowByte>
            <CR>
            -> i 01 02 2c 05

         * Check sor "m00..." as well, e.g.
         * m05019db60201c8036416043df6050000
         * This line is the feedback of polling the current state.
         * "i00" is an event information got when the device signals changes.
         * These are two ways to get information from the S88-device.
         */

        public int NumberOfModules
        {
            get
            {
                if (string.IsNullOrEmpty(Data)) return 0;
                if (!Data.StartsWith("i") && !Data.StartsWith("m")) return 0;
                var data = Data.TrimStart('i').Trim();
                data = data.TrimStart('m').Trim();
                var p0 = data.Substring(0, 2).Trim();
                if (int.TryParse(p0, out var v))
                    return v;
                return 0;
            }
        }

        /// <summary>
        /// key := module id
        /// value := module state
        /// </summary>
        public Dictionary<int, string> States
        {
            get
            {
                var res = new Dictionary<int, string>();
                var noOfModules = NumberOfModules;
                if (noOfModules == 0) return res;
                var p = Data.Substring("i00".Length); // as well "m00".Length
                if (string.IsNullOrEmpty(p)) return res;
                for (var idx = 0; idx < p.Length; idx += 6)
                {
                    var part0 = p.Substring(idx, 6);
                    var pModuleId = part0.Substring(0, 2);
                    var pModuleState = part0.Substring(2);

                    if (int.TryParse(pModuleId, out var mid))
                    {
                        res[mid] = pModuleState;
                    }
                }

                return res;
            }
        }
    }

    internal class DeviceInterface
    {
        public event DeviceInterfaceOpened Opened;
        public event DeviceInterfaceFailed Failed;
        public event DeviceInterfaceDataReceived DataReceived;

        private SafeFileHandle _handle = null;
        private ICfgHsi88 _cfgHsi88 = null;
        private ICfgDebounce _cfgDebounce = null;
        private FileStream _fs;
        private CancellationTokenSource _cancellationToken;

        private void Open()
        {
            _handle = NativeMethods.CreateFile(
                _cfgHsi88.DevicePath,
                NativeMethods.GENERIC_READ | NativeMethods.GENERIC_WRITE,
                NativeMethods.FILE_SHARE_READ | NativeMethods.FILE_SHARE_WRITE,
                IntPtr.Zero,
                NativeMethods.OPEN_EXISTING,
                0,
                IntPtr.Zero);

            if (_handle.IsInvalid)
            {
                Failed?.Invoke(this, new DeviceInterfaceEventArgs("Failed to open device."));
                throw new Exception("Failed to open device.");
            }

            Opened?.Invoke(this, EventArgs.Empty);
        }

        public void Close()
        {
            _cancellationToken.Cancel(false);
        }

        public bool IsRunnig()
        {
            if (_cancellationToken == null) return false;
            if (_fs == null) return false;
            return !_cancellationToken.IsCancellationRequested;
        }

        public void Init(ICfgHsi88 cfgHsi88, ICfgDebounce cfgDebounce)
        {
            _cfgHsi88 = cfgHsi88;
            _cfgDebounce = cfgDebounce;

            _cancellationToken = new CancellationTokenSource();

            try
            {
                if (!_cfgHsi88.CfgSimulation.IsEnabled)
                    Open();
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex, "Hsi device can not be opened");

                throw;
            }
        }

        /// <summary>
        /// Examples:
        ///     t\r             terminal mode
        ///     m\r             current states
        ///     s010205\r       apply number of shift registers
        ///     v\r             query version
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool Send(string command)
        {
            if (_fs == null) return false;
            if (!_fs.CanWrite) return false;
            if (!command.EndsWith("\r")) return false;

            try
            {
                var bytes = Encoding.ASCII.GetBytes(command);
                _fs.Write(bytes, 0, command.Length);
                return true;
            }
            catch (Exception ex)
            {
                ex.ShowException();
            }

            return false;
        }

        private readonly Random _rnd = new();
        private short _recentCountUpValue = 1;

        public async Task RunSimulationAsync()
        {
            await Task.Run(async () =>
            {
                var tkn = _cancellationToken.Token;

                while (!tkn.IsCancellationRequested)
                {
                    var noOfDevices = _cfgHsi88.NumberMax;
                    var deviceNo = 0;
                    var m = $"m{noOfDevices:D2}";
                    
                    if (_cfgHsi88.CfgSimulation.SimulationMode.Equals("random", StringComparison.OrdinalIgnoreCase))
                    {
                        for (var i = 0; i < noOfDevices * 2; ++i)
                        {
                            if (i % 2 == 0)
                            {
                                m += (deviceNo + 1).ToString("D2");
                                ++deviceNo;
                            }

                            var state = _rnd.Next(0, 255);
                            m += state.ToString("X2");
                        }
                    }
                    else if (_cfgHsi88.CfgSimulation.SimulationMode.Equals("countUp", StringComparison.OrdinalIgnoreCase))
                    {
                        for (var i = 0; i < noOfDevices; ++i)
                        {
                            ++_recentCountUpValue;

                            // TEMPORARY TEST, only the first 6 pins of the first port
                            // TEMPORARY TEST, only the first 6 pins of the first port
                            // TEMPORARY TEST, only the first 6 pins of the first port
                            //if (_recentCountUpValue > 6) _recentCountUpValue = 1;

                            // this can be kept, is not relevant for the temporary test
                            if (_recentCountUpValue > (noOfDevices * 16))
                                _recentCountUpValue = 1;

                            m += (i + 1).ToString("D2");

                            var mask = 1 << (_recentCountUpValue - 1);
                            var state = 0x0000 | mask;
                            m += state.ToString("X4");
                        }
                    }

                    var data = new DeviceInterfaceData(m);
                    if (!data.EventData)
                        DataReceived?.Invoke(this, data);

                    if (_cfgHsi88.CfgSimulation.IsEnabled)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(_cfgHsi88.CfgSimulation.BetweenTicksMs), tkn);
                    }
                    else
                    {
                        var intervalMs = _cfgDebounce.CheckInterval;
                        if (intervalMs < 10) intervalMs = 50;
                        await Task.Delay(TimeSpan.FromMilliseconds(intervalMs), tkn);
                    }
                }
            });
        }

        public async Task RunAsync()
        {
            await Task.Run(async () =>
            {
                const int bufferSize = 131072;
                var buffer = new byte[bufferSize];
                var bytesRead = 0;

                try
                {
                    _fs = new FileStream(_handle, FileAccess.ReadWrite, buffer.Length, isAsync: false);
                }
                catch (Exception ex)
                {
                    ex.ShowException();

                    Failed?.Invoke(this, new DeviceInterfaceEventArgs(ex.GetExceptionMessages()));

                    // highly fatal, no S88-data will be received nor handled
                    return;
                }

                // init terminal mode
                Send("t1\r");
                bytesRead = _fs.Read(buffer, 0, buffer.Length);
                var d0 = new DeviceInterfaceData(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                DataReceived?.Invoke(this, d0);

                // query device information/version
                Send($"v\r");
                bytesRead = _fs.Read(buffer, 0, buffer.Length);
                var d2 = new DeviceInterfaceData(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                DataReceived?.Invoke(this, d2);

                // init number of shift registers
                Send($"s{_cfgHsi88.NumberLeft:D2}{_cfgHsi88.NumberMiddle:D2}{_cfgHsi88.NumberRight:D2}\r");
                bytesRead = _fs.Read(buffer, 0, buffer.Length);
                var d1 = new DeviceInterfaceData(Encoding.ASCII.GetString(buffer, 0, bytesRead));
                DataReceived?.Invoke(this, d1);

                var tkn = _cancellationToken.Token;

                while (!tkn.IsCancellationRequested)
                {
                    Send("m\r");

                    bytesRead = _fs.Read(buffer, 0, buffer.Length);

                    if (bytesRead > 0)
                    {
                        var text = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        var lines = text.Split(new[] { '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var line in lines)
                        {
                            if (string.IsNullOrEmpty(line)) continue;
                            var m = line.Trim();
                            var data = new DeviceInterfaceData(m);
                            if (!data.EventData)
                                DataReceived?.Invoke(this, data);

                            //Trace.WriteLine($"NumberOfModules: {data.NumberOfModules}");
                            //for (var i = 1; i <= data.NumberOfModules; ++i)
                            //    Trace.Write($"{i} {data.States[i]} ");
                            //Trace.WriteLine(string.Empty);
                            //Trace.WriteLine(m);
                        }
                    }

                    var intervalMs = _cfgDebounce.CheckInterval;
                    if (intervalMs < 10) intervalMs = 50;
                    await Task.Delay(TimeSpan.FromMilliseconds(intervalMs));
                }
            });
        }
    }
}

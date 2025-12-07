// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using libUtilities;
using railyHsi88Usb.Cfg;

namespace railyHsi88Usb.HSI88USB
{
    internal class HsiStateData
    {
        private readonly ICfgDebounce _cfgDebounce;
        public const int PortIdOffset = 1; // offset of "1" because HSI-88-USB starts the object id with 1 for s88 modules
        public const int NumberOfPins = 16;
        public const int MaxNoOfDevices = 31;
        public const string ZeroHexValues = "0000";
        public const char Bin1 = '1';
        public const char Bin0 = '0';

        private string _nativeHexData = ZeroHexValues;

        public string NativeHexData
        {
            get => _nativeHexData;
            private set => _nativeHexData = value?.Trim() ?? ZeroHexValues;
        }

        public string NativeBinaryData => Converters.ToBinary(NativeHexData);

        private readonly Dictionary<int, DateTime> _states = new();

        public HsiStateData(ICfgDebounce cfgDebounce)
        {
            _cfgDebounce = cfgDebounce;

            for (var i = 0; i < NumberOfPins; ++i)
                _states.Add(i, DateTime.MinValue);
        }

        /// <summary>
        /// Updates the internal information about a S88-device and its pin states.
        /// When no update is applied the method returns `false`, in any other
        /// cases `true` is returned.
        /// This method provides so-called "Entprellung" to avoid undisired
        /// internal updates when the track/s88 feedback is dirty and flickers the signal.
        /// </summary>
        /// <param name="dataset"></param>
        /// <param name="isSimulationMode"></param>
        /// <returns></returns>
        public bool Update(string dataset, bool isSimulationMode = false)
        {
            if (string.IsNullOrEmpty(dataset)) return false;

            var recentBinary = Converters.ToBinary(NativeHexData);
            var updateBinary = Converters.ToBinary(dataset);

            var sbin = new[]
            {
                Bin0, Bin0, Bin0, Bin0,
                Bin0, Bin0, Bin0, Bin0,
                Bin0, Bin0, Bin0, Bin0,
                Bin0, Bin0, Bin0, Bin0
            };

            var changeCounter = 0;

            for (var i = 0; i < NumberOfPins; ++i)
            {
                sbin[i] = recentBinary[i];

                var cOld = recentBinary[i];
                var cNew = updateBinary[i];
                if (cOld == cNew)
                {
                    _states[i] = DateTime.Now;

                    continue;
                }

                var stateTime = _states[i];
                var deltaMs = (DateTime.Now - stateTime).TotalMilliseconds;

                var bounceOn = _cfgDebounce.On;
                var bounceOff = _cfgDebounce.Off;

                //
                // IMPORTANT
                // Attention, when we are in simulation mode, the bouncing check should be disabled.
                // It has been shown that active bouncing leads to obscured values, meaning that
                // values get distorted because the bouncing is not properly calculated during rapid value changes.
                //

                if (cOld == Bin0) // was off, must wait bounceOff before update
                {
                    if (deltaMs > bounceOn && !isSimulationMode)
                    {
                        sbin[i] = Bin1;
                        ++changeCounter;
                    }
                    else if (isSimulationMode)
                    {
                        sbin[i] = Bin1;
                        ++changeCounter;
                    }
                }
                else if (cOld == Bin1) // was on, must wait bounceOn beforeUpdate
                {
                    if (deltaMs > bounceOff && !isSimulationMode)
                    {
                        sbin[i] = Bin0;
                        ++changeCounter;
                    }
                    else if (isSimulationMode)
                    {
                        sbin[i] = Bin0;
                        ++changeCounter;
                    }
                }
            }

            NativeHexData = Converters.ToHex(new string(sbin));

            return changeCounter > 0;
        }

#if DEBUG

        /// <summary>
        /// Displays the current states of the pins for the specified port.
        /// This method is primarily used for debugging purposes, providing output
        /// of pin states in binary format, including the time difference since the last state change.
        /// </summary>
        /// <param name="portId">The ID of the port whose states will be displayed.</param>
        /// <param name="specificPin">
        /// The specific pin to display the state for. If -1 is passed, states for all pins will be displayed.
        /// </param>
        /// <param name="showHeadline">Indicates whether a headline ("ShowStates") should be included in the output.</param>
        /// <param name="logCallback">
        /// An optional callback for logging the output. If null, no output will be produced.
        /// </param>
        public void ShowStates(uint portId, int specificPin = -1, bool showHeadline = true, Action<string> logCallback = null)
        {
            var recentBinary = Converters.ToBinary(NativeHexData);

            if (showHeadline)
                logCallback?.Invoke("ShowStates");

            if (specificPin == -1)
            {
                for (var i = 0; i < NumberOfPins; ++i)
                {
                    var dt = _states[i];
                    var delta = DateTime.Now - dt;
                    logCallback?.Invoke($"   {recentBinary[i]}  {dt}  {delta.TotalMilliseconds}");
                }
            }
            else
            {
                var pinRight = NumberOfPins - specificPin;
                var dt = _states[pinRight];
                var delta = DateTime.Now - dt;
                logCallback?.Invoke($"{portId}:{specificPin}   {recentBinary[pinRight]}  {dt}  {delta.TotalMilliseconds}");
            }
        }
#endif
    }
}

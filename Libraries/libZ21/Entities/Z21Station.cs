// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities;
using Newtonsoft.Json.Linq;
using System;
// ReSharper disable RedundantDefaultMemberInitializer

namespace libZ21.Entities
{
    public class Z21Station : Z21Entity
    {
        public override EntityType Type => EntityType.Z21Station;

        #region Entity data provided by Z21

        // Hauptstrom: 87 mA
        // Programmstrom: 0 mA
        // Gefiltert: 61 mA
        // Temperatur: 32 °C
        // Versorgung: 20125 mV
        // Gleisspannung: 17985 mV
        // Status: 0x00
        // Flags: 0x20

        public bool TrackOn { get; private set; } = false;
        public bool IsShortCircuit { get; private set; } = false;
        public string Name => Globals.Z21Identifier;
        public int Hauptstrom { get; private set; } = 0;
        public int Programmstrom { get; private set; } = 0;
        public int Gefiltert { get; private set; } = 0;
        public int Temperatur { get; private set; } = 0;
        public int Versorgung { get; private set; } = 0;
        public int Gleisspannung { get; private set; } = 0;
        public int Status { get; private set; } = 0;
        public int Flags { get; private set; } = 0;

        public string HardwareType { get; set; } = string.Empty;
        public string FirmwareVersion { get; set; } = string.Empty;

        #endregion

        public Z21Station()
        {
            ObjectId = Globals.ID_EV_Z21Station;
        }

        public override JObject ToJsonObject()
        {
            var o = new JObject
            {
                ["driverName"] = DriverName,
                ["objectId"] = Globals.ID_EV_Z21Station,
                ["name"] = DisplayName,
                ["trackOn"] = TrackOn,
                ["isShortCircuit"] = IsShortCircuit,
                ["mainCurrent"] = Hauptstrom,
                ["progCurrent"] = Programmstrom,
                ["mainCurrentFilter"] = Gefiltert,
                ["temperatur"] = Temperatur,
                ["supplyVoltage"] = Versorgung,
                ["trackVoltage"] = Gleisspannung,
                ["hardwareType"] = HardwareType,
                ["firmwareVersion"] = FirmwareVersion
            };

            return o;
        }

        public override bool ParseData(object data)
        {
            if (data is Z21StatusInterpreter z21Status)
            {
                var changed = false;

                if (TrackOn != !z21Status.IsTrackVoltageOff)
                {
                    TrackOn = !z21Status.IsTrackVoltageOff;
                    changed = true;
                }

                return changed;
            }

            if (data is HardwareInfo hwInfo)
            {
                var changed = false;

                if (!HardwareType.Equals($"{hwInfo.Hardware}"))
                {
                    HardwareType = $"{hwInfo.Hardware}";
                    changed = true;
                }

                var fv = $"{hwInfo.FirmwareVersion.Major}.{hwInfo.FirmwareVersion.Minor}";
                if (!FirmwareVersion.Equals(fv))
                {
                    FirmwareVersion = fv;
                    changed = true;
                }

                return changed;
            }

            if (data is ShortCircuitState shortCircuitState)
            {
                var changed = false;

                if (IsShortCircuit != shortCircuitState.On)
                {
                    IsShortCircuit = shortCircuitState.On;
                    changed = true;
                }

                return changed;
            }

            if (data is OnOffState onOffState)
            {
                var changed = false;

                if (TrackOn != onOffState.On)
                {
                    TrackOn = onOffState.On;

                    if (onOffState.On)
                    {
                        IsShortCircuit = false;
                    }

                    changed = true;
                }

                if (IsShortCircuit != onOffState.ShortCircuit && onOffState.ShortCircuit.HasValue)
                {
                    IsShortCircuit = onOffState.ShortCircuit.Value;
                    changed = true;
                }

                return changed;
            }

            if (data is SystemState systemState)
            {
                var changed = false;
                
                if (systemState.MainCurrent != Hauptstrom)
                {
                    changed = true;
                    Hauptstrom = systemState.MainCurrent;
                }

                if (systemState.ProgCurrent != Programmstrom)
                {
                    changed = true;
                    Programmstrom = systemState.ProgCurrent;
                }

                if (systemState.MainCurrentFilter != Gefiltert)
                {
                    changed = true;
                    Gefiltert = systemState.MainCurrentFilter;
                }

                if (systemState.Temperature != Temperatur)
                {
                    changed = true;
                    Temperatur = systemState.Temperature;
                }

                if (systemState.SupplyVoltage != Versorgung)
                {
                    changed = true;
                    Versorgung = systemState.SupplyVoltage;
                }

                if (systemState.TrackVoltage != Gleisspannung)
                {
                    changed = true;
                    Gleisspannung = systemState.TrackVoltage;
                }

                return changed;
            }

            return false;
        }
    }
}

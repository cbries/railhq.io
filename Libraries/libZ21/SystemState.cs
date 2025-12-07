// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libZ21
{
    public class OnOffState
    {
        public bool On { get; set; }
        public bool? ShortCircuit { get; set; } = null;
    }

    public class ShortCircuitState
    {
        public bool On { get; set; }
    }

    public class SystemState
    {
        public int MainCurrent { get; private set; }
        public int ProgCurrent { get; private set; }
        public int MainCurrentFilter { get; private set; }
        public int Temperature { get; private set; }
        public int SupplyVoltage { get; private set; }
        public int TrackVoltage { get; private set; }
        public byte Status { get; private set; }
        public byte Flags { get; private set; }

        public SystemState(byte[] data)
        {
            if (data == null || data.Length < 18)
                throw new ArgumentException("Ungültige SystemState-Daten.");

            MainCurrent = BitConverter.ToInt16(data, 4);
            ProgCurrent = BitConverter.ToInt16(data, 6);
            MainCurrentFilter = BitConverter.ToInt16(data, 8);
            Temperature = BitConverter.ToInt16(data, 10);
            SupplyVoltage = BitConverter.ToUInt16(data, 12);
            TrackVoltage = BitConverter.ToUInt16(data, 14);
            Status = data[16];
            Flags = data[17];
        }

        public override string ToString()
        {
            return $"SystemState – Hauptstrom: {MainCurrent} mA, Programmstrom: {ProgCurrent} mA, " +
                   $"Gefiltert: {MainCurrentFilter} mA, Temperatur: {Temperature} °C, " +
                   $"Versorgung: {SupplyVoltage} mV, Gleisspannung: {TrackVoltage} mV, " +
                   $"Status: 0x{Status:X2}, Flags: 0x{Flags:X2}";
        }
    }

}

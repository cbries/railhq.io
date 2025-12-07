// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace libZ21
{
    public class CanDetectorMessage
    {
        public ushort NetworkIdRaw { get; }
        public string NetworkIdHex => $"0x{NetworkIdRaw:X4}";
        public ushort ModuleAddressRaw { get; }  // wie im Protokoll
        public int ModuleAddress => ModuleAddressRaw + 1;  // für Anzeige/Logik
        public byte Port { get; }
        public byte Type { get; }
        public ushort Value1Raw { get; }
        public ushort? Value2Raw { get; }

        public CanDetectorMessage(byte[] data)
        {
            if (data == null || data.Length < 10)
                throw new ArgumentException("CAN_DETECTOR data must be at least 10 bytes long.");

            NetworkIdRaw = BitConverter.ToUInt16(data, 4);
            ModuleAddressRaw = BitConverter.ToUInt16(data, 6);
            Port = data[8];
            Type = data[9];
            Value1Raw = BitConverter.ToUInt16(data, 10);

            if (data.Length >= 14)
                Value2Raw = BitConverter.ToUInt16(data, 12);
        }

        public bool IsOccupancyStatus => Type == 0x01;

        public bool IsRailCom => Type >= 0x11 && Type <= 0x1F;

        public bool IsOccupied =>
            IsOccupancyStatus &&
            (Value1Raw & 0x1000) != 0; // Bit 12 gesetzt = besetzt

        public (int address, string direction)? Lok1
        {
            get
            {
                if (!IsRailCom || GetAddress(Value1Raw) == 0)
                    return null;
                return (GetAddress(Value1Raw), GetDirection(Value1Raw));
            }
        }

        public (int address, string direction)? Lok2
        {
            get
            {
                if (!IsRailCom || Value2Raw == null || GetAddress(Value2Raw.Value) == 0)
                    return null;
                return (GetAddress(Value2Raw.Value), GetDirection(Value2Raw.Value));
            }
        }

        private int GetAddress(ushort raw) => raw & 0x3FFF;

        private string GetDirection(ushort raw)
        {
            int dir = (raw & 0xC000) >> 14;

            return dir switch
            {
                0b00 => "Keine Richtung / Halt",
                0b01 => "Vorwärts",
                0b10 => "Rückwärts",
                0b11 => "Ungültig oder unbekannt",
                _ => "Unbekannt" // Fallback
            };
        }

        public override string ToString()
        {
            if (IsOccupancyStatus)
                return $"[CAN_DETECTOR ({NetworkIdHex})] Modul={ModuleAddress}, Port={Port}, Status={Value1Raw:X4}, Occupied={IsOccupied}";
            
            if (IsRailCom)
            {
                var loks = new List<string>();
                if (Lok1 != null)
                    loks.Add($"Lok1: {Lok1.Value.address} ({Lok1.Value.direction})");
                if (Lok2 != null)
                    loks.Add($"Lok2: {Lok2.Value.address} ({Lok2.Value.direction})");
                return $"[CAN_DETECTOR ({NetworkIdHex})] Modul={ModuleAddress}, Port={Port}, RailCom: {string.Join(", ", loks)}";
            }
            return $"[CAN_DETECTOR ({NetworkIdHex})] Modul={ModuleAddress}, Port={Port}, Typ={Type:X2}";
        }
    }

}

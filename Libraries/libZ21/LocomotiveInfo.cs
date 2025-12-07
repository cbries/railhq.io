// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text;
// ReSharper disable InconsistentNaming

namespace libZ21
{
    public class LocomotiveInfo
    {
        public enum SpeedStepMode
        {
            Unknown = -1,

            Dcc14 = 0,
            Dcc28 = 2,
            Dcc128 = 4,

            Mm14 = 10,
            Mm14F4 = 12,
            Mm28F4 = 14
        }

        /// <summary>
        /// Die Anzahl der verarbeiteten Datensätze für ein empfangenes Datagramm.
        /// </summary>
        public int ParameterCount { get; private set; }

        public int Address { get; private set; }

        public bool IsMMDecoder { get; private set; }

        // Die Lok wird von einem anderen X-BUS Handregler gesteuert („besetzt“)
        public bool Occupied { get; private set; }

        // Fahrstufeninformation: 0=14, 2=28, 4=128
        // 0: DCC 14 Fahrstufen bzw. MMI mit 14 Fahrstufen und F0
        // 2: DCC 28 Fahrstufen bzw. MMII mit 14 realen Fahrstufen und F0-F4
        // 4: DCC 128 Fahrstufen
        //    bzw. MMII mit 28 realen Fahrstufen (Licht-Trick) und F0-F4
        public byte SpeedlevelInfo { get; private set; }

        public SpeedStepMode SpeedlevelProtocol => (IsMMDecoder, SpeedlevelInfo) switch
        {
            (true, 0) => SpeedStepMode.Mm14,
            (true, 2) => SpeedStepMode.Mm14F4,
            (true, 4) => SpeedStepMode.Mm28F4,

            (false, 0) => SpeedStepMode.Dcc14,
            (false, 2) => SpeedStepMode.Dcc28,
            (false, 4) => SpeedStepMode.Dcc128,

            _ => SpeedStepMode.Unknown
        };

        public string Protocol
        {
            get
            {
                switch (SpeedlevelProtocol)
                {
                    case SpeedStepMode.Dcc14: return "DCC14";
                    case SpeedStepMode.Dcc28: return "DCC28";
                    case SpeedStepMode.Dcc128: return "DCC128";
                    case SpeedStepMode.Mm14: return "MM14";
                    case SpeedStepMode.Mm14F4: return "MM14";
                    case SpeedStepMode.Mm28F4: return "MM28";
                }

                return string.Empty;
            }
        }

        public string GetSpeedlevelDescription()
        {
            return SpeedlevelProtocol switch
            {
                SpeedStepMode.Dcc14 => "DCC 14 Fahrstufen",
                SpeedStepMode.Dcc28 => "DCC 28 Fahrstufen",
                SpeedStepMode.Dcc128 => "DCC 128 Fahrstufen",

                SpeedStepMode.Mm14 => "MM I mit 14 Fahrstufen + F0",
                SpeedStepMode.Mm14F4 => "MM II mit 14 realen Fahrstufen + F0–F4",
                SpeedStepMode.Mm28F4 => "MM II mit 28 realen Fahrstufen (Licht-Trick) + F0–F4",

                _ => "Unbekannter Fahrstufenmodus"
            };
        }

        // Richtung: 1=vorwärts
        public bool Direction { get; private set; }

        public byte Speedlevel { get; private set; }
        public bool DoubleTraction { get; private set; }
        public bool SmartSearch { get; private set; }
        public List<bool> Functions { get; } = new();

        public static LocomotiveInfo FromBytes(byte[] db)
        {
            var loco = new LocomotiveInfo();

            if (db.Length >= 2)
            {
                loco.ParameterCount = 1;
                loco.Address = ((db[0] & 0x3F) << 8) + db[1];
            }

            if (db.Length >= 3)
            {
                // DB2 = 000MBKKK (Bit 7 ... Bit 0)
                // │ │ └───> KKK = Fahrstufeninfo (Bits 0–2)
                // │ └─────> B = Lok besetzt (Bit 3)
                // └───────> M = MM-Kennung (Bit 4)

                loco.ParameterCount = 3;

                // Bit 3 (0x08): Besetzt-Flag ("Occupied")
                loco.Occupied = (db[2] & 0x08) == 0x08;

                // Bits 0-2 (0x07): Fahrstufeninformation (SpeedlevelInfo)
                loco.SpeedlevelInfo = (byte)(db[2] & 0x07);

                // Bit 4 (0x10): MM-Dekoder-Kennung
                loco.IsMMDecoder = (db[2] & 0x10) == 0x10;
            }

            if (db.Length >= 4)
            {
                loco.ParameterCount = 5;
                loco.Direction = (db[3] & 0x80) == 0x80;
                loco.Speedlevel = (byte)(db[3] & 0x7F);
            }

            if (db.Length >= 5)
            {
                loco.ParameterCount = 8;
                loco.DoubleTraction = (db[4] & 0x40) == 0x40;
                loco.SmartSearch = (db[4] & 0x20) == 0x20;
                AddBits(db[4], loco.Functions, 0x10, 0x01, 0x02, 0x04, 0x08);
            }

            if (db.Length >= 6) AddBits(db[5], loco.Functions);
            if (db.Length >= 7) AddBits(db[6], loco.Functions);
            if (db.Length >= 8) AddBits(db[7], loco.Functions);
            if (db.Length >= 9) AddBits(db[8], loco.Functions);

            return loco;
        }

        private static void AddBits(byte value, List<bool> target, params byte[] bits)
        {
            if (bits.Length == 0)
                bits = [0x01, 0x02, 0x04, 0x08, 0x10, 0x20, 0x40, 0x80];

            foreach (var bit in bits)
                target.Add((value & bit) == bit);
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Adresse: {Address} (0x{Address:X})");
            sb.AppendLine($"ParameterCount: {ParameterCount}");
            sb.AppendLine($"Belegt: {(Occupied ? "Ja" : "Nein")}");
            sb.AppendLine($"FahrstufeInfo: {SpeedlevelInfo}");
            sb.AppendLine($"Richtung: {(Direction ? "Vorwärts" : "Rückwärts")}");
            sb.AppendLine($"Fahrstufe: {Speedlevel}");
            sb.AppendLine($"Doppeltraktion: {(DoubleTraction ? "Ja" : "Nein")}");
            sb.AppendLine($"SmartSearch: {(SmartSearch ? "Ja" : "Nein")}");

            for (var i = 0; i < Functions.Count; i++)
            {
                var s = $"F{i}: {(Functions[i] ? "An" : "Aus")}".PadRight(8);
                sb.Append(s);
                //if (i % 4 == 0)
                    sb.AppendLine(string.Empty);
            }

            return sb.ToString().Trim();
        }
    }
}

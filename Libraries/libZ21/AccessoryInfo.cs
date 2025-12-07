// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


using System;

namespace libZ21
{
    public class AccessoryInfo
    {
        public int Address { get; set; } = -1;

        /// <summary>
        /// Gibt den tatsächlichen Schaltzustand der Weiche wieder:
        /// true = Position „grün“ (P=1), false = Position „rot“ (P=0)
        /// null = Zustand unbekannt oder ungültig
        /// </summary>
        public bool? State { get; set; } = null;

        /// <summary>
        /// Gibt an, ob ein gültiger Schaltzustand gemeldet wurde (ZZ = 01 oder 10)
        /// </summary>
        public bool HasValidState => State.HasValue;

        public static AccessoryInfo FromBytes(byte[] db)
        {
            if (db == null || db.Length != 3)
                throw new ArgumentException("LAN_X_TURNOUT_INFO muss genau 3 Byte Daten enthalten.");

            int address = (db[0] << 8) + db[1] + 1;

            byte zz = (byte)(db[2] & 0x03); // Nur die letzten zwei Bits sind relevant

            bool? state = zz switch
            {
                0b00 => null,        // Noch nicht geschaltet
                0b01 => false,       // Weiche steht laut Schaltbefehl P=0
                0b10 => true,        // Weiche steht laut Schaltbefehl P=1
                0b11 => null,        // Ungültige Kombination
                _ => null
            };

            return new AccessoryInfo
            {
                Address = address,
                State = state
            };
        }

        public override string ToString()
        {
            return $"Addr: {Address}, State: {(State.HasValue ? (State.Value ? "P=1" : "P=0") : "Unbekannt")}";
        }
    }

}

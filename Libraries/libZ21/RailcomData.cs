// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libZ21
{
    public class RailcomData
    {
        public int LocoAddress { get; private set; }
        public int ReceiveCounter { get; private set; }
        public int ErrorCounter { get; private set; }
        public byte Options { get; private set; }
        public byte Speed { get; private set; }
        public byte QoS { get; private set; }

        public RailcomData(byte[] data)
        {
            if (data == null || data.Length < 16)
                throw new ArgumentException("Ungültige Railcom-Daten.");

            LocoAddress = BitConverter.ToUInt16(data, 4);
            ReceiveCounter = BitConverter.ToInt32(data, 6);
            ErrorCounter = BitConverter.ToUInt16(data, 10);
            Options = data[13];
            Speed = data[14];
            QoS = data[15];
        }

        public override string ToString()
        {
            return $"Railcom – Lok: {LocoAddress} (0x{LocoAddress:X}), " +
                   $"Empfangen: {ReceiveCounter}, Fehler: {ErrorCounter}, " +
                   $"Optionen: 0x{Options:X2}, Geschwindigkeit: {Speed}, QoS: {QoS}";
        }
    }
}

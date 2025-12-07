// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

// ReSharper disable InconsistentNaming

namespace libZ21
{
    public partial class Z21
    {
        public delegate void LAN_X_TURNOUT_INFO(AccessoryInfo accessoryInfo);
        public delegate void LAN_X_BC_TRACK_POWER_OFF();
        public delegate void LAN_X_BC_TRACK_POWER_ON();
        public delegate void LAN_X_PROGRAMMING_MODE();
        public delegate void LAN_X_BC_TRACK_SHORT_CIRCUIT();
        public delegate void LAN_X_CV_NACK_SC();
        public delegate void LAN_X_CV_NACK();
        public delegate void LAN_X_UNKNOWN_COMMAND();
        public delegate void LAN_X_STATUS_CHANGED(byte state);
        public delegate void LAN_X_GET_VERSION(double xBusVersion, byte identifier);
        public delegate void LAN_X_CV_RESULT(int cvAddress, byte value);
        public delegate void LAN_X_BC_STOPPED();
        public delegate void LAN_X_LOCO_INFO(LocomotiveInfo info);
        public delegate void LAN_X_GET_FIRMWARE_VERSION(double firmwareVersion);

        private LAN_X_TURNOUT_INFO call_LAN_X_TURNOUT_INFO;
        private LAN_X_BC_TRACK_POWER_OFF call_LAN_X_BC_TRACK_POWER_OFF;
        private LAN_X_BC_TRACK_POWER_ON call_LAN_X_BC_TRACK_POWER_ON;
        private LAN_X_PROGRAMMING_MODE call_LAN_X_PROGRAMMING_MODE;
        private LAN_X_BC_TRACK_SHORT_CIRCUIT call_LAN_X_BC_TRACK_SHORT_CIRCUIT;
        private LAN_X_CV_NACK_SC call_LAN_X_CV_NACK_SC;
        private LAN_X_CV_NACK call_LAN_X_CV_NACK;
        private LAN_X_UNKNOWN_COMMAND call_LAN_X_UNKNOWN_COMMAND;
        private LAN_X_STATUS_CHANGED call_LAN_X_STATUS_CHANGED;
        private LAN_X_GET_VERSION call_LAN_X_GET_VERSION;
        private LAN_X_CV_RESULT call_LAN_X_CV_RESULT;
        private LAN_X_BC_STOPPED call_LAN_X_BC_STOPPED;
        private LAN_X_LOCO_INFO call_LAN_X_LOCO_INFO;
        private LAN_X_GET_FIRMWARE_VERSION call_LAN_X_GET_FIRMWARE_VERSION;

        private void SolveXTunnel(Z21_XBus_Header header, byte[] db)
        {
            switch (header)
            {
                case Z21_XBus_Header.TURNOUT_INFO:
                    try
                    {
                        var instance = AccessoryInfo.FromBytes(db);
                        if(instance != null)
                            call_LAN_X_TURNOUT_INFO?.Invoke(instance);
                    }
                    catch
                    {
                        // ignore
                    }
                    break;
                case Z21_XBus_Header.PROGRAMMING:
                    if (db.Length == 1)
                    {
                        if (db[0] == 0x00) call_LAN_X_BC_TRACK_POWER_OFF?.Invoke();
                        else if (db[0] == 0x01) call_LAN_X_BC_TRACK_POWER_ON?.Invoke();
                        else if (db[0] == 0x02) call_LAN_X_PROGRAMMING_MODE?.Invoke();
                        else if (db[0] == 0x08) call_LAN_X_BC_TRACK_SHORT_CIRCUIT?.Invoke();
                        else if (db[0] == 0x12) call_LAN_X_CV_NACK_SC?.Invoke();
                        else if (db[0] == 0x13) call_LAN_X_CV_NACK?.Invoke();
                        else if (db[0] == 0x82) call_LAN_X_UNKNOWN_COMMAND?.Invoke();
                    }
                    break;
                case Z21_XBus_Header.GET_FIRMWARE:
                    if (db.Length == 3)
                    {
                        var major = (db[1] & 0x0F) + ((db[1] >> 4) * 10); // Umwandeln DBC-Format
                        var minor = (db[2] & 0x0F) + ((db[2] >> 4) * 10); // Umwandeln DBC-Format
                        var FW = major + (minor * 0.01);
                        if (db[0] == 0x0A) call_LAN_X_GET_FIRMWARE_VERSION?.Invoke(FW);
                    }
                    break;
                case Z21_XBus_Header.STATUS_CHANGE:
                    if (db.Length == 2)
                    {
                        if (db[0] == 0x22) call_LAN_X_STATUS_CHANGED?.Invoke(db[1]);
                    }
                    break;
                case Z21_XBus_Header.X_VERSION:
                    if (db.Length == 3)
                    {
                        var major = (db[1] & 0x0F) + ((db[1] >> 4) * 10); // Umwandeln DBC-Format
                        var minor = (db[2] & 0x0F) + ((db[2] >> 4) * 10); // Umwandeln DBC-Format
                        var Version = major + (minor * 0.01);
                        if (db[0] == 0x21) call_LAN_X_GET_VERSION?.Invoke(Version, db[2]);
                    }
                    break;
                case Z21_XBus_Header.CV_RESULT:
                    if (db.Length == 4)
                    {
                        var addr = (db[0] << 8) + db[1] + 1;
                        if (db[0] == 0x14) call_LAN_X_CV_RESULT?.Invoke(addr, db[3]);
                    }
                    break;
                case Z21_XBus_Header.BC_STOPPED:
                    if (db.Length == 1)
                    {
                        if (db[0] == 0x00) call_LAN_X_BC_STOPPED?.Invoke();
                    }
                    break;
                case Z21_XBus_Header.LOCO_INFO:
                    {
                        var loco = LocomotiveInfo.FromBytes(db);
                        call_LAN_X_LOCO_INFO?.Invoke(loco);
                    }
                    break;
            }
        }
        
        public void Register_LAN_X_TURNOUT_INFO(LAN_X_TURNOUT_INFO function) { call_LAN_X_TURNOUT_INFO = function; }
        public void Register_LAN_X_BC_TRACK_POWER_OFF(LAN_X_BC_TRACK_POWER_OFF function) { call_LAN_X_BC_TRACK_POWER_OFF = function; }
        public void Register_LAN_X_BC_TRACK_POWER_ON(LAN_X_BC_TRACK_POWER_ON function) { call_LAN_X_BC_TRACK_POWER_ON = function; }
        public void Register_LAN_X_PROGRAMMING_MODE(LAN_X_PROGRAMMING_MODE function) { call_LAN_X_PROGRAMMING_MODE = function; }
        public void Register_LAN_X_BC_TRACK_SHORT_CIRCUIT(LAN_X_BC_TRACK_SHORT_CIRCUIT function) { call_LAN_X_BC_TRACK_SHORT_CIRCUIT = function; }
        public void Register_LAN_X_CV_NACK_SC(LAN_X_CV_NACK_SC function) { call_LAN_X_CV_NACK_SC = function; }
        public void Register_LAN_X_CV_NACK(LAN_X_CV_NACK function) { call_LAN_X_CV_NACK = function; }
        public void Register_LAN_X_UNKNOWN_COMMAND(LAN_X_UNKNOWN_COMMAND function) { call_LAN_X_UNKNOWN_COMMAND = function; }
        public void Register_LAN_X_STATUS_CHANGED(LAN_X_STATUS_CHANGED function) { call_LAN_X_STATUS_CHANGED = function; }
        public void Register_LAN_X_GET_VERSION(LAN_X_GET_VERSION function) { call_LAN_X_GET_VERSION = function; }
        public void Register_LAN_X_CV_RESULT(LAN_X_CV_RESULT function) { call_LAN_X_CV_RESULT = function; }
        public void Register_LAN_X_BC_STOPPED(LAN_X_BC_STOPPED function) { call_LAN_X_BC_STOPPED = function; }
        public void Register_LAN_X_LOCO_INFO(LAN_X_LOCO_INFO function) { call_LAN_X_LOCO_INFO = function; }
        public void Register_LAN_X_GET_FIRMWARE_VERSION(LAN_X_GET_FIRMWARE_VERSION function) { call_LAN_X_GET_FIRMWARE_VERSION = function; }

        public static byte[] LAN_X_SET_TURNOUT_Command(int Adresse, bool Abzweig, bool Q_Modus, bool aktivieren)
        {
            Adresse--;
            byte Header = 0x53;
            var DB0 = (byte)(Adresse >> 8);
            var DB1 = (byte)(Adresse & 0xFF);
            var DB2 = GetDB2(aktivieren, !Abzweig, Q_Modus);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2);
            byte[] bytes = [0x09, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, XOR];
            return bytes;
        }

        public void LAN_X_SET_TURNOUT(int Adresse, bool Abzweig, bool Q_Modus, bool aktivieren)
        {
            var bytes = LAN_X_SET_TURNOUT_Command(Adresse, Abzweig, Q_Modus, aktivieren);
            SendCommand(bytes, bytes.Length);
        }

        public static byte[] LAN_X_SET_SIGNAL_Command(int Adresse, bool Zustand, bool qmode)
        {
            Adresse--;
            if (Adresse < 0) return null; // Nicht schalten, da Adresse 0
            byte Header = 0x53;
            var DB0 = (byte)(Adresse >> 8);
            var DB1 = (byte)(Adresse & 0xFF);
            var DB2 = GetDB2(true, Zustand, qmode);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2);
            byte[] bytes = [0x09, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, XOR];
            return bytes;
        }

        public void LAN_X_SET_SIGNAL(int Adresse, bool Zustand)
        {
            var bytes = LAN_X_SET_SIGNAL_Command(Adresse, Zustand, QMode);
            if (bytes == null) return;
            SendCommand(bytes, bytes.Length);
        }

        public void LAN_X_SET_SIGNAL_OFF(int Adresse, bool Zustand)
        {
            Adresse--;
            if (Adresse < 0) return; //Nicht schalten, da Adresse 0
            byte Header = 0x53;
            var DB0 = (byte)(Adresse >> 8);
            var DB1 = (byte)(Adresse & 0xFF);
            var DB2 = GetDB2(false, Zustand, QMode);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2);
            byte[] SendBytes = { 0x09, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, XOR };
            SendCommand(SendBytes, 9);
        }

        public void LAN_X_GET_TURNOUT_INFO(int Adresse)
        {
            Adresse--;
            if (Adresse < 0) return; //Nicht anfragen, da Adresse 0
            byte Header = 0x43;
            var DB0 = (byte)(Adresse >> 8);
            var DB1 = (byte)(Adresse & 0xFF);
            var XOR = (byte)(Header ^ DB0 ^ DB1);
            byte[] SendBytes = { 0x08, 0x00, 0x40, 0x00, Header, DB0, DB1, XOR };
            SendCommand(SendBytes, 8);
        }

        public void LAN_X_GET_TURNOUT_INFO(List<int> Adressen)
        {
            if (Adressen == null) return;
            if (Adressen.Count == 0) return;

            var SendBytes = new List<byte>();

            foreach (var Adresse in Adressen)
            {
                if (Adresse < 1) continue; //Nicht anfragen, da Adresse 0
                var Packet = new List<byte>
                {
                    0x08,
                    0x00,
                    0x40,
                    0x00,
                    0x43, //Header
                    (byte)((Adresse - 1) >> 8),
                    (byte)((Adresse - 1) & 0xFF)
                };
                Packet.Add((byte)(Packet[4] ^ Packet[5] ^ Packet[6])); // XOR Checksumme
                SendBytes.AddRange(Packet);
            }
            SendCommand(SendBytes.ToArray(), SendBytes.Count);
        }
    }
}

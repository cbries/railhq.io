// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUtilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Timers;
using static libZ21.LokFahrstufen;

// ReSharper disable InconsistentNaming

namespace libZ21
{
    public delegate void DataReceived(object sender, byte[] data);

    public enum Z21InstanceType
    {
        Gateway,
        Controller
    }

    public enum Z21_Header : byte
    {
        SERIAL_NUMBER = 0x10,
        CODE_STATUS = 0x18,
        HardwareInfo = 0x1A,
        X_BUS_TUNNEL = 0x40,
        BROADCAST_FLAGS = 0x51,
        GET_LOK_MODE = 0x60,
        GET_FKT_DEC_MODE = 0x70,
        RM_BUS = 0x80,
        SYSTEM_STATE = 0x84,
        RAILCOM = 0x88,
        LOCONET_RX = 0xA0,
        LOCONET_TX = 0xA1,
        LOCONET_LAN = 0xA2,
        LOCONET_ADDR = 0xA3,
        LOCONET_DETECTOR = 0xA4,
        CAN_DETECTOR = 0xC4,
    }

    public enum Z21_XBus_Header : byte
    {
        TURNOUT_INFO = 0x43,
        PROGRAMMING = 0x61,
        STATUS_CHANGE = 0x62,
        X_VERSION = 0x63,
        CV_RESULT = 0x64,
        BC_STOPPED = 0x81,
        LOCO_INFO = 0xEF,
        GET_FIRMWARE = 0xF3,
    }


    public partial class Z21
    {
        public event DataReceived OnDataReceived;

        private readonly Z21Connection _z21Connection;

        public Z21InstanceType InstanceType { get; }

        public bool IsController() => InstanceType == Z21InstanceType.Controller;

        /// <summary>
        /// Wenn `connection` nicht gesetzt, also `null` ist, dann
        /// werden keine Commands versendet. Diese Instanz dient
        /// dann rein zum Interpretieren der Z21-Daten.
        /// Alle Daten werden dann über `OnDataReceived` an
        /// angemeldete Handler weitergereicht.
        /// </summary>
        /// <param name="connection"></param>
        public Z21(Z21Connection connection)
        {
            _z21Connection = connection;

            if (_z21Connection != null)
            {
                Logging.Log.Debug($"*** Construct Z21-library as Gateway");

                InstanceType = Z21InstanceType.Gateway;

                _z21Connection.OnConnected += Z21ConnectionOnConnected;
                _z21Connection.OnStatusUpdated += Z21ConnectionOnStatusUpdated;
                _z21Connection.OnReceived += Z21ConnectionOnReceived;
                _z21Connection.Connect();
            }
            else
            {
                Logging.Log.Debug($"*** Construct Z21-library as Controller");

                InstanceType = Z21InstanceType.Controller;
            }
        }

        #region Z21Connection

        private void Z21ConnectionOnStatusUpdated(object sender, bool state, bool init)
        {
            Trace.WriteLine($"State: {state}  Init: {init}");
        }

        private void Z21ConnectionOnConnected(object sender)
        {
            var z21conn = sender as IZ21Connection;
            if (z21conn == null) return;

            Trace.WriteLine($"Connected to z21 ({z21conn.Z21Ip}:{z21conn.Z21Port})!");
        }

        private void Z21ConnectionOnReceived(object sender, byte[] data)
        {
            var z21conn = sender as IZ21Connection;
            if (z21conn == null) return;

            while (data.Length > 0)
            {
                var length = data[0] + data[1] * 256;
                var msgdata = data.Take(length).ToArray();

                //
                // Wir senden die Daten weiter,
                // lokal in dieser Instanz wird nichts gehandhabt.
                //
                OnDataReceived?.Invoke(this, msgdata);

                //var errorCode = SolveZ21Msg(msgdata);
                //if (errorCode != 0) call_LAN_ERROR?.Invoke(errorCode);

                data = data.Skip(length).ToArray();
            }
        }

        #endregion

        public static byte[] PowerOnBytes => [0x07, 0x00, 0x40, 0x00, 0x21, 0x81, 0xA0];
        public static byte[] PowerOffBytes => [0x07, 0x00, 0x40, 0x00, 0x21, 0x80, 0xA1];
        public static byte[] EmergencyStop => [0x06, 0x00, 0x40, 0x00, 0x80, 0x80];

        private bool QMode { get; set; } = true; // Beim Schalten Queue-Modus ein aus

        /// <summary>
        /// Queue-Modus aktivieren. 
        /// Wenn aktiv Z21 wartet mit dem Befehl bis Schienentransfer es erlaubt und sendet es 4mal hintereinander. 
        /// Wenn aus Befehl muss direkt gesendet werden. Es kann aber nur eine Weiche/Signal auf einmal angesteuert werden
        /// </summary>
        /// <param name="on_off"></param>
        public void SetQMode(bool on_off)
        {
            QMode = on_off;
        }

        public int ParseData(byte[] data)
        {
            return SolveZ21Msg(data);
        }

        private int SolveZ21Msg(byte[] data)
        {
            if (data == null) return 0;
            if (data.Length < 2) return 0;

            var length = data[0] + data[1] * 256;
            if (length != data.Length) return Z21ErrorCode.FALSE_LENGTH;

            length -= 4;

            switch ((Z21_Header)data[2])
            {
                #region Loconet
                
                case Z21_Header.LOCONET_RX: // LocoNet Rx
                case Z21_Header.LOCONET_TX: // LocoNet Tx
                case Z21_Header.LOCONET_LAN: // LocoNet LAN
                case Z21_Header.LOCONET_ADDR: // LocoNet Adresse
                case Z21_Header.LOCONET_DETECTOR: // LocoNet Rückmelder
                    return SolveZ21LoconetMsg(data);

                #endregion


                case Z21_Header.SERIAL_NUMBER:
                    if (length != 4) return Z21ErrorCode.FALSE_LENGTH;
                    call_LAN_GET_SERIAL_NUMBER?.Invoke(data[4] + (data[5] << 8) + (data[6] << 16) + (data[7] << 24));
                    break;

                case Z21_Header.CODE_STATUS:
                    if (length != 1) return Z21ErrorCode.FALSE_LENGTH;
                    call_LAN_GET_CODE?.Invoke(data[4]);
                    break;

                case Z21_Header.HardwareInfo: //  LAN GET HWINFO  2.20 (19)
                    {
                        HardwareTyp hardwareTyp;
                        var iv = (data[7] << 24) + (data[6] << 16) + (data[5] << 8) + (data[4]);
                        switch (iv)
                        {
                            case 0x00000200: hardwareTyp = HardwareTyp.Z21_OLD; break;
                            case 0x00000201: hardwareTyp = HardwareTyp.Z21_NEW; break;
                            case 0x00000202: hardwareTyp = HardwareTyp.SMARTRAIL; break;
                            case 0x00000203: hardwareTyp = HardwareTyp.z21_SMALL; break;
                            case 0x00000204: hardwareTyp = HardwareTyp.z21_START; break;
                            case 0x00000205: hardwareTyp = HardwareTyp.SINGLE_BOOSTER; break;
                            case 0x00000206: hardwareTyp = HardwareTyp.DUAL_BOOSTER; break;
                            case 0x00000211: hardwareTyp = HardwareTyp.Z21_XL; break;
                            case 0x00000212: hardwareTyp = HardwareTyp.XL_BOOSTER; break;
                            case 0x00000301: hardwareTyp = HardwareTyp.Z21_SWITCH_DECODER; break;
                            case 0x00000302: hardwareTyp = HardwareTyp.Z21_SIGNAL_DECODER; break;
                            default: hardwareTyp = HardwareTyp.None; break;
                        }
                        FirmwareVersionInfo firmware = new(data[9], data[8]);
                        var hwInfo = new HardwareInfo(hardwareTyp, firmware);
                        call_LAN_GET_HWINFO?.Invoke(hwInfo);
                    }
                    break;

                case Z21_Header.X_BUS_TUNNEL:
                    byte XOR_Check = 0x00;
                    for (var i = 4; i < data.Count(); i++)
                    {
                        XOR_Check = (byte)(XOR_Check ^ data[i]);
                    }
                    if (XOR_Check != 0x00) return Z21ErrorCode.FALSE_CHECK;

                    var para_db = data.Skip(5).ToArray();
                    para_db = para_db.Take(para_db.Count() - 1).ToArray();
                    if (data.Length <= 4) return Z21ErrorCode.FALSE_LENGTH;
                    SolveXTunnel((Z21_XBus_Header)data[4], para_db);

                    break;
                
                case Z21_Header.BROADCAST_FLAGS: // Broadcast-Flags
                    if (length != 4) return Z21ErrorCode.FALSE_LENGTH;
                    var flags = data[4] + (data[5] << 8) + (data[6] << 16) + (data[7] << 24);
                    call_LAN_GET_BROADCASTFLAGS?.Invoke(flags);
                    break;
                
                case Z21_Header.GET_LOK_MODE: // Lok-Status
                    if (length != 3) return Z21ErrorCode.FALSE_LENGTH;
                    var Adresse = data[4] + (data[5] << 8) + 1;
                    call_LAN_GET_LOCOMODE?.Invoke(Adresse, data[6]);
                    break;
                
                // Funktionsdecoder-Adresse 2 Byte, big endian d.h. zuerst high byte, gefolgt von low byte.
                // Modus 0 ... DCC Format
                // 1 ... MM Format
                case Z21_Header.GET_FKT_DEC_MODE: // Fx-Decoder Status
                    if (length != 3) return Z21ErrorCode.FALSE_LENGTH;
                    var FxAdresse = data[4] + (data[5] << 8) + 1;
                    call_LAN_GET_TURNOUTMODE?.Invoke(FxAdresse, data[6]);
                    break;
                
                case Z21_Header.RM_BUS: // Rückmelde-Bus
                    if (length != 11) return Z21ErrorCode.FALSE_LENGTH;
                    var GruppenIndex = data[4];
                    var RMStatus = data.Skip(5).ToArray();
                    call_LAN_RMBUS_DATACHANGED?.Invoke(GruppenIndex, RMStatus);
                    break;
                
                case Z21_Header.SYSTEM_STATE: // System-Status
                    if (length != 16) return Z21ErrorCode.FALSE_LENGTH;

                    try
                    {
                        var state = new SystemState(data);
                        call_LAN_SYSTEMSTATE_DATACHANGED?.Invoke(state);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                    break;
                
                case Z21_Header.RAILCOM: // Railcom
                    if (length != 13) return Z21ErrorCode.FALSE_LENGTH;

                    try
                    {
                        var railcom = new RailcomData(data);
                        call_LAN_RAILCOM_DATACHANGED?.Invoke(railcom);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                    break;
        
                case Z21_Header.CAN_DETECTOR:
                    {
                        if (length < 10) return Z21ErrorCode.FALSE_LENGTH;
                        var msg = new CanDetectorMessage(data);
                        call_LAN_CAN_DETECTOR?.Invoke(msg);
                    }
                    break;

                default:
                    return Z21ErrorCode.WRONG_HEADER;
            }
            return 0;
        }

        private int SolveZ21LoconetMsg(byte[] data)
        {
            var length = data[0] + data[1] * 256;
            if (length != data.Length) return Z21ErrorCode.FALSE_LENGTH;

            length -= 4;

            switch ((Z21_Header)data[2])
            {
                case Z21_Header.LOCONET_RX:      //LocoNet Rx
                    var LocoNet_RX = data.Skip(5).ToArray();
                    call_LAN_LOCONET_Z21_RX?.Invoke(LocoNet_RX);
                    break;
                case Z21_Header.LOCONET_TX:      //LocoNet Tx
                    var LocoNet_TX = data.Skip(5).ToArray();
                    call_LAN_LOCONET_Z21_TX?.Invoke(LocoNet_TX);
                    break;
                case Z21_Header.LOCONET_LAN:      //LocoNet LAN
                    var LocoNet_LAN = data.Skip(5).ToArray();
                    call_LAN_LOCONET_FROM_LAN?.Invoke(LocoNet_LAN);
                    break;
                case Z21_Header.LOCONET_ADDR:      //LocoNet Adresse
                    if (length != 3) return Z21ErrorCode.FALSE_LENGTH;
                    var LOCONet_Adresse = data[4] + (data[5] << 8) + 1;
                    call_LAN_LOCONET_DISPATCH_ADDR?.Invoke(LOCONet_Adresse, data[6]);
                    break;
                case Z21_Header.LOCONET_DETECTOR:      //LocoNet Rückmelder
                    if (length != 3) return Z21ErrorCode.FALSE_LENGTH;
                    var Reporter_Adresse = data[5] + (data[6] << 8) + 1;
                    call_LAN_LOCONET_DETECTOR?.Invoke(data[4], Reporter_Adresse);
                    break;
                default:
                    return Z21ErrorCode.WRONG_HEADER;
            }

            return 0;
        }

        private void SendCommand(byte[] data, int size)
        {
            if (IsController()) return;
            _z21Connection?.SendCommand(data, size);
        }

        public static byte[] GetSerialNumberCommand()
        {
            byte[] sendBytes = [0x04, 0x00, 0x10, 0x00];
            return sendBytes;
        }

        public void GetSerialNumber()
        {
            var bytes = GetSerialNumberCommand();
            SendCommand(bytes, bytes.Length);
        }

        public static byte[] GetHardwareInfoCommand()
        {
            var bytes = new byte[4];
            bytes[0] = 0x04;
            bytes[1] = 0;
            bytes[2] = 0x1A;
            bytes[3] = 0;
            return bytes;
        }

        public void GetHardwareInfo()
        {
            var bytes = GetHardwareInfoCommand();
            SendCommand(bytes, 4);
        }

        public static byte[] GetFirmwareVersionCommand()
        {
            byte[] sendBytes = [0x07, 0x00, 0x40, 0x00, 0xF1, 0x0A, 0xFB];
            return sendBytes;
        }

        public void GetFirmwareVersion()
        {
            var bytes = GetFirmwareVersionCommand();
            SendCommand(bytes, bytes.Length);
        }

        public void GetBroadcastFlags()
        {
            byte[] SendBytes = { 0x04, 0x00, 0x51, 0x00 };
            SendCommand(SendBytes, 4);
        }

        public void SetBroadcastFlags(Flags flags)
        {
            var tempdata = flags.GetAsBytes();
            byte[] SendBytes = { 0x08, 0x00, 0x50, 0x00, tempdata[0], tempdata[1], tempdata[2], tempdata[3] };
            SendCommand(SendBytes, 8);
        }

        public static byte[] GetStatusCommand()
        {
            byte[] sendBytes = [0x07, 0x00, 0x40, 0x00, 0x21, 0x24, 0x05];
            return sendBytes;
        }
        public void GetStatus()
        {
            var bytes = GetStatusCommand();
            SendCommand(bytes, bytes.Length);
        }

        public static byte[] GetLocomotiveInfoCommand(int locomotiveAddress)
        {
            const byte Header = 0xE3;
            const byte DB0 = 0xF0;
            var DB1 = Addr_High(locomotiveAddress);
            var DB2 = Addr_Low(locomotiveAddress);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2);
            byte[] sendBytes = [0x09, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, XOR];
            return sendBytes;
        }

        public void GetLocomotiveInfo(int locomotiveAddress)
        {
            var bytes = GetLocomotiveInfoCommand(locomotiveAddress);
            SendCommand(bytes, bytes.Length);
        }

        public static byte[] SetLocomotiveDriveCommand(int locomotiveAddress, int speed, int direction, MaxSpeedsteps maxSpeedSteps)
        {
            var SendeFahrStufe = (int)maxSpeedSteps;
            if (SendeFahrStufe == 4) SendeFahrStufe = 3;  // Kompatibilitätsumwandlung für Protokoll

            const byte Header = 0xE4;
            var DB0 = (byte)(0x10 + SendeFahrStufe);
            var DB1 = Addr_High(locomotiveAddress);
            var DB2 = Addr_Low(locomotiveAddress);
            var DB3 = FahrstufeToProtokol(speed, (int)direction, maxSpeedSteps);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2 ^ DB3);

            byte[] sendBytes = { 0x0A, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, DB3, XOR };

            return sendBytes;
        }

        /// <summary>
        /// Sendet einen Fahrbefehl an eine Lokomotive über das Z21-Protokoll.
        /// <paramref name="speedLevel"/> gibt an, welches Fahrstufenformat verwendet wird (FS14, FS28 oder FS128).
        /// <paramref name="speed"/> ist die Zielgeschwindigkeit innerhalb dieses Formats.
        /// <paramref name="direction"/> gibt die gewünschte Fahrtrichtung an (Vorwärts oder Rückwärts).
        /// </summary>
        /// <param name="address">Digitale Adresse der Lokomotive (1–9999).</param>
        /// <param name="speed">Zielgeschwindigkeit innerhalb des gewählten Fahrstufenformats.</param>
        /// <param name="direction">Fahrtrichtung (Vorwärts oder Rückwärts).</param>
        /// <param name="maxSspeedSteps"></param>
        public void SetLocomotiveDrive(int locomotiveAddress, int speed, int direction, MaxSpeedsteps maxSspeedSteps)
        {
            var bytes = SetLocomotiveDriveCommand(locomotiveAddress, speed, direction, maxSspeedSteps);
            SendCommand(bytes, bytes.Length);
        }

        public static byte[] SetLocomotveFunctionCommand(int locomotiveAddress, byte state, byte Funktion)
        {
            state = (byte)(state & 0x3);
            Funktion = (byte)(Funktion & 0x3F);
            byte Header = 0xE4;
            byte DB0 = 0xF8;
            var DB1 = Addr_High(locomotiveAddress);
            var DB2 = Addr_Low(locomotiveAddress);
            var DB3 = (byte)((state << 6) + Funktion);
            var XOR = (byte)(Header ^ DB0 ^ DB1 ^ DB2 ^ DB3);
            byte[] bytes = { 0x0A, 0x00, 0x40, 0x00, Header, DB0, DB1, DB2, DB3, XOR };
            return bytes;
        }

        public void SetLocomotiveFunction(int locomotiveAddress, byte state, byte Funktion)
        {
            var bytes = SetLocomotveFunctionCommand(locomotiveAddress, state, Funktion);
            SendCommand(bytes, 10);
        }

        public static byte[] GetSystemStateCommand()
        {
            byte[] bytes = [0x04, 0x00, 0x85, 0x00];
            return bytes;
        }

        public void GetSystemState()
        {
            var bytes = GetSystemStateCommand();
            SendCommand(bytes, bytes.Length);
        }
        
        private static byte GetDB2(bool activate, bool output, bool qmode)
        {
            //DB2 Byte               10Q0A00P 
            byte DB2 = 0b10000000;
            if (qmode) DB2 |= 0b00100000; // Queue-Modus aktivieren: Befehl wird in ein FiFo eingereiht und dann 4 mal an Gleis gesendet
            if (activate) DB2 |= 0b00001000; // Ausgang aktivieren
            if (output) DB2 |= 0b00000001; // Schaltausgang wählen
            return DB2;
        }

        public static byte[] RmbusGetdataCommand(byte groupIndex)
        {
            byte[] bytes = [0x05, 0x00, 0x81, 0x00, groupIndex];
            return bytes;
        }

        public void RmbusGetdata(byte groupIndex)
        {
            var bytes = RmbusGetdataCommand(groupIndex);
            SendCommand(bytes, bytes.Length);
        }

        #region Heartbeat

        private static Timer HeartbeatTimer;

        public void InitFlags()
        {
            if (IsController()) return;

            if (HeartbeatTimer != null) return;
            HeartbeatTimer = new Timer(5000);
            HeartbeatTimer.Elapsed += HeartbeatTimerOnElapsed;
            HeartbeatTimer.AutoReset = true;
            HeartbeatTimer.Enabled = true;
        }

        private void HeartbeatTimerOnElapsed(object sender, ElapsedEventArgs e)
        {
            if (_z21Connection is { Connected: false }) return;
            var temp = Broadcast.GetFlagConfig();
            SetBroadcastFlags(temp); //Flags neu setzen 
        }

        #endregion
    }
}

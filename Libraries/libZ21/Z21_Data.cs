// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


// ReSharper disable InconsistentNaming
namespace libZ21
{
    public partial class Z21
    {
        #region Delegates

        public delegate void LAN_ERROR(int errorCode);
        public delegate void LAN_CONNECT_STATUS(bool state, bool init);

        public delegate void LAN_GET_SERIAL_NUMBER(int serial);
        public delegate void LAN_GET_CODE(byte code);
        public delegate void LAN_GET_HWINFO(HardwareInfo hwInfo);

        public delegate void LAN_GET_BROADCASTFLAGS(int broadcastFlags);
        public delegate void LAN_GET_LOCOMODE(int address, byte mode);
        public delegate void LAN_GET_TURNOUTMODE(int address, byte mode);
        public delegate void LAN_RMBUS_DATACHANGED(byte GruppenIndex, byte[] RMStatus);
        public delegate void LAN_SYSTEMSTATE_DATACHANGED(SystemState state);
        public delegate void LAN_RAILCOM_DATACHANGED(RailcomData data);
        public delegate void LAN_LOCONET_Z21_RX(byte[] locoNet);
        public delegate void LAN_LOCONET_Z21_TX(byte[] locoNet);
        public delegate void LAN_LOCONET_FROM_LAN(byte[] LocoNet);
        public delegate void LAN_LOCONET_DISPATCH_ADDR(int address, byte result);
        public delegate void LAN_LOCONET_DETECTOR(byte type, int reportAddress);
        public delegate void LAN_CAN_DETECTOR(CanDetectorMessage msg);

        #endregion

        #region Callbacks

        private LAN_ERROR call_LAN_ERROR;
        private LAN_CONNECT_STATUS call_LAN_CONNECT_STATUS;

        private LAN_GET_SERIAL_NUMBER call_LAN_GET_SERIAL_NUMBER;
        private LAN_GET_CODE call_LAN_GET_CODE;
        private LAN_GET_HWINFO call_LAN_GET_HWINFO;

        private LAN_GET_BROADCASTFLAGS call_LAN_GET_BROADCASTFLAGS;
        private LAN_GET_LOCOMODE call_LAN_GET_LOCOMODE;
        private LAN_GET_TURNOUTMODE call_LAN_GET_TURNOUTMODE;
        private LAN_RMBUS_DATACHANGED call_LAN_RMBUS_DATACHANGED;
        private LAN_SYSTEMSTATE_DATACHANGED call_LAN_SYSTEMSTATE_DATACHANGED;
        private LAN_RAILCOM_DATACHANGED call_LAN_RAILCOM_DATACHANGED;
        private LAN_LOCONET_Z21_RX call_LAN_LOCONET_Z21_RX;
        private LAN_LOCONET_Z21_TX call_LAN_LOCONET_Z21_TX;
        private LAN_LOCONET_FROM_LAN call_LAN_LOCONET_FROM_LAN;
        private LAN_LOCONET_DISPATCH_ADDR call_LAN_LOCONET_DISPATCH_ADDR;
        private LAN_LOCONET_DETECTOR call_LAN_LOCONET_DETECTOR;
        private LAN_CAN_DETECTOR call_LAN_CAN_DETECTOR;

        #endregion

        #region Register

        public void Register_LAN_ERROR(LAN_ERROR function) { call_LAN_ERROR = function; }
        public void Register_LAN_CONNECT_STATUS(LAN_CONNECT_STATUS function) { call_LAN_CONNECT_STATUS = function; }
        public void Register_LAN_GET_SERIAL_NUMBER(LAN_GET_SERIAL_NUMBER function) { call_LAN_GET_SERIAL_NUMBER = function; }
        public void Register_LAN_GET_CODE(LAN_GET_CODE function) { call_LAN_GET_CODE = function; }
        public void Register_LAN_GET_HWINFO(LAN_GET_HWINFO function) { call_LAN_GET_HWINFO = function; }
        public void Register_LAN_GET_BROADCASTFLAGS(LAN_GET_BROADCASTFLAGS function) { call_LAN_GET_BROADCASTFLAGS = function; }
        public void Register_LAN_GET_LOCOMODE(LAN_GET_LOCOMODE function) { call_LAN_GET_LOCOMODE = function; }
        public void Register_LAN_GET_TURNOUTMODE(LAN_GET_TURNOUTMODE function) { call_LAN_GET_TURNOUTMODE = function; }
        public void Register_LAN_RMBUS_DATACHANGED(LAN_RMBUS_DATACHANGED function) { call_LAN_RMBUS_DATACHANGED = function; }
        public void Register_LAN_SYSTEMSTATE_DATACHANGED(LAN_SYSTEMSTATE_DATACHANGED function) { call_LAN_SYSTEMSTATE_DATACHANGED = function; }
        public void Register_LAN_RAILCOM_DATACHANGED(LAN_RAILCOM_DATACHANGED function) { call_LAN_RAILCOM_DATACHANGED = function; }
        public void Register_LAN_LOCONET_Z21_RX(LAN_LOCONET_Z21_RX function) { call_LAN_LOCONET_Z21_RX = function; }
        public void Register_LAN_LOCONET_Z21_TX(LAN_LOCONET_Z21_TX function) { call_LAN_LOCONET_Z21_TX = function; }
        public void Register_LAN_LOCONET_FROM_LAN(LAN_LOCONET_FROM_LAN function) { call_LAN_LOCONET_FROM_LAN = function; }
        public void Register_LAN_LOCONET_DISPATCH_ADDR(LAN_LOCONET_DISPATCH_ADDR function) { call_LAN_LOCONET_DISPATCH_ADDR = function; }
        public void Register_LAN_LOCONET_DETECTOR(LAN_LOCONET_DETECTOR function) { call_LAN_LOCONET_DETECTOR = function; }
        public void Register_LAN_CAN_DETECTOR(LAN_CAN_DETECTOR function) { call_LAN_CAN_DETECTOR = function; }


        #endregion
    }
}

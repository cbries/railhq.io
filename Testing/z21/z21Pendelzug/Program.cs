// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libZ21;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace z21Pendelzug
{
    internal class Program
    {
        private const int LokAdresse = 9;
        private const int Geschwindigkeit = 30;

        public static Z21Connection Z21Connection { get; private set; }
        public static Z21 Z21GatewayInstance { get; private set; }
        public static Z21 Z21ControllerInstance { get; private set; }

        private static LokFahrStateMachine _pendelStateMachine;

        private static readonly ManualResetEventSlim ConnectionWaitHandle = new(false);
        
        static void InitZ21Connection()
        {
            Z21Connection = new Z21Connection
            {
                Z21Ip = Z21Connection.DefaultHost,
                Z21Port = Z21Connection.Port
            };

            Z21GatewayInstance = new Z21(Z21Connection);
            Z21GatewayInstance.OnDataReceived += Z21GatewayInstanceOnDataReceived;

            new Thread(() =>
            {
                while (!Z21Connection.Connected)
                {
                    Thread.Sleep(100);
                }
                ConnectionWaitHandle.Set();
            }).Start();

            Trace.WriteLine(ConnectionWaitHandle.Wait(TimeSpan.FromSeconds(5))
                ? $"Connected to {Z21Connection.Z21Ip}:{Z21Connection.Z21Port}!"
                : $"Connection to {Z21Connection.Z21Ip}:{Z21Connection.Z21Port} timed out!");
        }

        static void InitZ21Controller()
        {
            Z21ControllerInstance = new Z21(Z21Connection);

            Z21ControllerInstance.Register_LAN_CONNECT_STATUS(CallbackConnectStatus);

            Z21ControllerInstance.Register_LAN_GET_SERIAL_NUMBER(CallBack_GET_SERIAL_NUMBER);
            Z21ControllerInstance.Register_LAN_GET_BROADCASTFLAGS(CallBack_Z21_Broadcast_Flags);
            Z21ControllerInstance.Register_LAN_SYSTEMSTATE_DATACHANGED(CallBack_Z21_System_Status);
            Z21ControllerInstance.Register_LAN_GET_HWINFO(Register_LAN_GET_HWINFO);

            Z21ControllerInstance.Register_LAN_X_TURNOUT_INFO(CallBack_LAN_X_TURNOUT_INFO);
            Z21ControllerInstance.Register_LAN_X_GET_FIRMWARE_VERSION(CallBack_LAN_X_GET_FIRMWARE_VERSION);
            Z21ControllerInstance.Register_LAN_X_LOCO_INFO(CallBack_Z21_LokUpdate);
            Z21ControllerInstance.Register_LAN_LOCONET_Z21_TX(CallBack_LoconetZ21Tx);
            Z21ControllerInstance.Register_LAN_LOCONET_Z21_RX(CallBack_LoconetZ21Rx);

            Z21ControllerInstance.Register_LAN_RMBUS_DATACHANGED(CallBackLanRmbusDatachanged);
            Z21ControllerInstance.Register_LAN_CAN_DETECTOR(CallbackCanDetector);
            Z21ControllerInstance.Register_LAN_RAILCOM_DATACHANGED(RegisterLanRailcomDatachanged);

            Z21ControllerInstance.SetQMode(true);

            Z21ControllerInstance.InitFlags();

            Z21ControllerInstance.GetStatus();
            Z21ControllerInstance.GetSystemState();
            Z21ControllerInstance.GetSerialNumber();
            Z21ControllerInstance.GetHardwareInfo(); ;
            Z21ControllerInstance.GetFirmwareVersion();
        }
        
        private static void Z21GatewayInstanceOnDataReceived(object sender, byte[] data)
        {
            Z21ControllerInstance?.ParseData(data);
        }

        private static void HandlePendelzug(CanDetectorMessage msg)
        {
            _pendelStateMachine?.UpdateSensors(msg);
        }
        
        static async Task Main()
        {
            InitZ21Connection();
            InitZ21Controller();

            _pendelStateMachine = new LokFahrStateMachine(LokAdresse, Geschwindigkeit, Z21ControllerInstance);
            var cts = new CancellationTokenSource();
            _ = _pendelStateMachine.RunAsync(cts.Token);

            //while (true)
            //{
            //    Z21ControllerInstance.RmbusGetdata(0);

            //    Console.WriteLine("Enter...");
            //    Console.ReadKey();
            //}

            //while (true)
            //{
            //    var direction = LokFahrstufen.Vorwaerts;
            //    var fahrstufe128 = LokFahrstufen.Fahstufe128;

            //    Z21ControllerInstance.SetLocomotiveDrive(9, 30, direction, fahrstufe128);
            //    await Task.Delay(2000);
            //    Z21ControllerInstance.SetLocomotiveDrive(9, 0, direction, fahrstufe128);
            //    await Task.Delay(1000);

            //    direction = LokFahrstufen.Rueckwaerts;

            //    Z21ControllerInstance.SetLocomotiveDrive(9, 30, direction, fahrstufe128);
            //    await Task.Delay(2000);
            //    Z21ControllerInstance.SetLocomotiveDrive(9, 0, direction, fahrstufe128);
            //    await Task.Delay(1000);
            //}

            //Console.WriteLine($"State: {stateTask.Status}");

            //for (var i = 500; i < 502; ++i)
            //{
            //    Console.WriteLine($"Adresse: {i}");

            //    Z21Start.LAN_X_SET_TURNOUT(i, true, false, false);
            //    System.Threading.Thread.Sleep(500);
            //    Z21Start.LAN_X_SET_TURNOUT(i, false, false, false);
            //    System.Threading.Thread.Sleep(500);
            //}

            Console.WriteLine("Pendelbetrieb läuft. Drücke eine Taste zum Beenden...");
            await Task.Run(() =>
            {
                Console.ReadKey();    // blockiert die Task bis Taste gedrückt
                cts.Cancel();         // StateMachine beenden
            });
        }

        private static void RegisterLanRailcomDatachanged(RailcomData data)
        {
            Trace.WriteLine($"{data}");
        }

        private static void Register_LAN_GET_HWINFO(HardwareInfo hardwareInfo)
        {
            Trace.WriteLine($"Hardware: {hardwareInfo.Hardware}  Firmware: {hardwareInfo.FirmwareVersion}");
        }

        private static void CallbackCanDetector(CanDetectorMessage msg)
        {
            Trace.WriteLine($"{msg}");

            HandlePendelzug(msg);
        }

        private static void CallBackLanRmbusDatachanged(byte gruppenIndex, byte[] rmStatus)
        {
            _pendelStateMachine?.UpdateSensors(gruppenIndex, rmStatus);

            Trace.WriteLine($"RMBUS: {gruppenIndex}    ");
            for (int i = 0; i < rmStatus.Length; ++i)
                Trace.WriteLine($"    {i}   ->   {rmStatus[i]}");
            Trace.WriteLine(string.Empty);
        }

        private static void CallBack_LoconetZ21Tx(byte[] locoNet)
        {
            Trace.WriteLine($"LoconetZ21Tx: {locoNet}");
        }

        private static void CallBack_LoconetZ21Rx(byte[] locoNet)
        {
            Trace.WriteLine($"LoconetZ21Rx: {locoNet}");
        }

        private static void CallBack_Z21_LokUpdate(LocomotiveInfo info)
        {
            Trace.WriteLine($"Lokomotive:");
            Trace.WriteLine($"{info}");
        }

        private static void CallBack_Z21_Broadcast_Flags(int broadcastFlags)
        {
            Trace.WriteLine($"Broadcast Flags: {broadcastFlags}");
        }

        private static void CallBack_Z21_System_Status(SystemState state)
        {
            //Trace.WriteLine($"{state}");
        }

        private static void CallBack_LAN_X_GET_FIRMWARE_VERSION(double fwVersion)
        {
            var info = fwVersion.ToString("F2", CultureInfo.CreateSpecificCulture("en-GB"));
            Trace.WriteLine($"Firmware: {info}");
        }

        private static void CallBack_LAN_X_TURNOUT_INFO(AccessoryInfo accessoryInfo)
        {
            Trace.WriteLine($"{accessoryInfo}");
        }

        private static void CallBack_GET_SERIAL_NUMBER(int serial)
        {
            Trace.WriteLine($"Serial Number: {serial}");
        }

        private static void CallbackConnectStatus(bool status, bool init)
        {
            Trace.WriteLine($"Connected: {status}  Init: {init}");
        }
    }
}

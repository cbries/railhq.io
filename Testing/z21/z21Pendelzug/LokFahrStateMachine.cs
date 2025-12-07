// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libZ21;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable ConvertToPrimaryConstructor
// ReSharper disable InconsistentNaming

namespace z21Pendelzug
{
    public class LokFahrStateMachine
    {
        public const int Modul0 = 0;

        private enum FahrState
        {
            StartVorwärts,
            WarteAufSensor1_1,
            StopVorwärts,
            PauseNachVorwärts,
            StartRückwärts,
            WarteAufSensor1_2,
            StopRückwärts,
            PauseNachRückwärts
        }

        private FahrState _currentState = FahrState.StartVorwärts;
        private readonly int _lokAdresse;
        private readonly int _geschwindigkeit;
        private readonly Z21 _z21;

        private readonly HoldBool Sensor1_1 = new(TimeSpan.FromSeconds(1));
        private readonly HoldBool Sensor1_2 = new(TimeSpan.FromSeconds(1));

        public void UpdateSensors(CanDetectorMessage msg)
        {
            Trace.WriteLine(msg);
        }

        public void UpdateSensors(byte gruppenIndex, byte[] rmStatus)
        {
            if (gruppenIndex == Modul0)
            {
                Sensor1_1.Value = (rmStatus[0] & 0b00000001) != 0;
                Sensor1_2.Value = (rmStatus[0] & 0b00000010) != 0;
            }
        }

        public LokFahrStateMachine(int lokAdresse, int geschwindigkeit, Z21 z21)
        {
            _lokAdresse = lokAdresse;
            _geschwindigkeit = geschwindigkeit;
            _z21 = z21;
        }

        public async Task RunAsync(CancellationToken cancellationToken = default)
        {
            const int refreshRmAfterSeconds = 100;
            int countSeconds = refreshRmAfterSeconds;

            //_z21.SetLocomotiveFunction(9, 1, 0);
            //_z21.SetLocomotiveFunction(9, 1, 1);
            //_z21.SetLocomotiveFunction(9, 1, 2);
            //_z21.SetLocomotiveFunction(9, 1, 3);
            //_z21.SetLocomotiveFunction(9, 1, 4);

            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);

                countSeconds -= 26;
                if (countSeconds < 0)
                {
                    _z21.RmbusGetdata(Modul0);
                    countSeconds = refreshRmAfterSeconds;
                }

                var directionF = LokFahrstufen.Vorwaerts;
                var directionR = LokFahrstufen.Rueckwaerts;
                var fahrstufe128 = LokFahrstufen.MaxSpeedsteps.F128;

                switch (_currentState)
                {
                    case FahrState.StartVorwärts:
                        _z21.SetLocomotiveDrive(_lokAdresse, _geschwindigkeit, directionF, fahrstufe128);
                        _currentState = FahrState.WarteAufSensor1_1;
                        break;

                    case FahrState.WarteAufSensor1_1:
                        if (Sensor1_1.Value)
                            _currentState = FahrState.StopVorwärts;
                        else
                            await Task.Delay(50, cancellationToken);
                        break;

                    case FahrState.StopVorwärts:
                        _z21.SetLocomotiveDrive(_lokAdresse, 0, directionF, fahrstufe128);
                        _currentState = FahrState.PauseNachVorwärts;
                        break;

                    case FahrState.PauseNachVorwärts:
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        _currentState = FahrState.StartRückwärts;
                        break;

                    case FahrState.StartRückwärts:
                        _z21.SetLocomotiveDrive(_lokAdresse, _geschwindigkeit, directionR, fahrstufe128);
                        _currentState = FahrState.WarteAufSensor1_2;
                        break;

                    case FahrState.WarteAufSensor1_2:
                        if (Sensor1_2.Value)
                            _currentState = FahrState.StopRückwärts;
                        else
                            await Task.Delay(50, cancellationToken);
                        break;

                    case FahrState.StopRückwärts:
                        _z21.SetLocomotiveDrive(_lokAdresse, 0, directionR, fahrstufe128);
                        _currentState = FahrState.PauseNachRückwärts;
                        break;

                    case FahrState.PauseNachRückwärts:
                        await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
                        _currentState = FahrState.StartVorwärts;
                        break;
                }
            }
        }
    }

}

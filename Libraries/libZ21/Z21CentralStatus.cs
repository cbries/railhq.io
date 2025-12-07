// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

// ReSharper disable ConvertToPrimaryConstructor

namespace libZ21
{
    [Flags]
    public enum Z21CentralStatus : byte
    {
        None = 0x00,
        EmergencyStop = 0x01,           // Der Nothalt ist eingeschaltet
        TrackVoltageOff = 0x02,         // Die Gleisspannung ist abgeschaltet
        ShortCircuit = 0x04,            // Kurzschluss
        ProgrammingModeActive = 0x20    // Der Programmiermodus ist aktiv
    }

    public class Z21StatusInterpreter
    {
        public Z21CentralStatus Status { get; private set; }

        public Z21StatusInterpreter(byte state)
        {
            Status = (Z21CentralStatus)state;
        }

        public bool IsEmergencyStopActive => Status.HasFlag(Z21CentralStatus.EmergencyStop);
        public bool IsTrackVoltageOff => Status.HasFlag(Z21CentralStatus.TrackVoltageOff);
        public bool IsShortCircuit => Status.HasFlag(Z21CentralStatus.ShortCircuit);
        public bool IsProgrammingModeActive => Status.HasFlag(Z21CentralStatus.ProgrammingModeActive);

        public override string ToString()
        {
            List<string> messages = new();

            if (IsEmergencyStopActive)
                messages.Add("Nothalt aktiv");
            if (IsTrackVoltageOff)
                messages.Add("Gleisspannung abgeschaltet");
            if (IsShortCircuit)
                messages.Add("Kurzschluss");
            if (IsProgrammingModeActive)
                messages.Add("Programmiermodus aktiv");
            if (messages.Count == 0)
                messages.Add("Keine besonderen Zustände");

            return string.Join(", ", messages);
        }
    }

}

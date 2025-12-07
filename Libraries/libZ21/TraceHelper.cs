// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace libZ21
{
    internal static class TraceHelper
    {
        public static bool Ready { get; private set; }

        public static void Set_Gleistatus(int status, int grund)
        {
            if (status == 0x00)
            {
                Trace.WriteLine("Strecke: In Betrieb");
                //TrackStatus.BackColor = Color.ForestGreen;
                //TrackStatus.ForeColor = Color.White;
                Ready = true;
            }
            if ((status & 0x02) == 0x02)
            {
                Trace.WriteLine("Strecke: Kein Strom");
                //TrackStatus.BackColor = Color.Gold;
                //TrackStatus.ForeColor = Color.Black;
                Ready = false;
            }
            if ((status & 0x01) == 0x01)
            {
                Trace.WriteLine("Strecke: Nothalt");
                //TrackStatus.BackColor = Color.Orange;
                //TrackStatus.ForeColor = Color.Black;
                Ready = false;
            }
            if ((status & 0x04) == 0x04)
            {
                Trace.WriteLine("Strecke: Kurzschluss");
                //TrackStatus.BackColor = Color.Red;
                //TrackStatus.ForeColor = Color.White;
                Ready = false;
            }
            if ((status & 0x20) == 0x20)
            {
                Trace.WriteLine("Programmiermodus");
                //TrackStatus.BackColor = Color.Blue;
                //TrackStatus.ForeColor = Color.White;
                Ready = false;
            }
        }
    }
}

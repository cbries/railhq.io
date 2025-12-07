// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Runtime.InteropServices;
using libUtilities;

namespace railyGateway
{
	partial class Program
	{
        internal static bool IsClosing;

        #region Win32 

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(SetConsoleCtrlEventHandler handler, bool add);
        private delegate bool SetConsoleCtrlEventHandler(CtrlTypes sig);
        private enum CtrlTypes
        {
            CtrlCEvent = 0,
            CtrlBreakEvent,
            CtrlCloseEvent,
            CtrlLogoffEvent = 5,
            CtrlShutdownEvent
        }

		#endregion

		private static bool ConsoleCtrlCheck(CtrlTypes ctrlType)
        {
            switch (ctrlType)
            {
                case CtrlTypes.CtrlCEvent:
                    Logging.Log.Debug($"CTRL+C received");
                    IsClosing = true;
                    break;

                case CtrlTypes.CtrlBreakEvent:
                    Logging.Log.Debug($"CTRL+BREAK received!");
                    IsClosing = true;
                    break;

                case CtrlTypes.CtrlCloseEvent:
                    Logging.Log.Debug($"Program being closed!");
                    IsClosing = true;
                    break;

                case CtrlTypes.CtrlLogoffEvent:
                case CtrlTypes.CtrlShutdownEvent:
                    Logging.Log.Debug($"User is logging off!");
                    IsClosing = true;
                    break;
            }

            return true;
        }
	}
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libShared.Entities;

namespace railyWebApp.EntityLogger
{
    public class LocomotiveUpdate
    {
        private static void HandleSpeed(libUserspace.Workspace userWorkspace, ILocomotive locomotive, IDataExchange dataExchange)
        {
            //
            // Note: wenn man in diesen Handler läuft, dann hat die entsprechende Lokomotive
            // schon einen neuen Endzustand, d.h. wenn der Anwender eine Lokomotive stoppt, dann 
            // ist der Wert hier "0", analog dazu, wenn der Anwender die Lokomotive losfahren lässt.
            //
            // Entsprechend muss man die Protokollierung eines Stop auch nur dann durchführen, wenn
            // die aktuelle Geschwindigkeit "0" ist. Auch eine Startprotokollierung ist nur dann
            // erforderlich, wenn die Geschwindigkeit größer "0" ist. 
            //

            var uid = userWorkspace.Uid;
            var locSelector = locomotive.DriverName + "_" + locomotive.ObjectId;

            var statusMessage = string.Empty;
            bool? r = false;

            //
            // Nur bei "0" oder weniger kann überhaupt ein "Stop" passiert sein.
            //
            if (locomotive.Speedstep <= 0)
            {
                r = userWorkspace.LocomotiveLogging?.StopTrainRide($"{locSelector}", out statusMessage);
            }
            if (r.HasValue && r.Value)
            {
                dataExchange.QueueDebugMessage(uid, $"{statusMessage}", DebugMessageT.Locomotives);
            }
            else
            {
                //
                // Nur wenn die Geschwindigkeit größer "0" ist, dann kann ein Start passiert sein.
                //
                if (locomotive.Speedstep > 0)
                {
                    r = userWorkspace.LocomotiveLogging?.StartTrainRide($"{locSelector}", out statusMessage);
                }

                if (r.HasValue && r.Value)
                {
                    dataExchange.QueueDebugMessage(uid, $"{statusMessage}", DebugMessageT.Locomotives);
                }
            }
        }

        private static void HandleMetadata(libUserspace.Workspace userWorkspace, ILocomotive locomotive, IDataExchange _)
        {
            userWorkspace.LocomotiveMetadata?.UpsertLocomotive(locomotive);
        }

        public static void LocomotiveUpdateHandler(string uid, ILocomotive locomotive, IDataExchange dataExchange)
        {
            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres) return;
            if (userWorkspace == null) return;

            HandleSpeed(userWorkspace, locomotive, dataExchange);
            HandleMetadata(userWorkspace, locomotive, dataExchange);
        }
    }
}

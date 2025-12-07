// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libShared;
using libShared.Entities;

namespace railyWebApp.EntityLogger
{
    public class AccessoryUpdate
    {
        public static void LocomotiveUpdateHandler(string uid, IAccessory locomotive, IDataExchange dataExchange)
        {
            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres) return;
            if (userWorkspace == null) return;

            // TODO 
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace libInterop
{
    public interface IRailyHostCallbacks
    {
        void MessageReceived(IRailyExtension sender, string jsonMessage);
        void ErrorRaised(IRailyExtension sender, string jsonErrorMessage);

        bool IsConnectionToControllerEstablished();
    }
}

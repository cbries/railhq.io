// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Threading.Tasks;
using railyGateway.Cfg;

namespace railyGateway
{
    internal interface IWebSocketClient
    {
        ICfgService CfgService { get; }

        Task ConnectAsync(TimeSpan timeout);
        void Connect();
        void Close();
        void SendMessage(string message);
    }
}

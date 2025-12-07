// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyGateway.Cfg
{
    public interface IConnection
    {
        bool IsEnabled { get; set; }
        int TimeoutSeconds { get; set; }
        string Host { get; set; }
        int Port { get; set; }
        string Username { get; set; }
        string Password { get; set; }
    }
}

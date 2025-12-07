// Copyright (c) 2021 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// File: IConnector.cs

using System;

namespace railyEsuEcos.Network
{
    public interface IConnector
    {
        string IpAddress { get; set; }
        UInt16 Port { get; set; }

        bool Start();
        bool Stop();

        bool IsConnected();
    }
}

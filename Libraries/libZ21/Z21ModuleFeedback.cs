// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace libZ21;

public class Z21ModuleFeedback : libShared.Entities.Impl.S88Entity
{
    private const byte MaxPins = 8;

    public Z21ModuleFeedback()
    {
        MaxPorts = -1;
        Port = -1;
        Pins = MaxPins;
        HexState = "00";
        BinaryState = "00000000";
        DriverName = Globals.Z21Identifier;

    }

    private byte _lastByte = 0;

    public bool ParseData(byte statusByte)
    {
        // Prüfen, ob sich der Zustand geändert hat
        if (statusByte == _lastByte)
            return false;

        _lastByte = statusByte;

        // Bitfolge berechnen (Pin 1 ganz rechts)
        var binary = Convert.ToString(statusByte, 2).PadLeft(MaxPins, '0');
        BinaryState = new string(binary.ToArray());
        HexState = statusByte.ToString("X2");

        return true;
    }
}
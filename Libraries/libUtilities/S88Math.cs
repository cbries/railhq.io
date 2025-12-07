// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace libUtilities
{
    public class InOut
    {
        public int InputPin { get; set; }
        public int OutputPort { get; set; }
        public int OutputPin { get; set; }
    }

    public class S88Math
    {
        public static InOut HandleInput(int address)
        {
            var myPortIdx = (address - 1) / 16 + 1;
            var myPortPin = (address - 1) % 16 + 1;

            return new InOut
            {
                InputPin = address,
                OutputPort = myPortIdx,
                OutputPin = myPortPin
            };
        }
    }
}

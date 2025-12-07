// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railySystray
{
    internal class Program
    {
        private static SystrayInstance _systrayInstance;
        
        static void Main(string[] args)
        {
            _systrayInstance = new SystrayInstance();
            _systrayInstance.LoadAndRun();
        }
    }
}

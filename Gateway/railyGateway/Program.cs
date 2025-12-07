// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Threading.Tasks;
using libUtilities;

namespace railyGateway
{
    partial class Program
    {
        private static void LoadExtensions()
        {
            var r = Services.FindAndLoad();
            if (!r) return;

            var no = Services.Count;
            Logging.Log.Info($"Found {Services.AvailableServices.Count} extension{(no > 1 ? "s" : string.Empty)}.");
            Logging.Log.Info($"Extensions: {string.Join(", ", Services.GetNames())}");
        }
        
        [STAThread]
        static async Task Main(string[] args)
        {
            LoadExtensions();

            var app = new AppRunner();
            await app.RunAsync(args);
        }
    }
}

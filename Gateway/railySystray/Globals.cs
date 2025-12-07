// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace railySystray
{
    public class Globals
    {
        public static string AppName = "railhq.io - Systray";

        private static bool UseTls =>
            Environment.GetEnvironmentVariable("RAILHQ_USE_TLS")?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? true;

        public static string HttpProtocol => UseTls ? "https" : "http";
        public static string WsProtocol => UseTls ? "wss" : "ws";
    }
}

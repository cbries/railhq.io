// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace railyGateway
{
    public static class Globals
    {
        public static string _getBaseDir => libShared.RailEnvironment.GetBaseDirectory();
#if RELEASE
        public static string HttpRootDir = Path.Combine(_getBaseDir, "wwwroot");
#else
        public static string HttpRootDir = Path.Combine(_getBaseDir, "..", "..", "..", "wwwroot");
#endif

        private static bool UseTls
        {
            get
            {
                var useTls = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
                if (string.IsNullOrEmpty(useTls))
                    return false; // Default: kein TLS für lokale Entwicklung
                return useTls.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }

        public static string HttpProtocol => UseTls ? "https" : "http";
        public static string WsProtocol => UseTls ? "wss" : "ws";
    }

    public class MessageFromServer
    {
        public string DriverName { get; set; }
        public int ObjectId { get; set; }
        public string Command { get; set; }
        public string Argument { get; set; }
        public object ArgumentValue { get; set; }
    }

    public class ErrorFromServer
    {
        public string Command { get; set; } = string.Empty;
        public ErrorFromServerInfo Info { get; set; } = new();
    }

    public class ErrorFromServerInfo
    {
        public int Code { get; set; } = -1;
        public string Message { get; set; } = string.Empty;
    }
}

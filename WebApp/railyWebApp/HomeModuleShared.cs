// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection.AuthenticatedEncryption.ConfigurationModel;

namespace railyWebApp
{
    public class HomeModuleShared
    {
        public static string DefaultFile = "index.html";
        public static string DefaultLocomotveFile = "locomotive.html";

        public static IReadOnlyList<string> FilesForSubstitution = new List<string>()
        {
            DefaultFile, "index.js", "index.min.js",
            DefaultLocomotveFile, "locomotive.js", "locomotive.min.js"
        };

        private static string RemoveReleaseBlock(string content)
        {
            return Regex.Replace(content, @"<!--RELEASE.*?RELEASE-->", "", RegexOptions.Singleline);
        }

        private static string RemoveDebugBlock(string content)
        {
            return Regex.Replace(content, @"<!--DEBUG.*?DEBUG-->", "", RegexOptions.Singleline);
        }

        public static async Task<string> DoSubstitutions(string pathToFile, AuthData authData)
        {
            var hostConfig = HostConfiguration.Current;
            
            // Determine WebSocket protocol based on TLS configuration
            var useTlsEnv = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
            var useTls = !string.IsNullOrEmpty(useTlsEnv)
                ? useTlsEnv.Equals("true", StringComparison.OrdinalIgnoreCase)
                : Globals.RuntimeConfiguration.Host.IsTls;
            var wsProtocol = useTls ? "wss" : "ws";
            var httpProtocol = useTls ? "https" : "http";
            
            var subst = new Dictionary<string, string> {
                { "{{Author}}", "riesolution - Dr. Christian Benjamin Ries" },
                { "{{ApplicationName}}", "railhq.io" },
                { "{{ApplicationDescription}}", "railhq.io - The first cloud-based model railway headquarter!" },
                { "{{GENERATE_NET_HOST}}", Globals.RuntimeConfiguration.Host.RemoteHost},
                { "\"##GENERATE_NET_PORT##\"", $"{Globals.RuntimeConfiguration.Host.ListenPort}"},
                { "(##GENERATE_NET_PORT##)", $"{Globals.RuntimeConfiguration.Host.ListenPort}"},
                { "##GENERATE_WS_PROTOCOL##", wsProtocol},
                { "##GENERATE_HTTP_PROTOCOL##", httpProtocol},
                { "{{CONFIG_INDEX_URL}}", hostConfig.WebsiteUrl?.TrimEnd('/') ?? $"{httpProtocol}://localhost" },
                { "{{CONFIG_WEBSITE_URL}}", hostConfig.WebsiteUrl ?? "https://railhq.io/" },
                { "{{CONFIG_WIKI_URL}}", hostConfig.WikiUrl ?? "https://railhq.io/" }
            };

            var cnt = await File.ReadAllTextAsync(pathToFile, Encoding.UTF8);

            foreach (var it in subst)
                cnt = cnt.Replace(it.Key, it.Value);

#if DEBUG
            // remove all lines between
            // <!--RELEASE
            //    and
            // RELEASE-->
            //
            // replace with string.Empty
            //   <!--DEBUG
            //   DEBUG-->

            cnt = cnt.Replace("<!--DEBUG", string.Empty).Replace("DEBUG-->", string.Empty);
            cnt = RemoveReleaseBlock(cnt);

#elif RELEASE
            // remove all lines between
            // <!--DEBUG
            //    and
            // DEBUG-->
            //
            // replace with string.Empty
            //   <!--RELEASE
            //   RELEASE-->

            cnt = cnt.Replace("<!--RELEASE", string.Empty).Replace("RELEASE-->", string.Empty);
            cnt = RemoveDebugBlock(cnt);

#endif
            return cnt;
        }

        private static string _resourcePath = string.Empty;

        //
        // virtual mapping of https://xyz.de/theme/ -> GetThemePath()/theme/
        //
        public static bool HasVirtualMap(string requestPath, out FileInfo result)
        {
            result = null;

            if (string.IsNullOrEmpty(_resourcePath))
                _resourcePath = libUtilities.Filesystem.GetResourcesPath();

            if (requestPath.StartsWith("theme/"))
            {
                var fullPath = Path.Combine(_resourcePath, requestPath);
                result = new FileInfo(fullPath);
                return true;
            }

            if (requestPath.StartsWith("images/locomotives/"))
            {
                var fullPath = Path.Combine(_resourcePath, requestPath);
                result = new FileInfo(fullPath);
                return true;
            }

            return false;
        }
    }
}

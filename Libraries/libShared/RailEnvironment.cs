// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace libShared
{
    public class RailEnvironment
    {
        public static string RailHqCfgName = "railhqGateway.json";

        public static string GetHomeDir()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Environment.ExpandEnvironmentVariables("%USERPROFILE%");
            }

            return Environment.GetEnvironmentVariable("HOME");
        }

        public static string GetConfigDirPath()
        {
            var homeDir = GetHomeDir();
            var cfgPath = Path.Combine(homeDir, RailHqCfgName);
            if (File.Exists(cfgPath)) return cfgPath;

            var dirOfCfg = GetAppDirPath();
            return Path.Combine(dirOfCfg, RailHqCfgName);
        }

        public static string GetAppDirPath()
        {
            try
            {
                var asmLocation = Assembly.GetExecutingAssembly().Location;
                if (!string.IsNullOrEmpty(asmLocation))
                    return Path.GetDirectoryName(asmLocation);

                return AppDomain.CurrentDomain.BaseDirectory;

            }
            catch
            {
                // ignore
            }

            return AppDomain.CurrentDomain.BaseDirectory;
        }

        public static string GetBaseDirectory()
        {
            var baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            return baseDir ?? Directory.GetCurrentDirectory();
        }

        public static bool IsRunningInContainer()
        {
            var v = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER");
            if (v == "1") return true;
            if (v == "true") return true;

            try
            {
                const string checkPath = "/proc/1/cgroup";
                if (!File.Exists(checkPath)) return false;
                var cgroupContent = File.ReadAllText(checkPath);
                return cgroupContent.Contains("docker") || cgroupContent.Contains("kubepods");
            }
            catch
            {
                return false;
            }
        }

        public static bool IsContainerBuild()
        {
            return string.Equals(
                Environment.GetEnvironmentVariable("CONTAINER_BUILD"),
                "true",
                StringComparison.OrdinalIgnoreCase
            );
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Text.RegularExpressions;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Controller
{
    public class QueryWindowsFiles
    {
        public static readonly string SetupBaseNameWindows = "railhqGatewaySetup";
        public static readonly string SetupSearchPatternWindows = @"railhqGatewaySetup-(\d+\.\d+)\.exe";

        public static string FindLatestSetup(ILogger<DownloadController> logger, string folder, string pattern, out string version)
        {
            version = string.Empty;

            if (!Directory.Exists(folder)) return null;

            var regex = new Regex(pattern);
            string latestFile = null;
            Version latestVersion = null;

            var files = Directory.GetFiles(folder, SetupBaseNameWindows + "-*.exe");

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var match = regex.Match(fileName);

                if (match.Success)
                {
                    try
                    {
                        var fileVersion = Version.Parse(match.Groups[1].Value);

                        if (latestVersion == null || fileVersion.CompareTo(latestVersion) > 0)
                        {
                            latestVersion = fileVersion;
                            latestFile = file;

                            version = latestVersion.ToString(2);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning($"Fehler beim Verarbeiten der Datei {fileName}: {ex.Message}");
                    }
                }
            }

            return latestFile;
        }

        /// <summary>
        /// This is only used by Windows setup files, when nothing else is defined in the download link.
        /// </summary>
        /// <param name="downloadDirectory"></param>
        /// <param name="version"></param>
        /// <param name="logger"></param>
        /// <returns></returns>
        public static string GetLatestSetup(ILogger<DownloadController> logger, string downloadDirectory, out string version)
        {
            version = string.Empty;

            if (string.IsNullOrEmpty(downloadDirectory)) return string.Empty;
            if (!Directory.Exists(downloadDirectory)) return string.Empty;
            try
            {
                var lastFile = FindLatestSetup(logger, downloadDirectory, SetupSearchPatternWindows, out version);

                return lastFile;
            }
            catch
            {
                // ignore
            }

            return string.Empty;
        }

    }

    [ApiController]
    [Route("api/download/setup")]
    public class DownloadController : ControllerBase
    {
        private readonly string Latest = "latest";
        private readonly string _setupNamePatternWindows = "railhqGatewaySetup-{0}.exe";
        private readonly string _setupNamePatternLinux = "railhqgateway_{0}_amd64.deb";
        private readonly string _setupNamePatternRpiX64 = "railhqgateway_{0}_arm64.deb";
        private readonly string _setupNamePatternRpiX86 = "railhqgateway_{0}_armhf.deb";

        private string DownloadFolder => GetDownloadDirectory().FullName;

        private readonly ILogger<DownloadController> _logger;

        public DownloadController(ILogger<DownloadController> logger)
        {
            _logger = logger;
        }

        private DirectoryInfo GetDownloadDirectory()
        {
            if (RailEnvironment.IsRunningInContainer())
            {
                var p = Path.Combine("/", "app", "resourcesSetups");

                return new DirectoryInfo(p);
            }

            var resDir = libUtilities.Filesystem.GetResourcesSetupsPath();
            return new DirectoryInfo(resDir);
        }

        [HttpGet("{version}/{os}")]
        public IActionResult DownloadSetup(string version, string os)
        {
            string filePath;
            
            //
            // in case "latest" file is selected, we will provide a Windows setup always
            //
            if (string.IsNullOrEmpty(version)) version = Latest;
            if (version.Equals(Latest, StringComparison.OrdinalIgnoreCase))
            {
                filePath = QueryWindowsFiles.GetLatestSetup(_logger, DownloadFolder, out var latestVersion);
                if (!string.IsNullOrEmpty(filePath))
                    version = latestVersion;
            }
            else
            {
                if (string.IsNullOrEmpty(os)) os = "Windows";

                var pattern = _setupNamePatternWindows;

                switch (os.ToLower())
                {
                    case "windows": pattern = _setupNamePatternWindows; break;
                    case "linux": pattern = _setupNamePatternLinux; break;
                    case "rpix64": pattern = _setupNamePatternRpiX64; break;
                    case "rpix86": pattern = _setupNamePatternRpiX86; break;
                }

                filePath = Path.Combine(DownloadFolder, string.Format(pattern, version));
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning($"Downloadversuch für ungültige Version: {version}");

                return NotFound(new { message = "Datei nicht gefunden." });
            }

            if(filePath.EndsWith(".deb", StringComparison.OrdinalIgnoreCase))
                return PhysicalFile(filePath, "application/vnd.debian.binary-package", Path.GetFileName(filePath));

            return PhysicalFile(filePath, "application/octet-stream", Path.GetFileName(filePath));
        }
    }
}

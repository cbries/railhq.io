// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

namespace FileInfoHelper
{
    internal class Program
    {
        public class FileInfoHelper
        {
            public static string GetFileInfoJson(string filePath)
            {
                // Dateigröße ermitteln
                var fileInfo = new FileInfo(filePath);
                var fileSize = (fileInfo.Length / (1024 * 1024.0)).ToString("F3") + " MB";

                // Hashwert berechnen (SHA256)
                var hash = GetFileHash(filePath);

                // Versionsnummer ermitteln
                var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
                var version = versionInfo.FileVersion?.Trim();

                // Betriebssystem
                var os = GetOperatingSystem()?.Trim();

                // JSON formatieren
                var jsonObject = new
                {
                    visible = true,
                    version = version,
                    hash = hash,
                    size = fileSize,
                    os = new[] { os },
                    releaseNotes = new[]
                    {
                        "🐞 Fehlerbehebungen und Verbesserungen",
                        "🖥️ Unterstützung für neue Hardware",
                        "🚀 Optimierung der Performance"
                    }
                };

                var json = JsonSerializer.Serialize(jsonObject, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                return json;
            }

            private static string GetOperatingSystem()
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return "Windows";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "Linux";
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return "macOS";
                }
                else
                {
                    return "Unknown";
                }
            }

            private static string GetFileHash(string filePath)
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(filePath);
                var hashBytes = sha256.ComputeHash(stream);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }

            public static void Main(string[] args)
            {
                if (args.Length == 0)
                {
                    Console.WriteLine("Fehler: Kein Dateipfad angegeben.");
                    Console.WriteLine("Verwendung: FileInfoHelper <Dateipfad>");
                    Console.WriteLine("Beispiel: FileInfoHelper \"C:\\Pfad\\zu\\railhqGatewaySetup.exe\"");
                    return;
                }

                var filePath = args[0];
                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Fehler: Die Datei '{filePath}' wurde nicht gefunden.");
                    return;
                }

                var result = GetFileInfoJson(filePath);
                Console.WriteLine(result);
            }
        }
    }
}

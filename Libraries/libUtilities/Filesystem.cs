// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace libUtilities
{
    public static class Filesystem
    {
        public static string GetCurrentDllDirectory()
        {
            var dllPath = Assembly.GetExecutingAssembly().Location;
            var dllDirectory = Path.GetDirectoryName(dllPath);
            return dllDirectory;
        }

        public static string BaseDir => GetCurrentDllDirectory();

        public static string FindSolutionDirectory()
        {
            return FindSolutionDirectory(BaseDir);
        }

        public static string FindSolutionDirectory(string startDirectory)
        {
            var currentDirectory = new DirectoryInfo(startDirectory);

            while (currentDirectory != null)
            {
                // Prüfe, ob im aktuellen Verzeichnis eine .sln-Datei existiert
                if (currentDirectory.GetFiles("*.sln").Length > 0)
                {
                    return currentDirectory.FullName;
                }

                // Navigiere ein Verzeichnis nach oben
                currentDirectory = currentDirectory.Parent;
            }

            throw new FileNotFoundException("Solution file (*.sln) konnte nicht gefunden werden.");
        }

        private static string _resourceDirPath = string.Empty;
        private static string _resourceSetupDirPath = string.Empty;

        public static string GetResourcesPath()
        {
            if (RailEnvironment.IsRunningInContainer())
            {
                return Path.Combine("/", "app", "resources");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(GetCurrentDllDirectory(), "resources");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrEmpty(_resourceDirPath)) return _resourceDirPath;
                var baseDir = FindSolutionDirectory(BaseDir);
                _resourceDirPath = System.IO.Path.Combine(baseDir, "resources");
                return _resourceDirPath;
            }

            return string.Empty;
        }

        public static string GetResourcesSetupsPath()
        {
            if (RailEnvironment.IsRunningInContainer())
            {
                return Path.Combine("/", "app", "resourcesSetups");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                return Path.Combine(GetCurrentDllDirectory(), "resourcesSetups");
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!string.IsNullOrEmpty(_resourceSetupDirPath)) return _resourceSetupDirPath;
                var baseDir = FindSolutionDirectory(BaseDir);
                _resourceSetupDirPath = System.IO.Path.Combine(baseDir, "resourcesSetups");
                return _resourceSetupDirPath;
            }

            return string.Empty;
        }

        public static bool IsSubst(string url, IReadOnlyList<string> filesForSubstitution)
        {
            try
            {
                var fileInfo = new FileInfo(url);
                var fname = fileInfo.Name;
                if (filesForSubstitution.Contains(fname))
                    return true;
                return false;
            }
            catch
            {
                // ignore
            }

            return false;
        }

        public static async Task DeleteDirectoryAsync(string dirPath)
        {
            if (Directory.Exists(dirPath))
            {
                foreach (var file in Directory.GetFiles(dirPath))
                    await Task.Run(() => File.Delete(file));
                foreach (var subDir in Directory.GetDirectories(dirPath))
                    await DeleteDirectoryAsync(subDir);
                await Task.Run(() => Directory.Delete(dirPath, true));
            }
        }

        public static async Task RenameAsync(string oldPath, string newPath)
        {
            if (File.Exists(oldPath))
                await Task.Run(() => File.Move(oldPath, newPath));
            else if (Directory.Exists(oldPath))
                await Task.Run(() => Directory.Move(oldPath, newPath));
            else
            {
                throw new FileNotFoundException("Die angegebene Datei oder das Verzeichnis existiert nicht.");
            }
        }

        public static async Task CopyDirectoryAsync(string sourceDir, string destinationDir)
        {
            if (string.IsNullOrEmpty(destinationDir)) return;
            if (!Directory.Exists(destinationDir))
                Directory.CreateDirectory(destinationDir);

            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destinationDir, Path.GetFileName(file));
                await Task.Run(() => File.Copy(file, destFile, true));
            }

            foreach (var subDir in Directory.GetDirectories(sourceDir))
            {
                var destSubDir = Path.Combine(destinationDir, Path.GetFileName(subDir));
                await CopyDirectoryAsync(subDir, destSubDir);
            }
        }

        public static DirectoryInfo CreateDir(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;

            try
            {
                if (Directory.Exists(path)) return new DirectoryInfo(path);
                Directory.CreateDirectory(path);
                return new DirectoryInfo(path);
            }
            catch
            {
                // ignore
            }

            return null;
        }

        /// <summary>
        /// We check if a "container" path exist, 
        /// if not we use the file provided within the solution.
        /// </summary>
        public static string GetCertPath(string certName, int maxSeconds = 5)
        {
            var p1 = Path.Combine("/", "app", "certificates", certName);

            if (RailEnvironment.IsRunningInContainer())
            {
                var timeout = TimeSpan.FromSeconds(maxSeconds);
                var start = DateTime.UtcNow;

                while (!File.Exists(p1))
                {
                    if (DateTime.UtcNow - start > timeout)
                    {
                        Console.Error.WriteLine($"❌ Certificate {p1} not found after {timeout.TotalSeconds} seconds, using fallback.");
                        break;
                    }

                    Console.WriteLine($"⏳ Waiting for certificate {p1}...");

                    Thread.Sleep(1000); // 0,5 Sekunde warten, um CPU zu schonen
                }                
            }

            if (File.Exists(p1))
            {
                Console.Error.WriteLine($"✅ Certificate {p1} found.");
                return p1;
            }

            // fallbback path inside solution
            var slnDir = libUtilities.Filesystem.FindSolutionDirectory();
            return Path.Combine(slnDir, "resourcesRuntime", "certificates", certName);
        }
    }
}

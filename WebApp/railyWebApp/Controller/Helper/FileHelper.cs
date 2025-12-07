// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace railyWebApp.Controller.Helper
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    public class FileHelper
    {
        public static async Task CopyDirectoryAsync(string inputDir, string dirpath)
        {
            if (!Directory.Exists(inputDir))
                throw new DirectoryNotFoundException($"Quelle nicht gefunden: {inputDir}");

            Directory.CreateDirectory(dirpath); // Zielverzeichnis sicherstellen

            // Zuerst alle Unterordner erstellen (inkl. leerer Ordner)
            foreach (var dir in Directory.GetDirectories(inputDir, "*", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(inputDir, dir);
                string targetDir = Path.Combine(dirpath, relativePath);
                Directory.CreateDirectory(targetDir);
            }

            var sourceFiles = Directory.GetFiles(inputDir, "*", SearchOption.AllDirectories);

            await Parallel.ForEachAsync(sourceFiles, async (file, _) =>
            {
                string relativePath = Path.GetRelativePath(inputDir, file);
                string targetFile = Path.Combine(dirpath, relativePath);
                string targetDir = Path.GetDirectoryName(targetFile)!;

                Directory.CreateDirectory(targetDir); // Sicherstellen, dass der Zielordner existiert

                using FileStream sourceStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read);
                using FileStream destinationStream = File.Create(targetFile);
                await sourceStream.CopyToAsync(destinationStream);
            });
        }
    }

}

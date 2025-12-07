// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.IO;
using System.Linq;

namespace libUtilities
{
    public static class FilesystemHelper
    {
        private static readonly string[] AllowedExtensions =
        {
            // Dokumentformate
            ".json", ".txt", ".xml", ".csv",

            // Standard-Bildformate
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tif", ".tiff", ".webp", ".svg", ".ico", ".heic"
        };

        public static string RemoveKnownExtension(string fileName, out string ext)
        {
            ext = string.Empty;
            if (string.IsNullOrWhiteSpace(fileName)) return fileName;
            ext = Path.GetExtension(fileName);
            if (!string.IsNullOrEmpty(ext) &&
                AllowedExtensions.Contains(ext.ToLowerInvariant()))
            {
                return Path.GetFileNameWithoutExtension(fileName);
            }
            return fileName;
        }
    }
}

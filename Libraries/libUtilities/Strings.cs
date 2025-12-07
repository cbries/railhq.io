// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace libUtilities
{
    public static class StringExtensions
    {
        /// <summary>
        /// Replaces the specified file extension at the end of the string with a new extension.
        /// </summary>
        /// <param name="input">The original filename or path.</param>
        /// <param name="fromExt">The file extension to be replaced (e.g., ".avif").</param>
        /// <param name="toExt">The new file extension to append (e.g., ".png").</param>
        /// <returns>
        /// A new string with the replaced file extension if the original string ends with <paramref name="fromExt"/>.
        /// If the original string does not end with <paramref name="fromExt"/>, it returns the input unchanged.
        /// </returns>
        public static string ReplaceFileExtension(this string input, string fromExt, string toExt)
        {
            if (input.EndsWith(fromExt, StringComparison.OrdinalIgnoreCase))
            {
                return input[..^fromExt.Length] + toExt;
            }
            return input;
        }

        public static string CleanString(this string input)
        {
            if (string.IsNullOrEmpty(input)) return input;

            // 1. Unicode-Normalisierung anwenden und diakritische Zeichen entfernen
            var normalized = input.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder();
            foreach (char c in normalized)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            var cleaned = sb.ToString().Normalize(NormalizationForm.FormC);

            // 2. Unerwünschte Sonderzeichen entfernen (z.B. alles außer Buchstaben, Zahlen, Bindestrich, etc.)
            cleaned = Regex.Replace(cleaned, @"[^a-zA-Z0-9\-_. ]", "");
            // 3. Mehrfache Leerzeichen durch ein einziges ersetzen
            cleaned = Regex.Replace(cleaned, @"\s+", " ");
            // 4. (Optional) Leerzeichen durch Bindestriche ersetzen, falls nötig
            cleaned = cleaned.Replace(" ", "-");

            return cleaned;
        }

        /// <summary>
        /// Removes all line breaks (`\r` and `\n`) from a string and trims leading and trailing whitespace.
        /// </summary>
        /// <param name="msg">The input string.</param>
        /// <returns>A string without line breaks and trimmed whitespace.</returns>
        public static string Inline(this string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return string.Empty;

            // Optimized approach: Use StringBuilder for efficient string manipulation
            var sb = new System.Text.StringBuilder(msg.Length);
            foreach (var c in msg)
            {
                if (c != '\r' && c != '\n')
                    sb.Append(c);
            }

            return sb.ToString().Trim();
        }

        /// <summary>
        /// Replaces all backslashes (`\`) in a string with forward slashes (`/`).
        /// </summary>
        /// <param name="msg">The input string.</param>
        /// <returns>A string where all backslashes are replaced with forward slashes.</returns>
        public static string ToSlashes(this string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return string.Empty;

            // Direct optimization: Use Replace to transform the string efficiently
            return msg.Replace('\\', '/');
        }

        /// <summary>
        /// Prüft, ob eine Liste einen bestimmten Wert enthält, unter Berücksichtigung der Groß- und Kleinschreibung.
        /// </summary>
        /// <param name="list">Die Liste von Strings.</param>
        /// <param name="value">Der zu suchende Wert.</param>
        /// <returns>True, wenn der Wert enthalten ist, andernfalls False.</returns>
        public static bool ContainsExact(this List<string> list, string value)
        {
            if (list == null) throw new ArgumentNullException(nameof(list));
            if (value == null) throw new ArgumentNullException(nameof(value));

            return list.Contains(value, StringComparer.Ordinal);
        }

        /// <summary>
        /// You can get this or test it originally with: Encoding.UTF8.GetString(Encoding.UTF8.GetPreamble())[0];
        /// But no need, this way we have a constant. As these three bytes `[239, 187, 191]` (a BOM) evaluate to a single C# char.
        /// </summary>
        public const char BomChar = (char)65279;

        /// <summary>
        /// Removes the BOM character from the start of a string if present.
        /// </summary>
        /// <param name="str">The input string to check and possibly modify.</param>
        /// <returns>The string without a BOM character at the beginning, or the original string if no BOM was present.</returns>
        public static string FixBomIfNeeded(this string str)
        {
            if (string.IsNullOrEmpty(str) || str[0] != BomChar)
                return str;

            return str.Substring(1);
        }

        /// <summary>
        /// Writes the specified content to a file without adding a BOM.
        /// </summary>
        /// <param name="path">The file path where the content should be written.</param>
        /// <param name="content">The content to write to the file.</param>
        /// <param name="errorMessage">Outputs any error message if the operation fails.</param>
        /// <returns>True if the operation succeeds, otherwise false.</returns>
        public static bool WriteAllTextNoBom(string path, string content, out string errorMessage)
        {
            errorMessage = string.Empty;

            try
            {
                var utf8WithoutBom = new UTF8Encoding(false);
                File.WriteAllText(path, content.AsSpan().Trim().ToString(), utf8WithoutBom);
                return true;
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                return false;
            }
        }
    }
}

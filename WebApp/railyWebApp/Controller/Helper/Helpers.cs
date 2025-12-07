// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using SkiaSharp;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace railyWebApp.Controller.Helper
{
    public class Helpers
    {
        public const string MetaDescription = "Platzhalter von railhq.io";
        public const string MetaAutor = "railhq.io";
        public const string MetaKeywords = "Modelleisenbahn in der Cloud";
        public const string MetaSoftware = "railhq - Modelleisenbahn in der Cloud";
        public const string MetaTimestamp = "yyyy:MM:dd HH:mm:ss";

        //public const int DummyFileWidth = 250;
        //public const int DummyFileHeight = 80;

        public const int DummyFileWidth = 550;
        public const int DummyFileHeight = 160;

        public static string GetMimeType(string filePath)
        {
            var extension = Path.GetExtension(filePath).ToLower();
            return extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".bmp" => "image/bmp",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
        }

        public static SKData GetFallbackImage(List<string> extraText = null, int width = DummyFileWidth, int height = DummyFileHeight)
        {
            var lines0 = new List<string>
            {
                MetaAutor,
                MetaKeywords
            };
            if (extraText != null && extraText.Count > 0)
                lines0.AddRange(extraText);

            var lines = lines0.ToArray();

            using var bitmap = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bitmap);

            // Hintergrund mit Farbverlauf
            using (var backgroundPaint = new SKPaint
            {
                Shader = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0),
                    new SKPoint(0, height),
                    new[] { SKColors.White, new SKColor(211, 211, 211) },
                    null,
                    SKShaderTileMode.Clamp)
            })
            {
                canvas.DrawRect(new SKRect(0, 0, width, height), backgroundPaint);
            }

            // Rahmen mit abgerundeten Ecken
            using (var borderPaint = new SKPaint
            {
                Color = SKColors.Gray,
                StrokeWidth = 2,
                IsStroke = true,
                IsAntialias = true
            })
            {
                canvas.DrawRoundRect(new SKRoundRect(new SKRect(2, 2, width - 2, height - 2), 12, 12), borderPaint);
            }

            // Text-Setup
            using var textPaint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                TextSize = 24,
                Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyle.Bold)
            };

            using var shadowPaint = new SKPaint
            {
                Color = SKColors.Gray.WithAlpha(128),
                IsAntialias = textPaint.IsAntialias,
                TextSize = textPaint.TextSize,
                Typeface = textPaint.Typeface,
                TextAlign = textPaint.TextAlign,
                Style = textPaint.Style
            };

            // Vertikale Zentrierung berechnen
            var totalTextHeight = lines.Length * textPaint.TextSize;
            var y = (height - totalTextHeight) / 2f + textPaint.TextSize;

            // Textzeilen mit Schatten zeichnen
            foreach (var line in lines)
            {
                var textWidth = textPaint.MeasureText(line);
                var x = (width - textWidth) / 2f;

                canvas.DrawText(line, x + 1, y + 1, shadowPaint); // Schatten leicht versetzt
                canvas.DrawText(line, x, y, textPaint);           // Text

                y += textPaint.TextSize;
            }

            using var image = SKImage.FromBitmap(bitmap);
            return image.Encode(SKEncodedImageFormat.Png, 85);
        }
    }

    public static class FileNameHelper
    {
        public static string ToWebSafeFileName(string fileName, bool useLowerInvariant = true)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            // 1. Entferne Pfadangaben, falls vorhanden
            fileName = fileName.Trim();
            fileName = fileName.Replace("\\", "/");
            fileName = fileName.Substring(fileName.LastIndexOf("/") + 1);

            // 2. Entferne diakritische Zeichen (Umlaute, Akzente)
            fileName = RemoveDiacritics(fileName);

            // 3. Ersetze Leerzeichen und nicht erlaubte Zeichen durch "-"
            fileName = Regex.Replace(fileName, @"[^a-zA-Z0-9\-_\.]", "-");

            // 4. Mehrere "-" auf ein einzelnes reduzieren
            fileName = Regex.Replace(fileName, @"-+", "-");

            // 5. Kleinbuchstaben für bessere Kompatibilität
            if (useLowerInvariant)
                return fileName.ToLowerInvariant();

            return fileName;
        }

        private static string RemoveDiacritics(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}

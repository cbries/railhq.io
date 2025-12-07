// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Text;
using NUglify;
using NUglify.Css;
using NUglify.JavaScript;

namespace Minifier
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Fehlende Argumente! Nutzung: Minifier <Pfad1> <Pfad2> ...");
                return;
            }

            if (args[0].Equals("-single", StringComparison.OrdinalIgnoreCase))
            {
                var path = args[1];
                var baseName = Path.GetFileNameWithoutExtension(path);
                var newFileName =
                    Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, baseName + ".min" + Path.GetExtension(path));
                if (path.EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                {
                    var content = File.ReadAllText(path, Encoding.UTF8);
                    var minified = Uglify.Js(content, new CodeSettings
                    {
                        OutputMode = OutputMode.SingleLine,
                        PreserveImportantComments = false,
                        TermSemicolons = true,
                        MinifyCode = true,
                        LocalRenaming = LocalRenaming.CrunchAll,
                        StripDebugStatements = true
                    });
                    var js = string.Empty;
                    if (!minified.HasErrors) js = minified.Code;
                    else ShowError(minified.Errors);

                    if (!string.IsNullOrWhiteSpace(js))
                        File.WriteAllText(newFileName, js, Encoding.UTF8);
                }
                else if (path.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
                {
                    string content = File.ReadAllText(path, Encoding.UTF8);
                    var minified = Uglify.Css(content, new CssSettings
                    {
                        OutputMode = OutputMode.SingleLine,  // Alles in eine Zeile
                        RemoveEmptyBlocks = true,            // Alle Kommentare entfernen
                        TermSemicolons = true,               // Fehlende Semikolons hinzufügen
                        MinifyExpressions = true             // Verkürzt CSS-Ausdrücke
                    });
                    var css = string.Empty;
                    if (!minified.HasErrors) css = minified.Code + "\n";
                    else ShowError(minified.Errors);

                    if (!string.IsNullOrWhiteSpace(css))
                        File.WriteAllText(newFileName, css, Encoding.UTF8);
                }

                Console.WriteLine($"Minifizierung: {path} -> {newFileName}");
                return;
            }

            foreach (var path in args)
            {
                if (!Directory.Exists(path))
                {
                    Console.WriteLine($"Uebersprungen (kein Verzeichnis): {path}");
                    continue;
                }

                DeleteMinifiedFiles(path);
                MinifyAndCombineFiles(path);
            }

            Console.WriteLine("Minifizierung abgeschlossen!");
        }

        private static void DeleteMinifiedFiles(string directory)
        {
            foreach (var file in Directory.GetFiles(directory, "*.min.*", SearchOption.AllDirectories))
            {
                try
                {
                    File.Delete(file);
                    Console.WriteLine($"Geloescht: {file}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Fehler beim Löschen {file}: {ex.Message}");
                }
            }
        }

        private static void ShowError(List<UglifyError> errors)
        {
            foreach (var it in errors)
            {
                Console.WriteLine($" [x] {it}");
            }
        }

        private static void MinifyAndCombineFiles(string directory)
        {
            var jsFiles = Directory.GetFiles(directory, "*.js", SearchOption.AllDirectories).Where(f => !f.Contains(".min."));
            var cssFiles = Directory.GetFiles(directory, "*.css", SearchOption.AllDirectories).Where(f => !f.Contains(".min."));

            string combinedJs = "", combinedCss = "";

            foreach (var file in jsFiles)
            {
                Console.WriteLine($"JS: {file}");

                string content = File.ReadAllText(file, Encoding.UTF8);
                var minified = Uglify.Js(content, new CodeSettings
                {
                    OutputMode = OutputMode.SingleLine,
                    PreserveImportantComments = false,
                    TermSemicolons = true,
                    MinifyCode = true,
                    LocalRenaming = LocalRenaming.CrunchAll,
                    StripDebugStatements = true
                });
                if (!minified.HasErrors) combinedJs += minified.Code + "\n";
                else ShowError(minified.Errors);
            }

            foreach (var file in cssFiles)
            {
                Console.WriteLine($"CSS: {file}");

                string content = File.ReadAllText(file, Encoding.UTF8);
                var minified = Uglify.Css(content, new CssSettings
                {
                    OutputMode = OutputMode.SingleLine,  // Alles in eine Zeile
                    RemoveEmptyBlocks = true,            // Alle Kommentare entfernen
                    TermSemicolons = true,               // Fehlende Semikolons hinzufügen
                    MinifyExpressions = true             // Verkürzt CSS-Ausdrücke
                });
                if (!minified.HasErrors) combinedCss += minified.Code + "\n";
                else ShowError(minified.Errors);
            }

            if (!string.IsNullOrWhiteSpace(combinedJs))
            {
                string outputJs = Path.Combine(directory, "app.min.js");
                File.WriteAllText(outputJs, combinedJs, Encoding.UTF8);
                Console.WriteLine($"JS kombiniert: {outputJs}");
            }

            if (!string.IsNullOrWhiteSpace(combinedCss))
            {
                string outputCss = Path.Combine(directory, "styles.min.css");
                File.WriteAllText(outputCss, combinedCss, Encoding.UTF8);
                Console.WriteLine($"CSS kombiniert: {outputCss}");
            }
        }
    }

}

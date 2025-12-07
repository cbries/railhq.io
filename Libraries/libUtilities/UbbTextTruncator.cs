// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace libUtilities
{
    public static class UbbTextTruncator
    {
        public static (string ShortHtml, string FullHtml, bool IsShortened) TruncateAndFormat(string ubbText, int wordLimit)
        {
            string fullHtml = FormatUbbToHtml(ubbText);

            var words = Regex.Split(ubbText, @"\s+");
            if (words.Length <= wordLimit)
            {
                return (fullHtml, fullHtml, false);
            }

            var truncatedWords = string.Join(" ", words[..wordLimit]);
            var remainingWords = string.Join(" ", words[wordLimit..]);

            // Optional: Satzende erkennen und anhängen
            var match = Regex.Match(remainingWords, @".*?[.!?]");
            if (match.Success)
            {
                truncatedWords += " " + match.Value;
            }

            // UBB-Tags korrekt schließen
            string closedTruncatedUbb = CloseUbbTags(truncatedWords);
            string shortHtml = FormatUbbToHtml(closedTruncatedUbb);

            return (shortHtml, fullHtml, true);
        }

        private static string CloseUbbTags(string text)
        {
            var openTags = new Stack<string>();
            var openTagRegex = new Regex(@"\[([a-z]+)(=[^\]]+)?\]", RegexOptions.IgnoreCase);
            var closeTagRegex = new Regex(@"\[/([a-z]+)\]", RegexOptions.IgnoreCase);

            foreach (Match match in openTagRegex.Matches(text))
            {
                openTags.Push(match.Groups[1].Value.ToLower());
            }

            foreach (Match match in closeTagRegex.Matches(text))
            {
                string closing = match.Groups[1].Value.ToLower();
                if (openTags.Contains(closing))
                {
                    // Entferne das passende öffnende Tag
                    var tempStack = new Stack<string>();
                    while (openTags.Peek() != closing)
                        tempStack.Push(openTags.Pop());
                    openTags.Pop();
                    while (tempStack.Count > 0)
                        openTags.Push(tempStack.Pop());
                }
            }

            // Jetzt alle übrig gebliebenen offenen Tags schließen
            while (openTags.Count > 0)
            {
                var tag = openTags.Pop();
                text += $"[/{tag}]";
            }

            return text;
        }

        private static string FormatUbbToHtml(string ubb)
        {
            // Einfaches UBB → HTML (ggf. anpassen oder erweitern)
            return ubb
                .Replace("[b]", "<strong>").Replace("[/b]", "</strong>")
                .Replace("[i]", "<em>").Replace("[/i]", "</em>")
                .Replace("[u]", "<u>").Replace("[/u]", "</u>")
                .Replace("[br]", "<br/>")
                .Replace("\n", "<br/>");
        }
    }
}

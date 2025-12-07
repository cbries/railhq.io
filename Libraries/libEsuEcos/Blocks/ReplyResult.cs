// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿namespace libEsuEcos.Blocks
{
    public class ReplyResult
    {
        public int ErrorCode { get; private set; }
        public string ErrorMessage { get; private set; }

        public void Parse(string line)
        {
            ErrorCode = 0;
            ErrorMessage = "OK";

            line = line.Replace("<END ", string.Empty);
            line = line.Trim().TrimEnd('\r', '\n', '>');

            var n = line.IndexOf(' ');
            if (n == -1) return;

            var firstPart = line.Substring(0, n).Trim();
            if (int.TryParse(firstPart, out var v))
                ErrorCode = v;

            var lastPart = line.Substring(n + 1).Trim();
            lastPart = lastPart.TrimStart('(').TrimEnd(')');
            if (!string.IsNullOrEmpty(lastPart))
                ErrorMessage = lastPart;
        }
    }
}

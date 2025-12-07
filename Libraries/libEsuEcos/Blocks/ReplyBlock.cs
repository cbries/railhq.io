// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace libEsuEcos.Blocks
{
    public class ReplyBlock : IBlock
    {
        public string StartLine { get; private set; } = string.Empty;
        public string EndLine { get; private set; } = string.Empty;
        public ICommand Command { get; private set; }
        public string NativeBlock { get; set; } = string.Empty;
        public ReplyResult Result { get; private set; }
        public ListEntryCollection ListEntries { get; private set; } = [];
        
        public bool Parse(IReadOnlyList<string> lines)
        {
            return Parse(string.Join('\n', lines));
        }

        public bool Parse(string block)
        {
            if (string.IsNullOrEmpty(block)) return false;

            NativeBlock = block;

            if (block.IndexOf("<REPLY", StringComparison.OrdinalIgnoreCase) == -1)
                return false;
            if (block.IndexOf("<END", StringComparison.OrdinalIgnoreCase) == -1)
                return false;

            var lines = block.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            for (var i = 0; i < lines.Count; ++i)
            {
                if (string.IsNullOrEmpty(lines[i]))
                    continue;

                lines[i] = lines[i].Trim();
            }

            StartLine = lines[0].Trim();
            ParseStart();

            EndLine = lines[lines.Count - 1].Trim();
            ParseEnd();

            lines.RemoveAt(lines.Count - 1);
            if(lines.Count > 0)
                lines.RemoveAt(0);

            if (lines.Count < 0)
                return true;

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line))
                    continue;

                var entry = new ListEntry();
                if (entry.Parse(line))
                    ListEntries.Add(entry);
            }

            return true;
        }

        private void ParseStart()
        {
            try
            {
                if (string.IsNullOrEmpty(StartLine))
                    return;

                var s = StartLine;
                s = s.Replace("<REPLY ", "");
                s = s.Trim().TrimEnd('\r', '\n', '>');
                Command = CommandFactory.Create(s);
            }
            catch
            {
                // ignore
            }
        }

        private void ParseEnd()
        {
            try
            {
                if (string.IsNullOrEmpty(StartLine))
                    return;

                Result = new ReplyResult();
                Result.Parse(EndLine);
            }
            catch
            {
                // ignore
            }
        }

        public ListEntryCollection GetListEntriesOf(string argname)
        {
            if (string.IsNullOrEmpty(argname)) return ListEntries;

            var res = new ListEntryCollection();

            foreach (var it in ListEntries)
            {
                if(it == null) continue;

                var args = it.Arguments;
                foreach (var itt in args)
                {
                    if (itt.Name.Equals(argname))
                        res.Add(it);
                }
            }

            return res;
        }
    }
}

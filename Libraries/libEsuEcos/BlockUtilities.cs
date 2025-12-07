// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using libEsuEcos.Blocks;

namespace libEsuEcos
{
    public static class BlockUtilities
    {
        public const char CR = '\r';
        public const char LF = '\n';
        public const string CRLF = "\r\n";

        public static List<IBlock> ExtractBlocks(IList<string> lines)
        {
            List<IBlock> blocks = new();
            List<string> currentBlock = new();
            List<string> toRemove = new();

            var insideBlock = false;
            var isEvent = false;
            var isReply = false;

            foreach (var line in lines)
            {
                if (string.IsNullOrEmpty(line)) continue;
                var l = line.Trim();

                if (!insideBlock)
                {
                    if (l.StartsWith("<EVENT", StringComparison.OrdinalIgnoreCase)
                        || l.StartsWith("<REPLY", StringComparison.OrdinalIgnoreCase))
                    {
                        isEvent = l.StartsWith("<EVENT", StringComparison.OrdinalIgnoreCase);
                        isReply = l.StartsWith("<REPLY", StringComparison.OrdinalIgnoreCase);

                        insideBlock = true;
                    }
                    else
                    {
                        continue;
                    }
                }

                if (insideBlock)
                {
                    currentBlock.Add(l);
                    
                }

                if (l.StartsWith("<END"))
                {
                    IBlock instance = null;
                    if (isEvent)
                        instance = new EventBlock();
                    else if (isReply)
                        instance = new ReplyBlock();

                    if (instance != null)
                    {
                        if (!instance.Parse(currentBlock))
                        {

                        }
                        else
                        {
                            toRemove.AddRange(currentBlock);

                            blocks.Add(instance);
                        }
                    }

                    currentBlock.Clear();

                    insideBlock = false;
                }
            }

            if (lines is List<string> list)
                toRemove.RemoveAll(it => list.Remove(it));

            return blocks;
        }

        public static List<IBlock> ExtractBlocks(ref string msg)
        {
            var lines = msg.Split(new[] { LF }, StringSplitOptions.RemoveEmptyEntries);
            var blocks = ExtractBlocks(lines);
            msg = string.Join(CRLF, lines);
            return blocks;
        }
    }
}

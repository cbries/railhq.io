// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using libEsuEcos.Blocks;

namespace libEsuEcos
{
    public interface IBlock
    {
        ICommand Command { get; }
        string NativeBlock { get; set; }
        string StartLine { get; }
        string EndLine { get; }
        ReplyResult Result { get; }
        ListEntryCollection ListEntries { get; }

        bool Parse(IReadOnlyList<string> lines);
        bool Parse(string block);

        ListEntryCollection GetListEntriesOf(string argname);
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using libEsuEcos;
using libInterop;

namespace railyEsuEcos.Commands
{
    public class Payload : IPayload
    {
        [JsonProperty("commandBlocks")] public List<string> CommandBlocks { get; } = new();

        public void AddCommands(IReadOnlyList<string> commands)
        {
            if (commands == null) return;
            if (commands.Count == 0) return;
            CommandBlocks.AddRange(commands);
        }

        public void AddBytes(byte[] bytes)
        {
            throw new System.NotImplementedException("Not supported for ECoS");
        }

        public void AddBlocks(IReadOnlyList<IBlock> blocks)
        {
            if (blocks == null) return;
            if (blocks.Count == 0) return;

            foreach (var blk in blocks)
            {
                if(string.IsNullOrEmpty(blk?.NativeBlock)) continue;

                CommandBlocks.Add(blk.NativeBlock);
            }
        }
    }
}

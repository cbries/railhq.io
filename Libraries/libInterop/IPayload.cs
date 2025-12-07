// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace libInterop
{
    public interface IPayload
    {
        List<string> CommandBlocks { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="commands"></param>
        void AddCommands(IReadOnlyList<string> commands);

        void AddBytes(byte[] bytes);
    }
}

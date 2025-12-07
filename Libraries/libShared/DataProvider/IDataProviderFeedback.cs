// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.Entities.Impl;
using System.Collections.Generic;

namespace libShared.DataProvider
{
    public interface IDataProviderFeedback
    {
        public Dictionary<int, S88Entity> Ports { get; }
    }
}

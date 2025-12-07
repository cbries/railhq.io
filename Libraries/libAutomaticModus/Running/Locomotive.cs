// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libShared.DataProvider;
using libShared.Entities;

namespace libAutomaticModus;

public class Locomotive
{
    public IDataProvider DataProvider { get; set; }
    public ILocomotive Entity { get; set; }
}
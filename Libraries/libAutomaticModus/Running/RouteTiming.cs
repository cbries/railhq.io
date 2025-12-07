// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;

namespace libAutomaticModus.Running;

public class RouteTiming
{
    public DateTime Started { get; set; } = DateTime.MinValue;
    public DateTime CruiseStart { get; set; } = DateTime.MinValue;
    public DateTime Enter { get; set; } = DateTime.MinValue;
    public DateTime In { get; set; } = DateTime.MinValue;
    public DateTime Ended { get; set; } = DateTime.MinValue;

}
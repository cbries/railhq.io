// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;

namespace libAutomaticModus;

public class Sensor
{
    public IDataProvider DataProvider { get; set; }
    public int Pin { get; set; }
    public string ModulePin { get; set; } = string.Empty;

    // is mostly used during automode to get
    // informed if the defined sensor is triggered
    public bool State { get; set; } = false;
}
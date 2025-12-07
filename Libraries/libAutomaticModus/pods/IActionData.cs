// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libAutomaticModus.pods
{
    public interface IActionData
    {
        string Command { get; set; }
        string Argument { get; set; }
    }
}

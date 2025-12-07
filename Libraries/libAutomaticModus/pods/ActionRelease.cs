// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.


namespace libAutomaticModus.pods
{
    public class ActionRelease : IActionData
    {
        public string Command { get; set; } = "release";
        public string Argument { get; set; } = "locomotive";

        public string DriverName { get; set; }
        public int ObjectId { get; set; }
    }
}

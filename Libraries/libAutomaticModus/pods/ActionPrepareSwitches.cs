// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libAutomaticModus.pods
{
    public class ActionPrepareSwitches : IActionData
    {
        public string Command { get; set; } = "prepareSwitches";
        public string Argument { get; set; } = "route";
        public string RouteName { get; set; }
    }

    public class ActionPrepareSignals : IActionData
    {
        public string Command { get; set; } = "prepareSignals";
        public string Argument { get; set; } = "sourceBlock";

        public string SourceBlock { get; set; }
        public string SourceLeavingSide { get; set; }
    }
}

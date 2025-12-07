// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libZ21.EntitiesPredefined
{
    public class PredefinedLocomotiveFunction
    {
        public string DriverName { get; set; } = string.Empty;
        public int Address { get; set; } = -1;
        public int FncIdx { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public string Icon { get; set; } = string.Empty;
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libZ21
{
    public class HardwareInfo(HardwareTyp hardware, FirmwareVersionInfo firmware)
    {
        public HardwareTyp Hardware = hardware;
        public FirmwareVersionInfo FirmwareVersion = firmware;

        public override string ToString()
        {
            return $"Hardware: {Hardware}, Firmware: {FirmwareVersion}";
        }
    }
}
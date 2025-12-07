// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libZ21
{
    public class FirmwareVersionInfo(int major, int minor)
    {
        public int Major = major;
        public int Minor = minor;

        public override string ToString()
        {
            return Major.ToString("X") + "." + Minor.ToString("X");
        }
    }
}
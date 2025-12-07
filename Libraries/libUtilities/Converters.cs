// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Linq;

namespace libUtilities
{
    public class Converters
    {
        /// <summary>
        /// Example:
        ///     ff => 11111111
        ///     f0 => 11110000
        /// </summary>
        /// <param name="hexValue"></param>
        /// <returns></returns>
        public static string ToBinary(string hexValue)
        {
            hexValue = hexValue.PadLeft(4, '0');

            return string.Join(string.Empty,
                hexValue.Select(
                    c => Convert.ToString(Convert.ToInt32(c.ToString(), 16), 2).PadLeft(4, '0')
                ));
        }

        /// <summary>
        /// Example:
        ///     11111111 => FF
        ///     00001111 => 0F
        /// </summary>
        /// <param name="binaryValue"></param>
        /// <returns></returns>
        public static string ToHex(string binaryValue)
        {
            var hex = string.Join(string.Empty,
                Enumerable.Range(0, binaryValue.Length / 8)
                    .Select(i => Convert.ToByte(binaryValue.Substring(i * 8, 8), 2).ToString("X2")));
            if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                hex = hex.Substring(2).Trim();
            return hex;
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using libInterop;

namespace libZ21
{
    public class Payload : IPayload
    {
        public List<string> CommandBlocks { get; } = new();

        public void AddCommands(IReadOnlyList<string> commands)
        {
            throw new NotImplementedException("Not supported for z21");
        }

        /// <summary>
        /// Wandelt `bytes` in einen String um.
        /// Beispiel:
        ///   `byte[] bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };`
        /// Ausgabe:
        ///   `DE-AD-BE-EF`
        /// </summary>
        /// <param name="bytes"></param>
        public void AddBytes(byte[] bytes)
        {
            var command = BitConverter.ToString(bytes);
            CommandBlocks.Add(command);
        }

        /// <summary>
        /// Wandelt alle gültigen Hex-Strings aus der Liste `CommandBlocks` in Byte-Arrays um.
        /// Leere oder ungültige Strings werden dabei übersprungen.
        /// </summary>
        /// <returns>Eine Liste von Byte-Arrays, die aus den Hex-Strings dekodiert wurden.</returns>
        public List<byte[]> GetEncodedCommands()
        {
            return CommandBlocks
                .Where(it => !string.IsNullOrWhiteSpace(it))
                .Select(HexStringToBytes)
                .ToList();
        }

        /// <summary>
        /// Wandelt einen Hex-String (z. B. "01-AB-FF") in ein Byte-Array um.
        /// Bindestriche zwischen den Bytes werden erwartet.
        /// </summary>
        /// <param name="hex">Ein String im Format "AA-BB-CC"</param>
        /// <returns>Das entsprechende Byte-Array</returns>
        /// <exception cref="ArgumentException">Wenn der String ungültige Hex-Zeichen enthält</exception>
        public static byte[] HexStringToBytes(string hex)
        {
            try
            {
                return hex
                    .Split('-', StringSplitOptions.RemoveEmptyEntries)
                    .Select(b => Convert.ToByte(b, 16))
                    .ToArray();
            }
            catch (FormatException ex)
            {
                // Optional: Logging oder Fehlerbehandlung
                throw new ArgumentException($"Ungültiges Hex-Format: '{hex}'", ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="receivedCommands"></param>
        /// <returns></returns>
        public static List<byte[]> ConvertToBytes(List<string> receivedCommands)
        {
            return receivedCommands
                .Where(it => !string.IsNullOrWhiteSpace(it))
                .Select(HexStringToBytes)
                .ToList();
        }
    }
}

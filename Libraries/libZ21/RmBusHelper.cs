// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace libZ21
{
    public class RmBusHelper
    {
        /// <summary>
        /// Gibt die absoluten Rückmelder-Adressen zurück, die in rmStatus als aktiv markiert sind.
        /// </summary>
        /// <param name="gruppenIndex">Index der Rückmeldergruppe (0 = Module 1-10, 1 = 11-20, ...)</param>
        /// <param name="rmStatus">10-Byte-Array mit je 1 Bit pro Eingang</param>
        /// <returns>Liste der absoluten Rückmelder-Adressen und deren Eingänge (1-8)</returns>
        public static List<(int module, int pin)> GetActiveFeedbacks(byte gruppenIndex, byte[] rmStatus)
        {
            if (rmStatus == null || rmStatus.Length != 10)
                throw new ArgumentException("rmStatus muss genau 10 Bytes lang sein.");

            var aktiveRueckmelder = new List<(int module, int pin)>();

            // Berechne die Basisadresse dieser Gruppe (z. B. gruppenIndex 0 → Adresse 1, gruppenIndex 1 → Adresse 11)
            var basisAdresse = gruppenIndex * 10 + 1;

            for (var i = 0; i < 10; i++) // 10 Module pro Gruppe
            {
                var statusByte = rmStatus[i];

                for (var bit = 0; bit < 8; bit++) // 8 Eingänge pro Modul
                {
                    if ((statusByte & (1 << bit)) != 0)
                    {
                        var adresse = basisAdresse + i;
                        var eingang = bit + 1; // Eingänge zählen von 1 bis 8
                        aktiveRueckmelder.Add((adresse, eingang));
                    }
                }
            }

            return aktiveRueckmelder;
        }
    }
}

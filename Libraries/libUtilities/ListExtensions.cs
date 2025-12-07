// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace libUtilities
{
    public static class ListExtensions
    {
        /// <summary>
        /// Retrieves a random item from the given list.
        /// </summary>
        /// <typeparam name="T">The type of elements in the list.</typeparam>
        /// <param name="liste">The list to select a random item from.</param>
        /// <returns>A randomly selected item from the list.</returns>
        /// <exception cref="ArgumentException">Thrown if the list is null or empty.</exception>
        public static T GetRandomItem<T>(this List<T> liste)
        {
            if (liste == null || liste.Count == 0)
                return default;

            var zufall = new Random();
            int index = zufall.Next(liste.Count);
            return liste[index];
        }
    }
}

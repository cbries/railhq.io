// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using System.Linq;

namespace libEsuEcos.Blocks
{
    public class ListEntryCollection : List<ListEntry>
    {
        public ListEntryCollection(IEnumerable<ListEntry> entries) 
            : base(entries)
        {
        }

        public ListEntryCollection()
        {
        }

        /// <summary>
        /// Special filter mechamism to avoid dublicate entity updates.
        /// E.g. `func` and `funcset` in the same list are not really needed.
        /// This method will remove one of these entries.
        /// In general the seem to provide the same information.
        /// </summary>
        /// <returns>A new ListEntryCollection with filtered entries.</returns>
        public ListEntryCollection GetSingletonEntries()
        {
            var list = new ListEntryCollection(this);

            // keep `func`
            // remove `funcset`
            var containsFunc = list.Any(entry => entry.Arguments[0].Name == "func");
            if (containsFunc) list.RemoveAll(entry => entry.Arguments[0].Name == "funcset");

            return list;
        }
    }
}

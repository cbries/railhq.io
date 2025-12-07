// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libEsuEcos.Entities;
using libShared.DataProvider;
using libShared.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;

namespace railyWebApp.DataProvider
{
    public static class DataProviderExtension
    {
        public static bool IsEcosInGo(this IReadOnlyList<IDataProvider> dps, out JObject statusMessage)
        {
            statusMessage = null;

            if (dps == null) return false;
            if (dps.Count == 0) return false;

            var clientsDps = dps as ClientDataProviders;
            if (clientsDps == null) return false;

            var dpEcos = clientsDps.Get(DataProviderType.ECoS50210).FirstOrDefault();
            var entities = dpEcos?.Entities as IReadOnlyCollection<IEntity>;

            var ecosBase = entities?.FirstOrDefault(it => it.ObjectId == 1) as Ecos2;
            if (ecosBase != null && ecosBase.Status.Equals("STOP", StringComparison.OrdinalIgnoreCase))
            {
                statusMessage = new JObject
                {
                    ["command"] = "warning",
                    ["info"] = "The ECoS is not in state 'GO'. No command will result in any change."
                };

                return false;
            }

            if (ecosBase != null && ecosBase.Status.Equals("GO", StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }
    }
}

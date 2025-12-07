// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using libShared.DataProvider;
using libShared.ExchangeProtocol;
using libUtilities;
using railyWebApp.DataProvider;
// ReSharper disable InlineOutVariableDeclaration

namespace railyWebApp
{
    public class DataProviderManager
    {
        private static readonly ConcurrentDictionary<string, ClientDataProviders> Providers = new();

        public static IDataProvider GetDataProviderByName(string uid, string dpName)
        {
            if (string.IsNullOrEmpty(uid)) return null;
            if (string.IsNullOrEmpty(dpName)) return null;

            foreach (var it in GetDataProviders(uid))
            {
                if (string.IsNullOrEmpty(it.Name)) continue;
                if (it.Name.Equals(dpName, StringComparison.OrdinalIgnoreCase))
                    return it;
            }

            return null;
        }

        /// <summary>
        /// Retrieves an overview of data providers associated with the given authentication token,
        /// organized by their respective <see cref="DataProviderType"/>.
        /// </summary>
        /// <param name="uid">
        /// A string representing the authentication token used to identify the associated data providers.
        /// </param>
        /// <returns>
        /// A dictionary where each key is a <see cref="DataProviderType"/> and the value is a list of 
        /// provider names of that type. Returns an empty dictionary if the authentication token is invalid 
        /// or no providers are associated with it.
        /// </returns>
        /// <remarks>
        /// The method iterates over all defined <see cref="DataProviderType"/> values, excluding 
        /// <see cref="DataProviderType.None"/>, and checks which data providers match each type.
        /// </remarks>
        public static Dictionary<DataProviderType, List<string>> GetOverviewOfDataProviders(string uid)
        {
            var res = new Dictionary<DataProviderType, List<string>>();
            if (!Providers.TryGetValue(uid, out var dps)) return res;

            foreach (DataProviderType dpType in Enum.GetValues(typeof(DataProviderType)))
            {
                if (dpType == DataProviderType.None) continue;

                if (!res.ContainsKey(dpType))
                    res.Add(dpType, new List<string>());

                foreach (var it in dps)
                {
                    if (it.Type.HasFlag(dpType))
                        res[dpType].Add(it.Name.ToLower());
                }
            }

            return res;
        }

        public static IReadOnlyList<IDataProvider> GetDataProvider(string uid, DataProviderType type)
        {
            return !Providers.TryGetValue(uid, out var dps) ? null : dps.Get(type);
        }

        public static IReadOnlyList<IDataProvider> GetDataProviders(string uid)
        {
            if (Providers.TryGetValue(uid, out var dps))
                return dps;
            return new List<IDataProvider>();
        }

        public static ClientDataProviders Apply(string uid)
        {
            ClientDataProviders clientProvider;

            if (string.IsNullOrEmpty(uid)) return null;
            if (Providers.TryGetValue(uid, out clientProvider))
            {
                if (clientProvider.Count > 0)
                    return clientProvider;
            }

            if (clientProvider == null)
                clientProvider = new ClientDataProviders();

            //
            // ECoS
            //
            var dpEcos = new libEsuEcos.DataProvider.DataProvider();
            clientProvider.Add(dpEcos);

            // S88
            var dpS88 = new DataProviderS88();
            clientProvider.Add(dpS88);

            // S88-Simulator
            var dpS88Simulator = new DataProviderS88Simulator();
            clientProvider.Add(dpS88Simulator);

            // ECoS-Simulator / "demo"
            var dpDemo = new DataProviderDemo();
            clientProvider.Add(dpDemo);

            // Z21
            var dpZ21 = new libZ21.DataProvider.DataProvider();
            dpZ21.LoadPredefinedEntities(uid, libUserspace.Filesystem.FleetBaseDir);
            clientProvider.Add(dpZ21);

            Providers.TryAdd(uid, clientProvider);

            return clientProvider;
        }
        
        public static bool Update(string uid, Request request, bool isSimulationMode)
        {
            if (!Providers.TryGetValue(uid, out var dps))
            {
                Logging.Log.Debug($"No DataProviderManager for provided authentication token.");
                return false;
            }

            try
            {
                var extensionName = request.ExtensionName;

                switch (extensionName)
                {
                    //
                    // ESU ECoS 50210
                    //
                    case libEsuEcos.Globals.EsuEcosIdentifier:
                        {
                            var ecosDps = dps.Get(DataProviderType.ECoS50210);
                            var ecosDp = ecosDps.FirstOrDefault() ?? EmptyDataProvider.Empty;
                            if (ecosDp != EmptyDataProvider.Empty)
                                return ecosDp.Update([request], isSimulationMode);
                        }
                        break;

                    //
                    // This region handles incoming data by HSI-88-USB.
                    // In most cases only changes should be received.
                    // Any change should be routed to all connected browser.
                    // We do not need any special routines to handle 
                    // S88 feedback, this will be provided by AutoMode 
                    // routines/implementation. 
                    //
                    case "HSI-88-USB":
                        {
                            // Sample request from `railyHsi88Usb`:
                            /*
                                {
                                 "version": "0.1",
                                 "extensionName": "HSI-88-USB",
                                 "timestamp": "2025-01-14T07:32:20.140978Z",
                                 "data": {
                                   "type": "command",
                                   "payload": {
                                     "event": {
                                       "port": 1,
                                       "state": {
                                         "hex": "9EA9",
                                         "binary": "1001111010101001"
                                       }
                                     },
                                     "info": {
                                       "left": 0,
                                       "middle": 0,
                                       "right": 1
                                     }
                                   }
                                 }
                               }
                             */
                            if (request.Data.Payload is JObject jsonPayload)
                            {
                                var payloadS88 = jsonPayload.ToObject<PayloadS88>();
                                if (payloadS88 != null)
                                {
                                    var anySuccess = false;
                                    var s88Dps = dps.Get(DataProviderType.S88Feedback);
                                    foreach (var dp in s88Dps)
                                    {
                                        if (string.IsNullOrEmpty(dp.Name)) continue;
                                        if (!dp.Name.Equals("S88")) continue;

                                        if (dp != EmptyDataProvider.Empty)
                                            if (dp.Update([request], isSimulationMode))
                                                anySuccess = true;
                                    }

                                    if (anySuccess) return true;
                                }
                            }
                        }
                        break;

                    //
                    // Demonstration
                    //
                    case DataProviderDemo.ProviderName:
                        {
                            var demoDps = dps.Get(DataProviderType.Demo);
                            var demoDp = demoDps.FirstOrDefault() ?? EmptyDataProvider.Empty;
                            if (demoDp != EmptyDataProvider.Empty)
                                return demoDp.Update([request], isSimulationMode);
                        }
                        break;

                    //
                    // Z21
                    //
                    case libZ21.Globals.Z21Identifier:
                        {
                            var z21Dps = dps.Get(DataProviderType.Z21);
                            var z21Dp = z21Dps.FirstOrDefault() ?? EmptyDataProvider.Empty;
                            if (z21Dp != EmptyDataProvider.Empty)
                                return z21Dp.Update([request], isSimulationMode);
                        }
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }
    }
}

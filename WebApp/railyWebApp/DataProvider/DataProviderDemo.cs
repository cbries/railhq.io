// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libShared.DataProvider;
using libShared.ExchangeProtocol;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using libShared.Entities;

namespace railyWebApp.DataProvider
{
    public class DataProviderDemo :
        libEsuEcos.DataProvider.DataProvider,
        IDataProviderDemoExtension
    {
        private const string SimulationDataName = "__demoRequests.json";
        public override DataProviderType Type => DataProviderType.Demo;
        public override string Name => ProviderName;

        public const string ProviderName = "demo";

        private string GetSimulationDataPath()
        {
            var resourcePath = libUtilities.Filesystem.GetResourcesPath();
            return Path.Combine(resourcePath, SimulationDataName);
        }

        public DataProviderDemo()
        {
            LoadSimulationData();
        }

        public void LoadSimulationData()
        {
            try
            {
                var simulationData = GetSimulationDataPath();
                var cnt = File.ReadAllText(simulationData, Encoding.UTF8);
                var arr = JArray.Parse(cnt);
                var arrReqs = new List<Request>();
                foreach (var it in arr)
                {
                    var instance = JsonConvert.DeserializeObject<Request>(it.ToString());
                    arrReqs.Add(instance);
                }
                var orderedRequests = arrReqs.OrderBy(r => r.Timestamp).ToList();
                orderedRequests.ForEach(it =>
                {
                    Update([it], true);
                });

                // update all entities to "demo"
                foreach (var it in Entities)
                {
                    if (it is IEntityBase itt)
                        itt.DriverName = Name;
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        #region IDataProviderDemoExtension

        public void TriggerEntityUpdate(IEntity entity)
        {
            if (entity == null) return;

            OnEntityUpdated(entity);
        }

        #endregion IDataProviderDemoExtension
    }
}

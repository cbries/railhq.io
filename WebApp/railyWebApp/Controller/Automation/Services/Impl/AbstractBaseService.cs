// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared.DataProvider;
using libShared.Entities;
using railyWebApp.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq;
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public abstract class AbstractBaseService
    {
        protected const string DpNameDemo = DataProviderDemo.ProviderName;
        protected const string DpNameS88 = "S88";

        public string Uid { get; private set; }

        public AbstractBaseService(string uid)
        {
            Uid = uid;
        }

        public IDataProvider GetDp(string driverName)
        {
            var dps = DataProviderManager.Apply(Uid);
            if (dps == null) return null;
            return driverName switch
            {
                libEsuEcos.Globals.EsuEcosIdentifier => dps.Get(DataProviderType.ECoS50210).FirstOrDefault(),
                libZ21.Globals.Z21Identifier => dps.Get(DataProviderType.Z21).FirstOrDefault(),
                DpNameDemo => dps.Get(DataProviderType.Demo).FirstOrDefault(),
                DpNameS88 => dps.Get(DataProviderType.S88Feedback).FirstOrDefault(),
                _ => null
            };
        }

        protected ILocomotive GetLocomotiveEntity(string driverName, int address)
        {
            var dp = GetDp(driverName);
            if (dp == null) return null;
            var locs = (dp.Entities as IReadOnlyCollection<IEntity>)?.
                Where(it => it.Type == EntityType.Locomotive);
            if (locs == null || !locs.Any()) return null;
            return locs?.FirstOrDefault(it =>
            {
                var l = it as ILocomotive;
                if (l != null)
                    if (l.Address.Equals($"{address}", StringComparison.Ordinal))
                        return true;
                if (l != null && l.ObjectId == address)
                    return true;
                return false;
            }) as ILocomotive;
        }

        protected IAccessory GetAccessoryEntity(string driverName, int address)
        {
            var dp = GetDp(driverName);
            if (dp == null) return null;
            var locs = (dp.Entities as IReadOnlyCollection<IEntity>)?.
                Where(it => it.Type == EntityType.Accessory);
            if (locs == null || !locs.Any()) return null;
            return locs?.FirstOrDefault(it =>
            {
                var l = it as IAccessory;
                if (l != null)
                    if (l.Address.Equals($"{address}", StringComparison.Ordinal))
                        return true;
                if (l != null && l.ObjectId == address)
                    return true;
                return false;
            }) as IAccessory;
        }

        protected IEntityS88 GetFeedbackEntity(string driverName, int module)
        {
            var dp = GetDp(driverName) as IDataProviderFeedback;
            return dp?.Ports.GetValueOrDefault(module);
        }
    }
}

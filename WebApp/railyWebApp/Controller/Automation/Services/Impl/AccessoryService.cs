// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using libShared.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using railyWebApp.Controller.Automation.Dto.Entity;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class AccessoryService : AbstractBaseService, IAccessoryService
    {
        public AccessoryService(string uid) : base(uid)
        {
            // ignore
        }

        #region IAccessoryService

        public async Task<IEnumerable<Accessory>> GetAll()
        {
            var dps = DataProviderManager.Apply(Uid);
            if (dps == null) return new List<Accessory>();

            var res = new List<Accessory>();

            foreach (var itDp in dps)
            {
                if (itDp == null) continue;
                var locs = (itDp.Entities as IReadOnlyCollection<IEntity>)?.
                    Where(it => it.Type == EntityType.Accessory);
                if (locs == null || !locs.Any()) continue;
                foreach (var itAcc in locs)
                {
                    var acc = itAcc as IAccessory;
                    if (acc == null) continue;

                    var name = acc.DisplayName;
                    if (string.IsNullOrEmpty(name))
                        name = acc.Name0;

                    var instance = new Accessory()
                    {
                        DriverName = acc.DriverName,
                        Address = acc.ObjectId,
                        Name = name,
                        Type = "SWITCH",
                        CurrentState = acc.State
                    };

                    res.Add(instance);
                }
            }

            return res;
        }

        public async Task<Accessory> Get(string driverName, int address)
        {
            var acc = GetAccessoryEntity(driverName, address);
            if (acc == null) return null;

            var name = acc.DisplayName;
            if (string.IsNullOrEmpty(name))
                name = acc.Name0;

            var res = new Accessory
            {
                DriverName = driverName,
                Address = address,
                Name = name,
                Type = "SWITCH",
                CurrentState = acc.State
            };
            return res;
        }

        public async Task<bool> Switch(string driverName, int address, string targetState)
        {
            var dp = GetDp(driverName);
            if (dp == null) return false;

            var request = new AutomationRequest();
            var obj = CommandFactory.GetAccessorySwitchCommand(driverName, address, targetState);
            request.Data.Payload = obj;
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        #endregion IAccessoryService
    }
}

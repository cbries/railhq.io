// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos.Entities;
using libShared.Entities;
using libZ21.Entities;
using railyWebApp.Controller.Automation.Dto.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class ControlStationService : AbstractBaseService, IControlStationService
    {
        public ControlStationService(string uid) : base(uid)
        {
            // ignore
        }

        #region ICommandStationService

        public async Task<List<string>> GetAvailableStationsAsync()
        {
            var dps = DataProviderManager.Apply(Uid);
            var res = new List<string>();
            foreach (var itDp in dps)
            {
                var station = (itDp.Entities as IReadOnlyCollection<IEntity>)?.Where(
                    it => it.Type.HasFlag(EntityType.Ecos2) || it.Type.HasFlag(EntityType.Z21Station)).FirstOrDefault();
                if (station == null) continue;
                if (!res.Contains(station.DriverName))
                    res.Add(station.DriverName);
            }
            return res;
        }

        private IEntity GetBaseEntity(string driverName)
        {
            var dps = DataProviderManager.Apply(Uid);
            foreach (var itDp in dps)
            {
                var station = (itDp.Entities as IReadOnlyCollection<IEntity>)?.Where(
                    it => it.Type.HasFlag(EntityType.Ecos2) || it.Type.HasFlag(EntityType.Z21Station)).FirstOrDefault();
                if (station == null) continue;
                if (station.DriverName.Equals(driverName, StringComparison.OrdinalIgnoreCase))
                    return station;
            }
            return null;
        }

        public async Task<ControlStationInfo> GetInfoAsync(string driverName)
        {
            var csEntity = GetBaseEntity(driverName);

            if (csEntity is Ecos2 stationEcos2)
            {
                var instance = new ControlStationInfo();
                instance.DriverName = driverName;
                instance.Model = string.Empty;
                if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier))
                    instance.Model = "ECoS";
                else
                    instance.Model = "Demo";
                instance.FirmwareVersion = stationEcos2.ApplicationVersion;
                instance.HardwareVersion = stationEcos2.HardwareVersion;
                instance.IsRunning = stationEcos2.Status == "GO";
                return instance;
            }

            if (csEntity is Z21Station stationZ21)
            {
                var instance = new ControlStationInfo();
                instance.DriverName = driverName;
                instance.Model = stationZ21.HardwareType;
                instance.FirmwareVersion = stationZ21.FirmwareVersion;
                instance.HardwareVersion = stationZ21.HardwareType;
                instance.IsRunning = stationZ21.TrackOn;
                return instance;
            }

            return null;
        }

        public async Task<bool> SetPowerAsync(string driverName, bool on)
        {
            var csEntity = GetBaseEntity(driverName);
            if (csEntity == null) return false;
            var request = new AutomationRequest();
            request.Data.Payload = CommandFactory.GetStationPowerCommand(driverName, on);
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        public async Task<bool?> GetPowerStatusAsync(string driverName)
        {
            var csEntity = GetBaseEntity(driverName);
            if (csEntity == null) return false;

            if (csEntity is Ecos2 stationEcos2)
                return stationEcos2.Status == "GO";

            if (csEntity is Z21Station stationZ21)
                return stationZ21.TrackOn;

            return false;
        }

        #endregion
    }
}

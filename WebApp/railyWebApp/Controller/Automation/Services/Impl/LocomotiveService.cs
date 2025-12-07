// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using libShared.Entities;
using railyWebApp.Controller.Automation.Dto.Entity;
using railyWebApp.DataProvider;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class LocomotiveService : AbstractBaseService, ILocomotiveService
    {
        public LocomotiveService(string uid) : base(uid)
        {
            // ignore
        }
        
        #region ILocomotiveService

        // OK
        public async Task<List<Locomotive>> GetAllLocomotivesAsync()
        {
            var dps = DataProviderManager.Apply(Uid);
            if (dps == null) return new List<Locomotive>();

            var res = new List<Locomotive>();

            foreach (var itDp in dps)
            {
                if (itDp == null) continue;
                var locs = (itDp.Entities as IReadOnlyCollection<IEntity>)?.
                    Where(it => it.Type == EntityType.Locomotive);
                if (locs == null || !locs.Any()) continue;
                foreach (var itLoc in locs)
                {
                    var instance = new Locomotive
                    {
                        DriverName = itLoc.DriverName,
                        Address = itLoc.ObjectId,
                        Name = itLoc.DisplayName,
                        DecoderType = (itLoc as ILocomotive)?.Protocol ?? string.Empty
                    };
                    res.Add(instance);
                }
            }

            return res;
        }
        
        // OK
        public async Task<Locomotive> GetLocomotiveAsync(string driverName, int address)
        {
            var loc = GetLocomotiveEntity(driverName, address);
            if (loc == null) return null;
            var res = new Locomotive
            {
                DriverName = driverName,
                Address = address,
                Name = loc.DisplayName,
                DecoderType = loc.Protocol
            };
            return res;
        }

        // OK
        public async Task<bool> SetSpeedAsync(string driverName, int address, int speed)
        {
            var dp = GetDp(driverName);
            if (dp == null) return false;
            var request = new AutomationRequest();
            request.Data.Payload = CommandFactory.GetSpeedCommand(driverName, address, speed);
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        // OK
        public async Task<bool> SetDirectionAsync(string driverName, int address, bool forward)
        {
            var dp = GetDp(driverName);
            if (dp == null) return false;

            var request = new AutomationRequest();
            var obj = CommandFactory.GetDirectionCommand(driverName, address, forward);
            request.Data.Payload = obj;
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        // OK
        public async Task<bool> StopLocomotiveAsync(string driverName, int address)
        {
            const int stopSpeed = 0;

            var dp = GetDp(driverName);
            if (dp == null) return false;
            var request = new AutomationRequest();
            request.Data.Payload = CommandFactory.GetSpeedCommand(driverName, address, stopSpeed);
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        // OK
        public async Task<bool> ToggleFunctionAsync(string driverName, int address, int functionNumber, bool state)
        {
            var dp = GetDp(driverName);
            if (dp == null) return false;
            var request = new AutomationRequest();
            request.Data.Payload = CommandFactory.GetFunctionCommand(driverName, address, functionNumber, state);
            var res = await WebSocketModule.HandleWebRequestAsync(Uid, request);
            return res;
        }

        // OK
        public async Task<LocomotiveState> GetStatusAsync(string driverName, int address)
        {
            var loc = GetLocomotiveEntity(driverName, address);
            if (loc == null) return null;

            var res = new LocomotiveState
            {
                DriverName = driverName,
                Address = address,
                
                Name = loc.DisplayName,
                DecoderType = loc.Protocol,

                IsDriving = loc.Speedstep > 0,
                IsFunctionAvailable = loc.Functions.Count > 0,

                Speed = loc.Speedstep,
                SpeedSteps = LocomotiveUtilities.GetNumberOfSpeedsteps(loc.Protocol),
                Direction = loc.Direction == LocomotiveDirection.Backward ? 0 : 1,

                // TODO
                // BlockId
                // AutomationState

                Tags = new List<string>()
            };

            foreach (var itFnc in loc.Functions)
            {
                // ecos based
                if (driverName.Equals(libEsuEcos.Globals.EsuEcosIdentifier)
                    || driverName.Equals(DataProviderDemo.ProviderName))
                {
                    if (itFnc.FunctionType == 0) continue;

                    res.Functions.Add(itFnc.FunctionIndex, itFnc.State);
                }
                else
                {
                    if (!itFnc.IsUsed) continue;

                    res.Functions.Add(itFnc.FncIdx, itFnc.State);
                }
            }

            return res;
        }

        #endregion ILocomotiveService
    }
}

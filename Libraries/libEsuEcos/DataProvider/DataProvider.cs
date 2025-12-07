// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using libEsuEcos.Blocks;
using libEsuEcos.Entities;
using libShared.DataProvider;
using libShared.Entities;
using libShared.ExchangeProtocol;
using libUtilities;
using libShared.Entities.Impl;

namespace libEsuEcos.DataProvider
{
    internal class EntitiesContainer : ConcurrentBag<IEntity>
    {
        public bool Has(int objectId, out IEntity entity)
        {
            entity = null;

            foreach (var it in this)
            {
                if (it.ObjectId == -1) continue;
                if (it.ObjectId == objectId)
                {
                    entity = it;
                    return true;
                }
            }

            return false;
        }
    }

    internal class EntitiesS88Container : ConcurrentBag<IEntityS88>
    {
        public bool Has(int port, out IEntityS88 entity)
        {
            entity = null;

            foreach (var it in this)
            {
                if (it.Port == -1) continue;
                if (it.Port == port)
                {
                    entity = it;
                    return true;
                }
            }

            return false;
        }
    }

    public class DataProvider
        : libShared.DataProvider.DataProvider
        , IEntityInverter
    {
        public override DataProviderType Type => DataProviderType.ECoS50210 | DataProviderType.S88Feedback;

        public override string Name => Globals.EsuEcosIdentifier;

        private readonly EntitiesContainer _entitiesContainer = new();
        private readonly EntitiesS88Container _entitiesS88Container = new();

        public override ICollection Entities => _entitiesContainer;

        #region IEntityInverter: State Cache Handling

        private readonly Dictionary<int, int> _accessoryStates = new();
        public int GetAccessoryState(int objectId) => _accessoryStates.GetValueOrDefault(objectId, 0);
        public int GetAccessoryState(IEntity entity) => GetAccessoryState(entity.ObjectId);
        public void SetAccessoryState(int objectId, int state) => _accessoryStates[objectId] = state;
        public void SetAccessoryState(IEntity entity, int state) => _accessoryStates[entity.ObjectId] = state;
        public int InvertAccessoryState(IEntity entity)
        {
            if (entity == null) return -1;
            var objectId = entity.ObjectId;
            var current = GetAccessoryState(objectId);
            var newState = 1 - current;
            SetAccessoryState(objectId, newState);
            return newState;
        }

        public int ChangeAccessoryState(IEntity entity)
        {
            if (entity == null) return -1;
            var objectId = entity.ObjectId;
            var currentState = GetAccessoryState(objectId);
            var newState = currentState == 0 ? 1 : 0;
            SetAccessoryState(objectId, newState);
            return newState;
        }

        #endregion

        private bool _isSimulationMode;

        public override bool Update(IReadOnlyList<Request> requests, bool isSimulationMode)
        {
            _isSimulationMode = isSimulationMode;

            if (requests == null) return false;
            if (requests.Count == 0) return false;

            try
            {
                foreach (var req in requests)
                {
                    var res = UpdateWith(req);
                    if (!res)
                        Logging.Log.Debug($"Request update failed: {req.Data.Payload.ToString().Inline()}");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        private bool UpdateWith(Request request)
        {
            //Logging.Log.Debug($"<UpdateWith> {request.Data.Payload}");

            var payload = request.Data.Payload as JObject;
            if (payload == null)
            {
                Logging.Log.Debug("Invalid payload format, JSON required.");
                return false;
            }

            var commandBlocks = payload["commandBlocks"] as JArray;
            if (commandBlocks == null)
            {
                Logging.Log.Debug("No valid command blocks provided.");
                return false;
            }
            if (commandBlocks.Count == 0) return true;

            foreach (var blkTkn in commandBlocks)
            {
                var blkStr = (string)blkTkn!;
                if (string.IsNullOrEmpty(blkStr)) continue;
                var blockInstances = BlockUtilities.ExtractBlocks(ref blkStr);
                if (blockInstances.Count == 0) continue;
                var res = UpdateWithBlocks(blockInstances);
                if (!res)
                    Logging.Log.Debug($"Parsing blocks failed: {blkStr}");
            }

            return true;
        }

        private bool UpdateWithBlocks(IReadOnlyList<IBlock> blocks)
        {
            foreach (var blk in blocks)
            {
                var res = UpdateWithBlock(blk);
                if (!res)
                    Logging.Log.Debug($"Block update failed: {blk}");
            }

            return true;
        }

        private bool UpdateWithBlock(IBlock block)
        {
            switch (block)
            {
                case ReplyBlock replyBlock:
                    return HandleReply(replyBlock);

                case EventBlock eventBlock:
                    return HandleEvent(eventBlock);

                default:
                    Logging.Log.Debug($"unknown Block type: {block.NativeBlock}");
                    break;
            }

            return true;
        }

        private bool HandleReply(ReplyBlock block)
        {
            // filter relevant commands
            // I guess we just need:
            //     {get, request, queryObjects}
            var cmd = block?.Command;
            if (Command.IsRequest(cmd)) return true;

            // avoid invalid commands
            // Samples:
            /*
:              <REPLY get(26, state)>
               <END 11 (unknown option at 9)>
               
               <REPLY get(20, state)>
               <END 15 (unknown object at 6)> 

               <REPLY set(1001, speedstep[15])>
               <END 25 (controlled by somebody else)>
             */
            if (block != null)
            {
                switch (block.Result.ErrorCode)
                {
                    case 11: // unknown option at x
                    case 15: // unknown object at x
                    case 25: // controlled by somebody else
                        {
                            Logging.Log.Debug($"TODO check block handling for: {block.NativeBlock.Inline()}");
                            return true;
                        }
                }

                // +++++
                // keep as code fallback (it is working legacy code)
                if ((block.EndLine.IndexOf("unknown option", StringComparison.OrdinalIgnoreCase) != -1
                     || block.EndLine.IndexOf("unknown object", StringComparison.OrdinalIgnoreCase) != -1))
                {
                    Logging.Log.Debug($"TODO check block handling for: {block.NativeBlock.Inline()}");
                    return true;
                }
                // ++++++++++++++
            }

            if (cmd != null)
            {
                var res = _entitiesContainer.Has(cmd.ObjectId, out var entity);
                if (res)
                {
                    if (Command.IsGet(cmd))
                        return HandleGetUpdate(block);
                    if (Command.IsSet(cmd))
                        return true; // TODO do we need to handle `set`
                    if (Command.IsRelease(cmd))
                        return true; // TODO do we need to handle `release`

                    return false;
                }

                if (Command.IsQueryObjects(cmd))
                    return HandleQueryObjects(block);

                var cleanedNativeBlock = block.NativeBlock.Replace("\r", string.Empty).Trim();
                Logging.Log.Debug($"REPLY: {cleanedNativeBlock}");

                if (Command.IsGet(cmd))
                    return HandleGet(block);

                Logging.Log.Warn($"Missing handling for Block: {block.NativeBlock.Inline()}");

                return true;
            }

            return false;
        }

        private enum FeedbackDeviceType
        {
            S88Device, DetectorDevice, UnknownDevice
        }

        private bool HandleEvent(EventBlock block)
        {
            var objectId = block.ObjectId;
            if (objectId == -1) return false;

            var isS88BusDevice = objectId >= 100 && objectId < 131;
            var isECoSDetectorDevice = objectId >= 200 && objectId < 231;

            //
            // S88 event received
            // 
            if (
                    //
                    // S88-Bus devices
                    //
                    isS88BusDevice
                    ||
                    //
                    // ESU ECoS-Detectors
                    //
                    isECoSDetectorDevice
                )
            {
                // ignore real S88 feedback data
                // we use a S88 simulator in this case
                // the customer decided
                if (_isSimulationMode)
                    return false;

                var t = FeedbackDeviceType.UnknownDevice;
                if (isS88BusDevice) t = FeedbackDeviceType.S88Device;
                else if (isECoSDetectorDevice) t = FeedbackDeviceType.DetectorDevice;

                var res = HandleS88Event(block, t);

                return res;
            }

            if (_entitiesContainer.Has(objectId, out IEntity entity))
            {
                var res = entity.ParseData(block);
                if (res)
                {
                    //
                    // Jede Änderung eines Schaltartikels sollte gecached werden, 
                    // so dass eine ordentliche Schaltung für invertierte 
                    // Schaltvorgänge problemlos abläuft.
                    //
                    if (entity.Type == EntityType.Accessory && entity is IAccessory acc)
                    {
                        if (int.TryParse(acc.State, out var istate))
                            SetAccessoryState(entity, istate);
                    }

                    OnEntityUpdated(entity);
                }

                return res;
            }

            Logging.Log.Debug($"EVENT failed -> id({block.ObjectId}) unknown");

            return false;
        }

        private bool IsS88DeviceUsed = false;
        private bool IsECoSDetectorUsed = false;

        private bool HandleS88Event(EventBlock eventBlock, FeedbackDeviceType deviceType)
        {
            var objectId = eventBlock.ObjectId;

            foreach (var entry in eventBlock.ListEntries)
            {
                if (entry.Arguments[0].Name.Equals("state", StringComparison.OrdinalIgnoreCase))
                {
                    var stateValueHex = entry.Arguments[0].Parameter[0].Trim().Replace("0x", string.Empty);
                    var stateValueBinary = Converters.ToBinary(stateValueHex);

                    const int offsetS88Device = 99;
                    const int offsetEcosDetectorDevice = 199;

                    var portIndex = 0;
                    if (deviceType == FeedbackDeviceType.S88Device)
                    {
                        IsS88DeviceUsed = true;
                        portIndex = objectId - offsetS88Device;
                    }
                    else if (deviceType == FeedbackDeviceType.DetectorDevice)
                    {
                        IsECoSDetectorUsed = true;
                        portIndex = 31 // Offset für ECoSDetector-Geräte, Port 1..31 sind S88-Bus-Ports
                            + objectId - offsetEcosDetectorDevice;
                        // portIndex >= 32
                    }

                    Logging.Log.Debug($"S88(ObjectId: {objectId} Port: {portIndex}) -> {stateValueHex}   {stateValueBinary}");

                    //
                    // maximum ports supported by ECoS
                    // 
                    // Die maximale Anzahl wird für den S88-Bus sowie die ECoSDetectors gleich 
                    // verwendet, also in beiden Welten werden derzeit von railhq.io jeweils maximal 
                    // `maxPort` unterstützt. Zur Wahrheit gehört, wir wissen gar nicht wie 
                    // viele ECoSDetectors maximal unterstützt werden.
                    //
                    // Wenn beide Varianten vorhanden sind, so sind nach unserer
                    // Definition bis zu 62 Geräter maximal möglich, daher wird hier
                    // für den ECoS-DataProvider der maximale Wert hochgesetzt.
                    // Das Hochsetzen kann während der Laufzeit geschehen,
                    // ein Zurücksetzen auf den Standardwert 31 ist nur nach
                    // einem Neustart/Neuladen der Extension möglich.
                    var maxPorts = 31;
                    if (IsECoSDetectorUsed)
                        maxPorts = 62;

                    //
                    // the main challenge at this point is the objectId and portIdx of S88
                    // the ECoS provides S88-feedback between objectId 100 and objectId 131
                    // S88 start from 1 to 31 in generally
                    // keeping an entity with objectId 1 is not possible because of ECoS base id
                    //

                    if (_entitiesS88Container.Has(portIndex, out IEntityS88 availableEntity))
                    {
                        if (availableEntity is S88Entity s88)
                        {
                            var changed = false;
                            if (!s88.HexState.Equals(stateValueHex, StringComparison.OrdinalIgnoreCase))
                            {
                                changed = true;
                                s88.HexState = stateValueHex;
                                s88.BinaryState = stateValueBinary;
                            }

                            if (changed)
                                OnEntityUpdated(availableEntity);
                        }
                    }
                    else
                    {
                        var entity = new S88Entity
                        {
                            Port = portIndex,
                            MaxPorts = maxPorts,
                            HexState = stateValueHex,
                            BinaryState = stateValueBinary,
                            DriverName = Globals.EsuEcosIdentifier
                        };

                        _entitiesS88Container.Add(entity);

                        OnEntityUpdated(entity);
                    }
                }
            }

            return true;
        }

        private bool HandleGet(ReplyBlock block)
        {
            var cmd = block.Command;

            switch (cmd.ObjectId)
            {
                case Globals.ID_EV_ECoS:
                    {
                        var ecosEntity = new Ecos2();
                        if (ecosEntity.ParseData(block))
                        {
                            _entitiesContainer.Add(ecosEntity);
                            OnEntityAdded(ecosEntity);
                        }
                    }
                    break;
            }

            return true;
        }

        private bool HandleGetUpdate(ReplyBlock block)
        {
            var cmd = block.Command;

            switch (cmd.ObjectId)
            {
                case Globals.ID_EV_ECoS:
                    {
                        if (_entitiesContainer.Has(Globals.ID_EV_ECoS, out var ecosEntity))
                        {
                            var res = ecosEntity.ParseData(block);
                            if (res)
                                OnEntityUpdated(ecosEntity);
                            return res;
                        }
                    }
                    break;

                case var esuObjectId when (
                    Globals.IsLocomotiveId(esuObjectId)
                    || Globals.IsAccessorySwitch(esuObjectId)):
                    {
                        if (_entitiesContainer.Has(esuObjectId, out var esuEntity))
                        {
                            var res = esuEntity.ParseData(block);
                            if (res)
                                OnEntityUpdated(esuEntity);
                            return res;
                        }
                    }
                    break;
            }

            return true;
        }

        private bool HandleQueryObjects(ReplyBlock block)
        {
            var cmd = block.Command;

            switch (cmd.ObjectId)
            {
                case Globals.ID_EV_LokManager:
                    {
                        // queryObjects for LokManager returns a list of registered Locomotives
                        /*
                            <REPLY queryObjects(10, addr, name, protocol)>
                            1000 name["Schweineschnauze"] addr[54] protocol[MM14]
                            1001 name["BR_232_371-5"] addr[9] protocol[DCC28]
                            ..
                            ..
                            <END 0 (OK)>                     
                         */

                        foreach (var item in block.ListEntries)
                        {
                            var locObjectId = item.ObjectId;
                            if (locObjectId == -1) continue;
                            var res = _entitiesContainer.Has(locObjectId, out var entity);
                            if (res)
                            {
                                // TODO update known locomotive
                            }
                            else
                            {
                                var locEntity = new Locomotive();
                                if (locEntity.ParseData(item))
                                {
                                    _entitiesContainer.Add(locEntity);
                                    OnEntityAdded(locEntity);
                                }
                            }
                        }
                    }
                    break;

                case Globals.ID_EV_SchaltartikelManager:
                    {
                        // queryObjects for SchaltartikelManager returns a list of registered Switches/Signals
                        /*
                            <REPLY queryObjects(11, addr, protocol)>
                            20001 name1["S2_6"] name2[""] name3[""] addrext[1g,1r] addr[1] protocol[DCC] type[ACCESSORY] mode[SWITCH] symbol[0]
                            20000 name1["S2_5"] name2[""] name3[""] addrext[2g,2r] addr[2] protocol[DCC] type[ACCESSORY] mode[SWITCH] symbol[0]
                            20002 name1["S2_4"] name2[""] name3[""] addrext[3g,3r] addr[3] protocol[DCC] type[ACCESSORY] mode[SWITCH] symbol[0]
                            ..
                            ..
                            30000 name1["BK_1"] name2[""] name3[""] type[ROUTE]
                            30001 name1["Neuer "] name2["Fahrweg"] name3[""] type[ROUTE]
                            ..
                            ..
                            <END 0 (OK)>                         
                         */

                        foreach (var item in block.ListEntries)
                        {
                            var accessoryObjectId = item.ObjectId;
                            if (accessoryObjectId == -1) continue;
                            var res = _entitiesContainer.Has(accessoryObjectId, out var entity);
                            if (res)
                            {
                                Logging.Log.Debug($"TODO update known accessory: {accessoryObjectId}");
                            }
                            else
                            {
                                var accessoryEntity = new Accessory();
                                if (accessoryEntity.ParseData(item))
                                {
                                    _entitiesContainer.Add(accessoryEntity);
                                    OnEntityAdded(accessoryEntity);
                                }
                            }
                        }
                    }
                    break;
            }

#if DEBUG
            Logging.Log.Info($"{Name} Entities: {_entitiesContainer.Count}");
#endif

            return true;
        }
    }
}

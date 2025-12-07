// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using libEsuEcos.Blocks;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json.Linq;

namespace libEsuEcos.Entities
{
    public class Ecos2 : Entity
    {
        public override EntityType Type => EntityType.Ecos2;

        #region Entity data provided by ECoS

        public string Name => DisplayName;
        public string ProtocolVersion { get; private set; } = string.Empty;
        public string ApplicationVersion { get; private set; } = string.Empty;
        public string HardwareVersion { get; private set; } = string.Empty;
        public string Status { get; private set; } = "STOP";

        #endregion

        public Ecos2()
        {
            ObjectId = Globals.ID_EV_ECoS;
        }

        public override JObject ToJsonObject()
        {
            var o = new JObject
            {
                ["driverName"] = DriverName,
                ["status"] = Status,
                ["name"] = DisplayName,
                ["protocolVersion"] = ProtocolVersion,
                ["applicationVersion"] = ApplicationVersion,
                ["hardwareVersion"] = HardwareVersion
            };

            return o;
        }

        public override bool ParseData(object data)
        {
            switch (data)
            {
                case ReplyBlock replyBlock:
                    {
                        var cmd = replyBlock.Command;
                        if (!Command.IsGet(cmd)) return false;

                        if (cmd.ArgumentsHas("info"))
                        {
                            if (replyBlock.ListEntries.Count >= 4)
                            {
                                DisplayName = replyBlock.ListEntries[0].Arguments[0].Name;
                                ProtocolVersion = replyBlock.ListEntries[1].Arguments[0].Parameter[0];
                                ApplicationVersion = replyBlock.ListEntries[2].Arguments[0].Parameter[0];
                                HardwareVersion = replyBlock.ListEntries[3].Arguments[0].Parameter[0];
                            }
                            else
                            {
                                Logging.Log.Debug($"Incorrect `info` reply.");
                            }
                        }
                        else if (cmd.ArgumentsHas("status"))
                        {
                            Status = replyBlock.ListEntries[0].Arguments[0].Parameter[0];
                        }

                        return true;
                    }

                case EventBlock eventBlock:
                    {
                        foreach (var it in eventBlock.ListEntries)
                        {
                            if (it.ObjectId == -1) continue;
                            if (it.Arguments.Count == 0) continue;
                            var arg = it.Arguments[0];

                            //
                            // We do not need to provide "railcom"-Events to the server.
                            // In most cases these events are only relevant for the ECoS station alone.
                            //
                            if (!string.IsNullOrEmpty(arg?.Name))
                            {
                                if (arg.Name.StartsWith("railcom", StringComparison.OrdinalIgnoreCase))
                                    return false;
                            }

                            if (arg != null && arg.Is("status"))
                            {
                                Status = it.Arguments[0].Parameter[0];
                            }
                        }

                        return true;
                    }

                default:
                    // unknown type
                    return false;
            }
        }
    }
}

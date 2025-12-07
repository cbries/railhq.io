// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libEsuEcos.Blocks;
using libShared.Entities;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;

namespace libEsuEcos.Entities
{
    public class Accessory : Entity, IAccessory
    {
        public override EntityType Type => EntityType.Accessory;

        public string Address { get; private set; } = string.Empty;
        public string Protocol { get; private set; } = string.Empty;
        public string Category { get; private set; } = string.Empty;
        public List<string> AddrExt { get; } = new();
        public string Mode { get; private set; } = string.Empty;
        public string Symbol { get; private set; } = string.Empty;
        public int Gates { get; set; } = 0;
        public string State { get; private set; } = string.Empty;
        public string Switching { get; private set; } = string.Empty;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <returns>true when the entity is allowed to use by Raily</returns>
        public override bool ParseData(object data)
        {
            switch (data)
            {
                case ListEntry entry:
                    {
                        if (entry.ObjectId == -1) return false;

                        if (entry.Arguments.Count == 0)
                        {
                            // ignore accessories like "Fahrstraße"
                            // entities like this are created/managed by Raily itself
                            return false;
                        }

                        ObjectId = entry.ObjectId;

                        foreach (var arg in entry.Arguments)
                            ParseArgParameter0(arg);

                        return true;
                    }

                case EventBlock eventBlock:
                    {
                        var singletonListEntries = eventBlock.ListEntries.GetSingletonEntries();

                        foreach (var it in singletonListEntries)
                        {
                            if (it.ObjectId == -1) continue;
                            if (it.Arguments.Count == 0) continue;
                            var arg = it.Arguments[0];

                            ParseArgParameter0(arg);
                        }

                        return true;
                    }

                case ReplyBlock replyBlock:
                    {
                        var cmd = replyBlock.Command;
                        if (!Command.IsGet(cmd)) return false;

                        if (cmd.ArgumentsHas("state"))
                        {
                            State = replyBlock.ListEntries[0].Arguments[0].Parameter[0];
                        }
                        else if (cmd.ArgumentsHas("switching"))
                        {
                            Switching = replyBlock.ListEntries[0].Arguments[0].Parameter[0];
                        }

                        return true;
                    }

                default:
                    // unknown type
                    return false;
            }
        }

        public override JObject ToJsonObject()
        {
            var o = new JObject
            {
                ["name1"] = Name0,
                ["name2"] = Name1,
                ["name3"] = Name2
            };
            var a0 = new JArray();
            foreach (var e in AddrExt)
                a0.Add(e);
            o["objectId"] = ObjectId;
            o["driverName"] = DriverName;
            o["addrext"] = a0;
            o["addr"] = Address;
            o["protocol"] = Protocol;
            o["type"] = Type.ToString();
            o["mode"] = Mode;
            o["state"] = State;
            o["switching"] = Switching;
            o["gates"] = Gates;

            return o;
        }

        private void ParseArgParameter0(CommandArgument arg)
        {
            if (arg.Is("addr")) Address = arg.Parameter[0];
            else if (arg.Is("protocol")) Protocol = arg.Parameter[0];
            else if (arg.Is("type")) Category = arg.Parameter[0];
            else if (arg.Is("addrext"))
            {
                AddrExt.Clear();
                foreach (var it in arg.Parameter)
                    AddrExt.Add(it.Trim());
            }
            else if (arg.Is("mode")) Mode = arg.Parameter[0];
            else if (arg.Is("symbol")) Symbol = arg.Parameter[0];
            else if (arg.Is("name1")) Name0 = arg.Parameter[0];
            else if (arg.Is("name2")) Name1 = arg.Parameter[0];
            else if (arg.Is("name3")) Name2 = arg.Parameter[0];
            else if (arg.Is("gates"))
            {
                if (int.TryParse(arg.Parameter[0], out var result))
                    Gates = result;
            }
            else if (arg.Is("state")) State = arg.Parameter[0];
            else if (arg.Is("switching")) Switching = arg.Parameter[0];
        }
    }
}

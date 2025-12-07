// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using libEsuEcos.Blocks;
using libShared.Entities;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
// ReSharper disable RedundantDefaultMemberInitializer

namespace libEsuEcos.Entities
{
    public class FuncDescItem : IFuncDescItem
    {
        public int FunctionIndex { get; set; }
        public int FunctionType { get; set; } = 0;
        public bool Moment { get; set; } = false;
        public bool State { get; set; } = false;

        #region not used for ECoS

        public int FncIdx { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public string Icon { get; set; } = string.Empty;

        #endregion
    }

    public class Locomotive : Entity, ILocomotive
    {
        [JsonProperty("protocol")] public string Protocol { get; private set; } = string.Empty;
        [JsonProperty("address")] public string Address { get; private set; } = string.Empty;
        [JsonProperty("direction")] public LocomotiveDirection Direction { get; private set; } = LocomotiveDirection.Forward;
        [JsonProperty("maxSpeed")] public int MaxSpeed => LocomotiveUtilities.GetNumberOfSpeedsteps(Protocol);
        [JsonProperty("speedstep")] public int Speedstep { get; private set; } = 0;
        [JsonProperty("functions")] public List<IFuncDescItem> Functions { get; } = new();

        [JsonProperty("type")] public override EntityType Type => EntityType.Locomotive;
        [JsonProperty("name")] public string Name => DisplayName;

        private IFuncDescItem GetFuncDescItem(int idx)
        {
            if (Functions.Count == 0) return null;
            if (idx >= Functions.Count) return null;
            if (idx < 0) return null;
            return Functions[idx];
        }

        private void ApplyFuncUpdate(ListEntry entry)
        {
            if (entry == null) return;

            var arg = entry.Arguments[0];

            var pars = arg.Parameter;
            if (pars.Count == 2)
            {
                try
                {
                    if (int.TryParse(pars[0], out var fncIndex)
                        && int.TryParse(pars[1], out var fncState))
                    {
                        var fnc = GetFuncDescItem(fncIndex);
                        if (fnc != null)
                            fnc.State = fncState == 1;
                    }
                }
                catch
                {
                    // ignore
                }
            }
        }

        public override JObject ToJsonObject()
        {
            var arFncDesc = new JArray();
            for (var i = 0; i < Functions.Count; ++i)
            {
                if (Functions[i].FunctionType == 0) continue;

                var odesc = new JObject
                {
                    ["idx"] = i,
                    ["state"] = Functions[i].State,
                    ["type"] = Functions[i].FunctionType,
                    ["moment"] = Functions[i].Moment
                };
                arFncDesc.Add(odesc);
            }

            var o = new JObject
            {
                ["driverName"] = DriverName,
                ["objectId"] = ObjectId,
                ["name"] = DisplayName,
                ["protocol"] = Protocol,
                ["addr"] = Address,

                ["speedstep"] = Speedstep,
                ["speedstepMax"] = LocomotiveUtilities.GetNumberOfSpeedsteps(Protocol),
                ["direction"] = (int)Direction,
                ["funcdesc"] = arFncDesc,
                ["nrOfFunctions"] = Functions.Count
            };

            return o;
        }

        public override bool ParseData(object data)
        {
            switch (data)
            {
                case ListEntry entry:
                    {
                        if (entry.ObjectId == -1) return false;

                        ObjectId = entry.ObjectId;

                        foreach (var arg in entry.Arguments)
                        {
                            if (arg.Is("name")) DisplayName = arg.Parameter[0];
                            else if (arg.Is("dir"))
                            {
                                if (int.TryParse(arg.Parameter[0], out var result))
                                    Direction = (LocomotiveDirection)result;
                            }
                            else if (arg.Is("addr")) Address = arg.Parameter[0];
                            else if (arg.Is("protocol")) Protocol = arg.Parameter[0];
                            else if (arg.Is("speedstep"))
                            {
                                if (int.TryParse(arg.Parameter[0], out var result))
                                    Speedstep = result;
                            }
                        }

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

                            if (arg.Is("name")) DisplayName = arg.Parameter[0];
                            else if (arg.Is("dir"))
                            {
                                if (int.TryParse(arg.Parameter[0], out var result))
                                    Direction = (LocomotiveDirection)result;
                            }
                            else if (arg.Is("addr")) Address = arg.Parameter[0];
                            else if (arg.Is("protocol")) Protocol = arg.Parameter[0];
                            else if (arg.Is("speedstep"))
                            {
                                if (int.TryParse(arg.Parameter[0], out var result))
                                    Speedstep = result;
                            }
                            else if (arg.Is("func"))
                                ApplyFuncUpdate(it);
                        }

                        return true;
                    }

                case ReplyBlock replyBlock:
                    {
                        var cmd = replyBlock.Command;
                        if (cmd.ObjectId != ObjectId)
                        {
                            Logging.Log.Debug($"Object identifier mismatch: {replyBlock.Command.ObjectId} != {ObjectId}");
                            return false;
                        }

                        if (cmd.ArgumentsHas("dir"))
                        {
                            var firstListEntry = replyBlock.GetListEntriesOf("dir").FirstOrDefault();
                            var args = firstListEntry?.Arguments;
                            if (args != null)
                            {
                                if (int.TryParse(args[0].Parameter[0], out var result))
                                    Direction = (LocomotiveDirection)result;
                            }
                        }

                        // Wenn wir beide Datensätze erhalten, dann
                        // ist die Reihenfolge der Bearbeitung wichtig.
                        // Erst "funcdesc", dann "func", da "func" Daten
                        // von "funcdesc" abfragt.
                        if (cmd.ArgumentsHas("funcdesc") && cmd.ArgumentsHas("func"))
                        {
                            ParseFuncdesc(replyBlock);
                            ParseFunc(replyBlock);
                        }
                        else
                        {
                            if (cmd.ArgumentsHas("func")) ParseFunc(replyBlock);
                            if (cmd.ArgumentsHas("funcdesc")) ParseFuncdesc(replyBlock);
                        }

                        if (cmd.ArgumentsHas("speedstep"))
                        {
                            var speedstepEntries = replyBlock.GetListEntriesOf("speedstep");

                            foreach (var itt in speedstepEntries)
                            {
                                if (itt.Arguments[0].Name.Equals("speedstep", StringComparison.Ordinal))
                                {
                                    if (int.TryParse(itt.Arguments[0].Parameter[0], out var v))
                                    {
                                        if (Speedstep == v)
                                            return false;

                                        Speedstep = v;
                                    }
                                }
                            }
                        }

                        return true;
                    }

                default:
                    // unknown type
                    return false;
            }
        }

        private void ParseFunc(ReplyBlock replyBlock)
        {
            var funcEntries = replyBlock.GetListEntriesOf("func");
            foreach (var it in funcEntries)
                ApplyFuncUpdate(it);
        }

        private void ParseFuncdesc(ReplyBlock replyBlock)
        {
            var funcdescEntries = replyBlock.GetListEntriesOf("funcdesc");

            foreach (var it in funcdescEntries)
            {
                if (it.ObjectId != ObjectId) continue;

                var functionIndex = -1;
                var functionType = 0;
                var functionMoment = false;

                var arg = it.Arguments[0];
                var n = arg.Parameter.Count;
                if (n > 1)
                {
                    int.TryParse(arg.Parameter[0], out functionIndex);
                    int.TryParse(arg.Parameter[1], out functionType);
                }

                if (n > 2)
                {
                    if (arg.Parameter[2].Equals("moment"))
                        functionMoment = true;
                }

                var fnc = GetFuncDescItem(functionIndex);
                if (fnc == null)
                {
                    fnc = new FuncDescItem();
                    Functions.Add(fnc);
                }

                fnc.FunctionIndex = functionIndex;
                fnc.FunctionType = functionType;
                fnc.Moment = functionMoment;
            }
        }
    }
}

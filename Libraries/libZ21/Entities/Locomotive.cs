// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using libShared.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;
using libZ21.EntitiesPredefined;
using Newtonsoft.Json.Linq;
// ReSharper disable RedundantDefaultMemberInitializer

namespace libZ21.Entities
{
    public class FuncDescItem : IFuncDescItem
    {
        // normally used by ESU l
        public int FunctionIndex { get; set; }
        public int FunctionType { get; set; } = 0;
        public bool Moment { get; set; } = false;
        public bool State { get; set; } = false;

        public int FncIdx { get; set; } = -1;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public string Icon { get; set; } = string.Empty;
    }

    public class Locomotive : Z21Entity, ILocomotive
    {
        public override EntityType Type => EntityType.Locomotive;

        [JsonProperty("protocol")]
        public string Protocol
        {
            get; 
            private set;
        } = string.Empty;
        [JsonProperty("address")] public string Address { get; private set; } = string.Empty;
        [JsonProperty("direction")] public LocomotiveDirection Direction { get; private set; } = LocomotiveDirection.Forward;
        [JsonProperty("maxSpeed")] public int MaxSpeed => LocomotiveUtilities.GetNumberOfSpeedsteps(Protocol);
        [JsonProperty("speedstep")] public int Speedstep { get; private set; } = 0;
        [JsonProperty("functions")] public List<IFuncDescItem> Functions { get; } = new();
        [JsonProperty("doubleTraction")] public bool DoubleTraction { get; set; } = false;
        [JsonProperty("smartSearch")] public bool SmartSearch { get; set; } = false;

        public override JObject ToJsonObject()
        {
            var arFncDesc = new JArray();
            for (var i = 0; i < Functions.Count; ++i)
            {
                //if (Functions[i].FunctionType == 0) continue;

                var odesc = new JObject
                {
                    // normally used by ESU ECoS
                    ["idx"] = i,
                    ["state"] = Functions[i].State,
                    ["type"] = Functions[i].FunctionType,
                    ["moment"] = Functions[i].Moment,

                    // normally used by Z21
                    ["fncIdx"] = Functions[i].FncIdx,
                    ["name"] = Functions[i].Name,
                    ["description"] = Functions[i].Description,
                    ["isUsed"] = Functions[i].IsUsed,
                    ["icon"] = Functions[i].Icon
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
            if (data is PredefinedEntityLocomotive predefinedLocData)
            {
                DisplayName = predefinedLocData.Name;
                Name0 = predefinedLocData.Name;
                ObjectId = predefinedLocData.Address;
                Address = $"{predefinedLocData.Address}";
                Protocol = predefinedLocData.Protocol;

                Functions.Clear();

                foreach (var itFnc in predefinedLocData.Functions)
                {
                    var instance = new FuncDescItem
                    {
                        FunctionIndex = itFnc.Index,
                        FunctionType = 0,
                        Moment = false,
                        State = false,

                        FncIdx = itFnc.FunctionIndex,
                        Name = itFnc.Name,
                        Description = itFnc.Description,
                        IsUsed = itFnc.IsUsed,
                        Icon = itFnc.Icon
                    };

                    Functions.Add(instance);
                }

                return true;
            }

            if (data is LocomotiveInfo locData)
            {
                var changed = false;

                // 
                // ATTENTION: als Default ist DCC14 als Protokoll
                //            hier wird LocomotiveInfo aus den Daten
                //            der z21 gespeist, da steht aber kein
                //            Protokoll drin -- also hier nicht verwenden!
                //
                //if (!Protocol.Equals(locData.Protocol))
                //{
                //    Protocol = locData.Protocol;
                //    changed = true;
                //}

                if (!Address.Equals($"{locData.Address}"))
                {
                    Address = $"{locData.Address}";
                    changed = true;
                }

                var locDirection = locData.Direction
                    ? LocomotiveDirection.Forward
                    : LocomotiveDirection.Backward;
                if (Direction != locDirection)
                {
                    Direction = locDirection;
                    changed = true;
                }

                if (Speedstep != locData.Speedlevel)
                {
                    Speedstep = locData.Speedlevel;
                    changed = true;
                }

                if (SetFunctionsFromStateList(locData.Functions))
                    changed = true;

                if (DoubleTraction != locData.DoubleTraction)
                {
                    DoubleTraction = locData.DoubleTraction;
                    changed = true;
                }

                if (SmartSearch != locData.SmartSearch)
                {
                    SmartSearch = locData.SmartSearch;
                    changed = true;
                }

                return changed;
            }

            return false;
        }

        private bool SetFunctionsFromStateList(List<bool> states)
        {
            var hasChanged = false;

            for (var i = 0; i < states.Count; i++)
            {
                var existing = Functions.FirstOrDefault(f => f.FncIdx == i);

                if (existing != null)
                {
                    if (existing.State != states[i])
                    {
                        existing.State = states[i];
                        hasChanged = true;
                    }
                }
                else
                {
                    Functions.Add(new FuncDescItem
                    {
                        FncIdx = i,
                        FunctionType = 0,
                        Moment = false,
                        State = states[i]
                    });

                    if (states[i]) // Nur als Änderung werten, wenn neue Funktion aktiv ist
                        hasChanged = true;
                }
            }

            return hasChanged;
        }

    }
}

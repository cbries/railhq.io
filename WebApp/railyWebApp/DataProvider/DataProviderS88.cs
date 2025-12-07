// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using libShared.DataProvider;
using libShared.Entities.Impl;
using Newtonsoft.Json.Linq;

namespace railyWebApp.DataProvider
{
    public class DataProviderS88 : libShared.DataProvider.DataProvider, IDataProviderFeedback
    {
        public override DataProviderType Type => DataProviderType.S88Feedback;

        public override string Name => "S88";

        #region IDataProviderFeedback

        public Dictionary<int, S88Entity> Ports { get; } = new();

        #endregion

        public override bool Update(IReadOnlyList<libShared.ExchangeProtocol.Request> requests, bool _)
        {
            foreach (var req in requests)
            {
                if (req.Data.Payload is JObject jsonPayload)
                {
                    var payloadS88 = jsonPayload.ToObject<PayloadS88>();
                    if (payloadS88 == null) continue;

                    var maxPorts = payloadS88.Info.Left + payloadS88.Info.Middle + payloadS88.Info.Right;

                    if (Ports.TryGetValue(payloadS88.Event.Port, out var entity))
                    {
                        entity.MaxPorts = maxPorts;
                        entity.HexState = payloadS88.Event.State.Hex;
                        entity.BinaryState = payloadS88.Event.State.Binary;

                        OnEntityUpdated(entity);
                    }
                    else
                    {
                        var entityS88 = new S88Entity
                        {
                            Port = payloadS88.Event.Port,
                            MaxPorts = maxPorts,
                            HexState = payloadS88.Event.State.Hex,
                            BinaryState = payloadS88.Event.State.Binary
                        };

                        OnEntityUpdated(entityS88);
                    }
                }
            }

            return true;
        }
    }
}

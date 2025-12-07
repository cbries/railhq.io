// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using libShared.DataProvider;
using libShared.Entities.Impl;
using libShared.ExchangeProtocol;
using libUtilities;

namespace railyWebApp.DataProvider
{
    public class DataProviderS88Simulator :
        libShared.DataProvider.DataProvider,
        IDataProviderSimulator,
        IDataProviderFeedback
    {
        public const string DriverName = "S88-Simulator";

        public override string Name => DriverName;
        public override DataProviderType Type => DataProviderType.S88Feedback | DataProviderType.S88FeedbackSimulator;

        #region IDataProviderFeedback

        public Dictionary<int, S88Entity> Ports { get; } = new();

        #endregion

        public DataProviderS88Simulator()
        {
            for (var i = 1; i <= 31; ++i)
            {
                Ports.Add(i, new S88Entity
                {
                    BinaryState = "0000000000000000",
                    HexState = "0000",
                    MaxPorts = 31,
                    DriverName = "S88", // we simulate the original S88 driver, therefor we have to use the same name
                    Pins = 16,
                    Port = i
                });
            }
        }

        #region IDataProviderSimulator

        public void ToggleState(int port, int pin)
        {
            if (port <= 0 || port > 31) return;

            var entity = Ports[port];
            var pinRight = 16 - pin;
            char[] binaryState = entity.BinaryState.ToCharArray();
            var currentState = binaryState[pinRight] == '1';
            if (currentState)
                binaryState[pinRight] = '0';
            else
                binaryState[pinRight] = '1';

            entity.BinaryState = new string(binaryState);

            Logging.Log.Debug($"Simulator: {port}::{pin}  -->  {entity.BinaryState}");

            OnEntityUpdated(entity);
        }

        #endregion

        public override bool Update(IReadOnlyList<Request> requests, bool _)
        {
            // updates are simulated by `ToggleState`

            return false;
        }
    }
}

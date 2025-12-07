// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libShared.DataProvider;
using System.Collections.Generic;

namespace libAutomaticModus.Running
{
    public enum StageStates
    {
        Idle,
        Cruise,
        WaitForSensorEnter,
        SensorEnter,
        WaitForSensorIn,
        SensorIn,
        Stop
    }

    public class StageRoutes : List<StageData>
    {

    }

    public class StageData(IReadOnlyList<IDataProvider> dataProviders)
        : RouteData(dataProviders)
    {
        #region StateMachine

        public new StageStates State { get; set; } = StageStates.Idle;

        public new StageStates NextState()
        {
            switch (State)
            {
                case StageStates.Idle: State = StageStates.Cruise; break;
                case StageStates.Cruise: State = StageStates.WaitForSensorEnter; break;
                case StageStates.WaitForSensorEnter: State = StageStates.SensorEnter; break;
                case StageStates.SensorEnter: State = StageStates.WaitForSensorIn; break;
                case StageStates.WaitForSensorIn: State = StageStates.WaitForSensorIn; break;
                case StageStates.SensorIn: State = StageStates.Stop; break;
                case StageStates.Stop: State = StageStates.Idle; break;
            }

            return State;
        }

        #endregion
    }
}

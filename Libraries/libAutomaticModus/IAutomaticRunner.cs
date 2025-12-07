// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libAutomaticModus.pods;
using libShared;
using System;
using System.Threading.Tasks;

namespace libAutomaticModus
{
    public class ActionTriggeredData
    {
        public IActionData Data { get; set; }
        public IDataExchange DataExchange { get; set; }
    }

    public interface IAutomaticRunner
    {
        event EventHandler<string> StatusUpdated;
        event EventHandler<ActionTriggeredData> ActionTriggered;
        event EventHandler RoutingFinalized;
        event EventHandler StagingFinalized;

        bool IsSimulationMode { get; set; }
        int RunningRoutes { get; }
        DateTime Started { get; }
        DateTime Stopped { get; }
        
        bool IsStarted();
        bool Stop();
        Task<bool> StopForce();
        bool Start(AutomaticData preparedAutomaticData);

        /// <summary>
        /// This call should inform all clients about the recent
        /// started automation and selected routes.
        /// </summary>
        /// <returns></returns>
        Task<bool> Restore();
    }
}

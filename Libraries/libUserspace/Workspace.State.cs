// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;
// ReSharper disable RedundantDefaultMemberInitializer

namespace libUserspace
{
    public partial class Workspace : IWorkspaceState
    {
        private const int DelayAutoModeSeconds = 1;
        private const int DelayNormalSeconds = 2;

        private CancellationTokenSource _cancellationTokenSourceStateUpdate;
        private Task _backgroundTaskStateUpdate;
        public event EventHandler<JObject> StateUpdated;

        public void ResetRecentStateObject()
        {
            _recentStateObject = null;
        }

        public void TriggerStateUpdateToClients(bool forceUpdate = false)
        {
            OnStatusUpdated(GetStateObject(forceUpdate));
        }

        public void StartStateInformer()
        {
            if (_backgroundTaskStateUpdate != null && !_backgroundTaskStateUpdate.IsCompleted) return;
            _cancellationTokenSourceStateUpdate = new CancellationTokenSource();
            _backgroundTaskStateUpdate = Task.Run(() => ReportStatusAsync(_cancellationTokenSourceStateUpdate.Token));
        }

        private void StopStateInformer()
        {
            if (_backgroundTaskStateUpdate == null) return;
            _cancellationTokenSourceStateUpdate.Cancel();
            _backgroundTaskStateUpdate.Wait();
            _cancellationTokenSourceStateUpdate.Dispose();
            _backgroundTaskStateUpdate = null;
            _cancellationTokenSourceStateUpdate = null;
        }

        public class StateObject
        {
            [JsonIgnore] public bool Changed { get; set; } = true;

            [JsonProperty("automaticEnabled")] public bool AutomaticEnabled { get; set; } = false;
            [JsonProperty("simulationEnabled")] public bool SimulationEnabled { get; set; } = false;
            [JsonProperty("runningRoutes")] public int RunningRoutes { get; set; } = 0;
            [JsonProperty("startedDt")] public DateTime StartedDt { get; set; } = new();
            [JsonProperty("stoppedDt")] public DateTime StoppedDt { get; set; } = new();
        }

        private StateObject _recentStateObject;

        private JObject GetStateObject(bool forceUpdate = false)
        {
            if (_recentStateObject == null)
            {
                _recentStateObject = new StateObject
                {
                    AutomaticEnabled = AutomaticEnabled,
                    SimulationEnabled = SimulationEnabled,
                    RunningRoutes = RunningRoutes,
                    StartedDt = Started,
                    StoppedDt = Stopped
                };

                return JObject.FromObject(_recentStateObject);
            }

            _recentStateObject.Changed = false;

            // AutomaticEnabled
            if (_recentStateObject.AutomaticEnabled != AutomaticEnabled)
            {
                _recentStateObject.Changed = true;
                _recentStateObject.AutomaticEnabled = AutomaticEnabled;
            }

            // SimulationEnabled
            if (_recentStateObject.SimulationEnabled != SimulationEnabled)
            {
                _recentStateObject.Changed = true;
                _recentStateObject.SimulationEnabled = SimulationEnabled;
            }

            // RunningRoutes
            if (_recentStateObject.RunningRoutes != RunningRoutes)
            {
                _recentStateObject.Changed = true;
                _recentStateObject.RunningRoutes = RunningRoutes;
            }

            // StartedDt
            if (_recentStateObject.StartedDt != Started)
            {
                _recentStateObject.Changed = true;
                _recentStateObject.StartedDt = Started;
            }

            // StoppedDt
            if (_recentStateObject.StoppedDt != Stopped)
            {
                _recentStateObject.Changed = true;
                _recentStateObject.StoppedDt = Stopped;
            }

            if(forceUpdate)
                return JObject.FromObject(_recentStateObject);

            if (_recentStateObject.Changed)
                return JObject.FromObject(_recentStateObject);

            return null;
        }

        private async Task ReportStatusAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var stateObject = GetStateObject();
                OnStatusUpdated(stateObject);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(DelayInSeconds), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        protected virtual void OnStatusUpdated(JObject state)
        {
            if (state == null) return;
            StateUpdated?.Invoke(this, state);
        }

        private int DelayInSeconds
        {
            get
            {
                if (AutomaticEnabled) return DelayAutoModeSeconds;
                return DelayNormalSeconds;
            }
        }

        #region IWorkspaceState

        public bool AutomaticEnabled => _automaticRunner?.IsStarted() ?? false;
        public bool SimulationEnabled => _automaticRunner?.IsSimulationMode ?? false;
        public int RunningRoutes => _automaticRunner?.RunningRoutes ?? 0;
        public DateTime Started => _automaticRunner?.Started ?? DateTime.MaxValue;
        public DateTime Stopped => _automaticRunner?.Stopped ?? DateTime.MaxValue;

        #endregion
    }
}

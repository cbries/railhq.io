// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebApp.Controller.Helper;
using StackExchange.Redis;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class StatusController : RailhqControllerBase
    {
        private readonly SupabaseHealthCheck _healthCheck;
        private readonly IConnectionMultiplexer _redis;

        public StatusController(
            SupabaseService authService, 
            SupabaseHealthCheck healtCheck,
            IConnectionMultiplexer redis,
            IMemoryCache cache) : base(authService, cache)
        {
            _healthCheck = healtCheck;
            _redis = redis;
        }

        [HttpGet("current/{uid}")]
        public async Task<IActionResult> GetStatus(string uid)
        {
            var status = new Dictionary<string, object>
            {
                ["workspaces"] = CheckWorkspaces(),
                ["controller"] = CheckController(),
                ["api"] = CheckApi(),
                ["database"] = await _healthCheck.CheckDatabaseAsync()
            };

            if (!string.IsNullOrEmpty(uid))
                status["gateway"] = CheckGateway(uid);

            return Ok(status);
        }

        #region Fake State Information

        // always true
        // these three states are currently online/offline
        // when offline, we could not query the state as well :-)

        private bool CheckWorkspaces()
        {
            if (_redis == null) return false;
            return _redis.IsConnected;
        }

        private bool CheckApi() => true;
        private bool CheckController() => true;

        #endregion

        private bool CheckGateway(string uid)
        {
            if (AuthService == null) return false;

            if (string.IsNullOrEmpty(uid)) return false;

            var connections = ConnectionManager.GetConnection(uid);
            var sock = connections?.ControllerSocket;
            if (sock == null) return false;

            return sock.State == WebSocketState.Open;
        }
    }
}

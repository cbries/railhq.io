// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using libUtilities;

namespace railyWebApp
{
    public class ClientConnection
    {
        public WebSocket ControllerSocket { get; set; }

        public ClientConnection()
        {
            Logging.Log.Debug($"*** construct ClientConnection() -> 0x{GetHashCode()}");
        }

        #region Browser Connections

        private readonly ConcurrentDictionary<WebSocket, byte> _browserSockets = new();

        public IReadOnlyList<WebSocket> BrowserSockets => _browserSockets.Keys.ToList().AsReadOnly();

        public void Add(WebSocket socket)
        {
            var r = _browserSockets.TryAdd(socket, 0);
            if (!r)
                Logging.Log.Debug($"Can not add WebSocket instance to client list.");
        }

        public bool Remove(WebSocket socket)
        {
            return _browserSockets.TryRemove(socket, out _);
        }

        #endregion
    }
}

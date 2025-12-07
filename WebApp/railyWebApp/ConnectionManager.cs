// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace railyWebApp
{
    public class ConnectionManager
    {
        private static readonly ConcurrentDictionary<string, ClientConnection> Connections = new();

        public static ClientConnection AddConnection(string uid, WebSocket socket, bool isBrowser)
        {
            if (!Connections.TryGetValue(uid, out var clientConnection))
            {
                clientConnection = new ClientConnection();
                Connections[uid] = clientConnection;
            }
            
            if (isBrowser)
            {
                clientConnection.Add(socket);
            }
            else
            {
                clientConnection.ControllerSocket = socket;
            }

            return clientConnection;
        }

        public static void RemoveSocket(WebSocket socket)
        {
            if (socket == null) return;

            var uid = GetUidOf(socket);
            if (string.IsNullOrEmpty(uid)) return;

            if (!Connections.TryGetValue(uid, out var clientConnection)) return;
            
            if (clientConnection.ControllerSocket == socket)
            {
                clientConnection.ControllerSocket = null;
            }
            else
            {
                clientConnection.Remove(socket);
            }

            if (clientConnection.ControllerSocket == null && clientConnection.BrowserSockets.Count == 0)
            {
                Connections.TryRemove(uid, out _);
            }
        }


        public static ClientConnection GetConnection(string uid)
        {
            Connections.TryGetValue(uid, out var clientConnection);

            return clientConnection;
        }

        public static string GetUidOf(WebSocket socket)
        {
            if (socket == null) return string.Empty;

            var socketHashCode = socket.GetHashCode();

            foreach (var entry in Connections)
            {
                var key = entry.Key;
                var value = entry.Value;

                if (value.ControllerSocket != null
                    && value.ControllerSocket.GetHashCode().Equals(socketHashCode))
                    return key;

                foreach (var c in value.BrowserSockets)
                {
                    if (c.GetHashCode().Equals(socketHashCode))
                        return entry.Key;
                }
            }

            return string.Empty;
        }
    }
}

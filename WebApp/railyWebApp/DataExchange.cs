// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using libShared;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using libMetamodel.Settings;
using railyWebApp.Playground;

namespace railyWebApp;

public class DataExchange : IDataExchange
{
    public async Task SendWs(WebSocket socket, JObject data)
    {
        try
        {
            if (socket.State == WebSocketState.Closed) return;
            if (socket.State == WebSocketState.CloseReceived) return;
            if (socket.State == WebSocketState.Aborted) return;
            if (socket.State == WebSocketState.CloseSent) return;

            var json = JsonConvert.SerializeObject(data);
            var jsonBuffer = Encoding.UTF8.GetBytes(json);
            if (jsonBuffer.Length <= 2) return;
            await socket.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logging.ExceptionLog(ex);
        }
    }

    public async Task SendWs(WebSocket socket, string json)
    {
        try
        {
            if (socket.State == WebSocketState.Closed) return;
            if (socket.State == WebSocketState.CloseReceived) return;
            if (socket.State == WebSocketState.Aborted) return;
            if (socket.State == WebSocketState.CloseSent) return;

            var jsonBuffer = Encoding.UTF8.GetBytes(json);
            if (json.Length <= 2) return;
            await socket.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Logging.ExceptionLog(ex);
        }
    }

    public async Task SendWarning(WebSocket socket, string warningMessage)
    {
        if (socket == null) return;
        if (string.IsNullOrEmpty(warningMessage)) return;

        Logging.Log.Debug(warningMessage);
        await SendWs(socket, new JObject
        {
            ["command"] = "warning",
            ["info"] = new JObject
            {
                {"message", warningMessage}
            }
        });
    }

    public async Task SendFatal(WebSocket socket,
        int fatalMessageCode,
        string fatalMessage,
        string reason,
        bool closeConnection = false)
    {
        if (socket == null) return;
        if (string.IsNullOrEmpty(fatalMessage)) return;

        Logging.Log.Debug(fatalMessage);
        await SendWs(socket, new JObject
        {
            ["command"] = "fatal",
            ["info"] = new JObject
            {
                {"code", fatalMessageCode},
                {"message", fatalMessage}
            }
        });

        if (closeConnection)
            await CloseClient(socket, reason);
    }

    public async Task SendObjectToAllClients(string uid, JObject obj)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Logging.Log.Warn($"Send of command failed because no authentication token is given.");
            return;
        }
        var clients = ConnectionManager.GetConnection(uid);
        try
        {
            if (clients is { BrowserSockets: not null })
            {
                var wsBrowsers = clients.BrowserSockets;

                foreach (var ws in wsBrowsers)
                {
                    try
                    {
                        if (ws != null)
                            await WebSocketModule.DataExchange.SendWs(ws, obj);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    public async Task SendObjectToAllClients(string uid, JObject obj, List<WebSocket> wsToIgnore)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Logging.Log.Warn($"Send of command failed because no authentication token is given.");
            return;
        }
        var clients = ConnectionManager.GetConnection(uid);
        try
        {
            if (clients is { BrowserSockets: not null })
            {
                var wsBrowsers = clients.BrowserSockets;

                foreach (var ws in wsBrowsers)
                {
                    if (wsToIgnore.Contains(ws)) continue;

                    try
                    {
                        if (ws != null)
                            await WebSocketModule.DataExchange.SendWs(ws, obj);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }

    public async Task SendSettingsToClients(string uid)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Logging.Log.Warn($"Send of command failed because no authentication token is given.");
            return;
        }

        // send setting update to all connected webClients
        var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
        if (wsres)
        {
            var data0 = new JObject
            {
                ["command"] = "update",
                ["settings"] = JObject.FromObject(userWorkspace.Metamodel.Settings)
            };

            await SendObjectToAllClients(uid, data0);
        }
    }

    public void QueueDebugMessage(string uid, string message, DebugMessageT msgType = DebugMessageT.Runtime)
    {
        if (string.IsNullOrEmpty(message)) return;

        var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
        if (wsres)
        {
            var debuggingSettings = userWorkspace.Metamodel?.Settings?.Debugging;
            if (debuggingSettings == null) debuggingSettings = new Debugging();
            
            switch (msgType)
            {
                case DebugMessageT.Runtime when debuggingSettings.Runtime:
                    {
                        _ = PgHelper.SendDebugToClient(uid, message);
                    }
                    break;

                case DebugMessageT.Accessories when debuggingSettings.Accessories:
                    {
                        _ = PgHelper.SendDebugToClient(uid, message);
                    }
                    break;

                case DebugMessageT.Locomotives when debuggingSettings.Locomotives:
                    {
                        _ = PgHelper.SendDebugToClient(uid, message);
                    }
                    break;

                case DebugMessageT.Routes when debuggingSettings.Routes:
                    {
                        _ = PgHelper.SendDebugToClient(uid, message);
                    }
                    break;

                case DebugMessageT.Exceptions when debuggingSettings.Exceptions:
                    {
                        _ = PgHelper.SendDebugToClient(uid, message);
                    }
                    break;
            }
        }
    }

    public async Task CloseClient(WebSocket socket, string reason)
    {
        if (socket == null) return;
        if (socket.State == WebSocketState.Closed) return;
        if (socket.State == WebSocketState.CloseSent) return;
        if (socket.State == WebSocketState.CloseReceived) return;
        if (socket.State == WebSocketState.CloseSent) return;

        try
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
        }
        catch
        {
            // ignore
        }
    }

    public async Task CloseClients(string uid, string reason, List<WebSocket> wsToIgnore)
    {
        if (string.IsNullOrEmpty(uid))
        {
            Logging.Log.Warn($"Close failed because no authentication token is given.");
            return;
        }
        var clients = ConnectionManager.GetConnection(uid);
        try
        {
            if (clients is { BrowserSockets: not null })
            {
                var wsBrowsers = clients.BrowserSockets;

                foreach (var ws in wsBrowsers)
                {
                    if (wsToIgnore.Contains(ws)) continue;

                    try
                    {
                        if (ws != null)
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, reason, CancellationToken.None);
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }
        }
        catch
        {
            // ignore
        }
    }
}
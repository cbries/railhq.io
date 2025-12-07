// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using libShared.ExchangeProtocol;
using libUtilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace railyWebApp.Playground
{
    public enum DebugInfoLevel
    {
        None,
        Info,
        Warn,
        Error,
        Fatal
        
    }

    public class PgHelper
    {
        internal static bool HasCommand(Request request, out string command)
        {
            command = string.Empty;
            if (request.Data.Payload is not JObject payloadData) return false;
            command = payloadData.GetString("command");
            if (!string.IsNullOrEmpty(command)) return true;
            var extensionName = request.ExtensionName;
            Logging.Log.Debug($"Missing command in request from extension: {extensionName}");
            return false;
        }

        internal static bool IsPayloadCommand(JObject cmddata, string commandName)
        {
            if (string.IsNullOrEmpty(commandName)) return false;
            if (!cmddata.ContainsKey("command")) return false;
            return cmddata.GetString("command").Equals(commandName);
        }

        internal static bool IsArgument(JObject cmddata, string argumentName)
        {
            if (cmddata == null) return false;
            if (string.IsNullOrEmpty(argumentName)) return false;
            return cmddata.GetString("argument").Equals(argumentName);
        }

        internal static bool IsArgument(JObject cmddata, string argumentName, string argumentValue)
        {
            if (cmddata == null) return false;
            if (string.IsNullOrEmpty(argumentName)) return false;
            if (string.IsNullOrEmpty(argumentValue)) return false;
            return cmddata.GetString("argument").Equals(argumentName)
                   && cmddata.GetString("argumentValue").Equals(argumentValue);
        }

        internal static async Task<bool> SendToGateway(string uid, JObject cmdToGateway)
        {
            try
            {
                var clientConnections = ConnectionManager.GetConnection(uid);
                var wsGateway = clientConnections?.ControllerSocket;
                if (wsGateway == null || wsGateway.State != WebSocketState.Open)
                {
                    Logging.Log.Debug($"No connection to `railhq.io - Gateway`. Command not sent.");
                    return false;
                }

                var json = JsonConvert.SerializeObject(cmdToGateway);
                var jsonBuffer = Encoding.UTF8.GetBytes(json);
                await wsGateway.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true, CancellationToken.None);

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        internal static async Task<bool> SendDebugToClient(string uid, string message, DebugInfoLevel level = DebugInfoLevel.Info)
        {
            try
            {
                if (string.IsNullOrEmpty(uid)) return false;
                if (string.IsNullOrEmpty(message)) return false;

                var clientConnections = ConnectionManager.GetConnection(uid);
                foreach (var ws in clientConnections.BrowserSockets)
                {
                    var instance = new DebugMessage
                    {
                        Priority = level.ToString(),
                        Messages = [message.Trim()]
                    };

                    //var debugMessage = new JObject
                    //{
                    //    ["command"] = "debugMessages",
                    //    ["datetime"] = DateTime.Now,
                    //    ["priority"] = level.ToString(),
                    //    ["messages"] = new JArray
                    //    {
                    //        message.Trim()
                    //    }
                    //};

                    await SendToClient(ws, JsonConvert.SerializeObject(instance, Formatting.None));
                }

                return true;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return false;
        }

        internal static async Task SendToClient(WebSocket wsClient, string jsonMessage)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonMessage)) return;
                if (wsClient == null) return;
                if (wsClient.State != WebSocketState.Open) return;

                byte[] jsonBuffer = Encoding.UTF8.GetBytes(jsonMessage);
                await wsClient.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }
    }
}

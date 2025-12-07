// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace libShared
{
    public interface IDataExchange
    {
        Task SendWs(WebSocket socket, JObject data);
        Task SendWs(WebSocket socket, string data);
        Task SendWarning(WebSocket socket, string warningMessage);
        Task SendFatal(WebSocket socket, int fatalMessageCode, string fatalMessage, string reason, bool closeConnection = false);
        Task SendObjectToAllClients(string uid, JObject obj);
        Task SendObjectToAllClients(string uid, JObject obj, List<WebSocket> wsToIgnore);
        Task SendSettingsToClients(string uid);
        Task CloseClient(WebSocket socket, string reason);
        Task CloseClients(string uid, string reason, List<WebSocket> wsToIgnore);

        void QueueDebugMessage(string uid, string message, DebugMessageT msgType = DebugMessageT.Runtime);
    }

    public enum DebugMessageT
    {
        Runtime = 1,
        Accessories = 2,
        Locomotives = 4,
        Routes = 8,
        Exceptions = 16
    }
}

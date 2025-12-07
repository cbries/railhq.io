// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libInterop;
using libUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyGateway.Cfg;
using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace railyGateway.WebApp
{
    public class WebSocketModule
    {
        public static string WsUri = "/ws";
        private static ICfgService _cfgService;

        public static async Task HandleWsRequest(
            HttpContext context,
            Func<ICfgService, Task> callbackConfigChanged,
            Func<JObject> callbackQueryState)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                _cfgService = context.RequestServices.GetRequiredService<ICfgService>();

                var socket = await context.WebSockets.AcceptWebSocketAsync();
                Logging.Log.Debug($"New connection: {context.Connection.RemoteIpAddress}");

                await SendConfig(socket);
                await HandleWebSocketAsync(socket, callbackConfigChanged, callbackQueryState);
            }
            else
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
            }
        }

        private static async Task HandleWebSocketAsync(
            WebSocket socket,
            Func<ICfgService, Task> callbackConfigChanged,
            Func<JObject> callbackQueryState)
        {
            const int noOfMegabytes = 1;
            var buffer = new byte[noOfMegabytes * 1024 * 1024];

            var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            if (result.MessageType == WebSocketMessageType.Text)
            { }

            do
            {
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    break;
                }

                if (result.MessageType != WebSocketMessageType.Text) continue;

                var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var jsonObject = JObject.Parse(message);

                // 
                // when `command` is set a special handling is
                // needed and will be executed, in any
                // other case we will handle the data as cfg info
                //
                if (jsonObject["command"] != null)
                {
                    var cmd = jsonObject.GetString("command");
                 
                    //
                    // query current state and provide to configuration page
                    //
                    if (cmd.Equals("state", StringComparison.OrdinalIgnoreCase))
                    {
                        var stateObj = callbackQueryState?.Invoke();
                        if (stateObj != null)
                        {
                            await SendWs(socket, stateObj);
                        }
                    }
                    //
                    // validate the entered authentication credentials
                    //
                    else if (cmd.Equals("validateAuth", StringComparison.OrdinalIgnoreCase))
                    {
                        var username = jsonObject.GetString("username");
                        var password = jsonObject.GetString("password");
                        var host = jsonObject.GetString("host");
                        var port = jsonObject.GetInt("port");

                        var url = $"https://{host}:{port}/api/Auth";

                        try
                        {
                            var data = new JObject
                            {
                                {"username", username},
                                {"password", password}
                            }.ToString(Formatting.None);
                            var content = new StringContent(data, Encoding.UTF8, "application/json");

                            var httpClient = new HttpClient();
                            var response = await httpClient.PostAsync(url, content);
                            var responseString = await response.Content.ReadAsStringAsync();
                            var dataResponse = JObject.Parse(responseString);
                            dataResponse["command"] = "validateAuth";
                            await SendWs(socket, dataResponse);
                        }
                        catch
                        {
                            // ignore
                        }
                    }
                    //
                    //
                    //
                    else if (cmd.Equals("triggerEcosConnect", StringComparison.OrdinalIgnoreCase))
                    {
                        var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
                        IRailyExtension extEcos = null;
                        foreach (var ext in extensions)
                        {
                            if (ext is railyEsuEcos.Plugin plugin)
                                extEcos = plugin;
                            if (extEcos != null) break;
                        }

                        extEcos?.TryOpen();
                    }

                    //
                    //
                    //
                    else if (cmd.Equals("triggerZ21Connect", StringComparison.OrdinalIgnoreCase))
                    {
                        var extensions = Services.ServiceProvider.GetServices<IRailyExtension>();
                        IRailyExtension extZ21 = null;
                        foreach (var ext in extensions)
                        {
                            if (ext is railhqZ21.Plugin plugin)
                                extZ21 = plugin;
                            if (extZ21 != null) break;
                        }

                        extZ21?.TryOpen();
                    }
                }
                else
                {
                    //
                    // no special command, we received the changed configuration
                    //
                    await SaveNewConfiguration(jsonObject, _cfgService);
                    await SendConfig(socket);
                    callbackConfigChanged?.Invoke(_cfgService);
                }

                result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

            } while (!result.CloseStatus.HasValue);

            if (socket.State != WebSocketState.Closed && result.CloseStatus.HasValue)
                await socket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }

        private static async Task SendConfig(WebSocket socket)
        {
            var cfgCnt = _cfgService.GetCfgContent();
            var jsonObject = JObject.Parse(cfgCnt);

            var c = jsonObject["connection"];
            var connectionEnabled = c.GetBool("isEnabled");
            var connectionTimeoutSeconds = c.GetInt("timeoutSeconds");
            var connectionHost = c.GetString("host");
            var connectionPort = c.GetInt("port");

            var e = jsonObject["ecos"];
            var z21 = jsonObject["z21"];
            var h = jsonObject["hsi"];
            var hd = h?["debounce"];

            var cfgObject = new JObject
            {
                {"auth", new JObject
                {
                    {"username", c.GetString("username")},
                    {"password", c.GetString("password")}
                }},
                {"server", new JObject
                {
                    {"enabled", connectionEnabled},
                    {"timeoutSeconds", connectionTimeoutSeconds},
                    {"host", connectionHost},
                    {"port", connectionPort}
                }},
                {"ecos", new JObject
                {
                    {"enabled", e.GetBool("isEnabled")},
                    {"host", e.GetString("ip")},
                    {"port", e.GetInt("port")}
                }},
                {"z21", new JObject
                {
                    {"enabled", z21.GetBool("isEnabled")},
                    {"host", z21.GetString("ip")},
                    {"port", z21.GetInt("port")}
                }},
                {"hsi", new JObject
                {
                    {"enabled", h.GetBool("isEnabled")},
                    {"left", h.GetInt("left")},
                    {"middle", h.GetInt("middle")},
                    {"right", h.GetInt("right")},
                    {"devicePath", h.GetString("devicePath")},
                    {"checkIntervalMs", hd.GetInt("checkIntervalMs")},
                    {"onMs", hd.GetInt("onMs")},
                    {"offMs", hd.GetInt("offMs")}
                }}
            };

            await SendWs(socket, new JObject
            {
                { "command", "update" },
                { "message", cfgObject }
            });
        }

        // TODO
        // TODO the save and restore mechanism for the configuration must
        // be more dynamic; any change should be applied directly
        // the configuration website should provide information
        // about the current state and should inform the customer
        // if the changes works or not
        internal static async Task SaveNewConfiguration(JObject obj, ICfgService cfgService)
        {
            try
            {
                var originalCnt = cfgService.GetCfgContent();
                var originalObj = JObject.Parse(originalCnt);

                var c = originalObj["connection"];

                if (obj["auth"] is JObject authObj)
                {
                    if (c != null)
                    {
                        c["username"] = authObj.GetString("username");
                        c["password"] = authObj.GetString("password");
                    }
                }

                if (obj["server"] is JObject serverObj)
                {
                    if (c != null)
                    {
                        c["host"] = $"{serverObj.GetString("host")}";
                        c["port"] = serverObj.GetInt("port");
                        c["timeoutSeconds"] = serverObj.GetInt("timeoutSeconds");
                        c["isEnabled"] = serverObj.GetBool("enabled");
                    }
                }

                if (obj["ecos"] is JObject ecosObj)
                {
                    var e = originalObj["ecos"];
                    if (e != null)
                    {
                        e["isEnabled"] = ecosObj.GetBool("enabled");
                        e["ip"] = ecosObj.GetString("host");
                        e["port"] = ecosObj.GetInt("port");
                    }
                }

                if (obj["z21"] is JObject z21Obj)
                {
                    var e = originalObj["ecos"];
                    if (e != null)
                    {
                        e["isEnabled"] = z21Obj.GetBool("enabled");
                        e["ip"] = z21Obj.GetString("host");
                        e["port"] = z21Obj.GetInt("port");
                    }
                }

                if (obj["hsi"] is JObject hsiObj)
                {
                    var h = originalObj["hsi"];
                    if (h != null)
                    {
                        h["isEnabled"] = hsiObj.GetBool("enabled");
                        h["left"] = hsiObj.GetInt("left");
                        h["middle"] = hsiObj.GetInt("middle");
                        h["right"] = hsiObj.GetInt("right");
                        h["devicePath"] = hsiObj.GetString("devicePath");
                        var hd = h["debounce"];
                        if (hd != null)
                        {
                            hd["checkIntervalMs"] = hsiObj.GetInt("checkIntervalMs");
                            hd["onMs"] = hsiObj.GetInt("onMs");
                            hd["offMs"] = hsiObj.GetInt("offMs");
                        }
                    }
                }

                var json = originalObj.ToString(Formatting.Indented);
                await System.IO.File.WriteAllTextAsync(cfgService.ConfigPath, json, Encoding.UTF8);
                cfgService.RefreshFromHarddisk();
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        internal static async Task SendWs(WebSocket socket, JObject data)
        {
            try
            {
                var json = JsonConvert.SerializeObject(data);
                var jsonBuffer = Encoding.UTF8.GetBytes(json);
                await socket.SendAsync(new ArraySegment<byte>(jsonBuffer), WebSocketMessageType.Text, true,
                    CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }
    }
}

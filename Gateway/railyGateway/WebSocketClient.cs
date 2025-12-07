// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using libUtilities;
using railyGateway.Cfg;
using WebSocket4Net;
using ErrorEventArgs = SuperSocket.ClientEngine.ErrorEventArgs;

// ReSharper disable InconsistentNaming

namespace railyGateway
{
    public class ConnectFailedEventArgs(string message) : EventArgs
    {
        public string Message { get; } = message;
    }

    internal class WebSocketClient : IWebSocketClient
    {
        private readonly WebSocket _webSocket;
        private readonly ICfgService _cfgService;
        public ICfgService CfgService => _cfgService;

        public event EventHandler<string> MessageReceived;
        public event EventHandler ConnectionEstablished;
        public event EventHandler ConnectionClosed;
        public event EventHandler<ConnectFailedEventArgs> ConnectFailed;

        private readonly string _serverUri;

        public string ServerUri => _serverUri;

        public bool IsConnected
        {
            get
            {
                if (_webSocket == null) return false;
                if (_webSocket.State == WebSocketState.Open) return true;
                return false;
            }
        }

        public WebSocketClient(ICfgService cfgService)
        {
            _cfgService = cfgService;
            
            var cfgInstance = _cfgService.GetConfig();

            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;

            _serverUri = $"{Globals.WsProtocol}://{cfgInstance.Connection.Host}:{cfgInstance.Connection.Port}/ws/controller";
            _webSocket = new WebSocket(_serverUri);
            _webSocket.MessageReceived += (_, e) =>
            {
                OnMessageReceived(e.Message);
            };
            _webSocket.Opened += (_, _) =>
            {
                _webSocket.Closed += WebSocketOnClosed;

                OnOpened();
            };
        }

        private void WebSocketOnClosed(object sender, EventArgs e)
        {
            _webSocket.Closed -= WebSocketOnClosed;

            var eventArgs = e as ClosedEventArgs;
            var reason = eventArgs?.Reason;

            var targetHost = _cfgService.GetConfig().Connection.Host;
            var resPing = NetworkInfo.IsPingOk(targetHost, true);
            if (!resPing) reason = $"Ping to {targetHost} failed.";
            if (string.IsNullOrEmpty(reason)) reason = "unknown";

            Logging.Log.Info($"Connect to Controller failed: {reason}");
            OnClosed();
        }

        #region IWebSocketClient

        public async Task ConnectAsync(TimeSpan timeout)
        {
            //if (IsClosing()) return;

            if (IsConnected) return;

            using var cts = new CancellationTokenSource();
            cts.CancelAfter(timeout);

            var startDt = DateTime.Now;
            var walltimeDt = startDt + timeout;

            while (!cts.Token.IsCancellationRequested)
            {
                try
                {
                    if (_webSocket.State == WebSocketState.Connecting)
                    {
                        await Task.Delay(250, cts.Token);
                        continue;
                    }

                    if (_webSocket.State == WebSocketState.Open)
                        break;

                    __localCleanHandler();

                    _webSocket.Opened += __localWebSocketOnOpened;
                    _webSocket.Error += __localWebSocketOnError;

                    #region local methods

                    void __localCleanHandler()
                    {
                        _webSocket.Opened -= __localWebSocketOnOpened;
                        _webSocket.Error -= __localWebSocketOnError;
                    }

                    void __localWebSocketOnOpened(object sender, EventArgs e)
                    {
                        __localCleanHandler();

                        // TODO
                    }

                    void __localWebSocketOnError(object sender, ErrorEventArgs e)
                    {
                        __localCleanHandler();
                        
                        //Logging.Log.Info($"{e.Exception.Message}");
                    }

                    #endregion

                    if (_webSocket.State == WebSocketState.Open)
                        break;

                    _webSocket.Open();

                    for (var i = 0; i < 5; ++i)
                    {
                        await Task.Delay(500, cts.Token);

                        if (_webSocket.State == WebSocketState.Open)
                        {
                            break;
                        }
                    }
                }
                catch (TaskCanceledException ex)
                {
                    if (DateTime.Now > walltimeDt)
                    {
                        var m = $"Connection timeout after {timeout.TotalSeconds} seconds: {ex.GetExceptionMessages()}";
                        Logging.Log.Info(m);
                        ConnectFailed?.Invoke(this, new ConnectFailedEventArgs(m));
                    }
                }
                catch (Exception ex)
                {
                    var exMsg = $"Connect to Controller failed: {ex.GetExceptionMessages()}";
                    Logging.Log.Debug(exMsg);
                    ConnectFailed?.Invoke(this, new ConnectFailedEventArgs(exMsg));
                }

                try
                {
                    if (!cts.Token.IsCancellationRequested)
                        await Task.Delay(TimeSpan.FromSeconds(1), cts.Token);
                }
                catch
                {
                    // ignore
                }
            }
        }

        public void Connect()
        {
            if (IsClosing()) return;

            _webSocket.Open();
        }

        public void Close()
        {
            if (IsClosing()) return;

            _webSocket.Close();
        }

        public void SendMessage(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            if (_webSocket.State != WebSocketState.Open)
            {
                Logging.Log.Debug($"SendMessage called to early, not connected to {_serverUri}.");
                
                return;
            }

            _webSocket.Send(message);
        }

        #endregion

        private bool IsClosing()
        {
            if (_webSocket == null) return true;
            if (_webSocket.State == WebSocketState.Closing) return true;
            if (_webSocket.State == WebSocketState.Closed) return true;
            return false;
        }

        protected virtual void OnMessageReceived(string message)
        {
            MessageReceived?.Invoke(this, message);
        }

        protected virtual void OnOpened()
        {
            ConnectionEstablished?.Invoke(this, EventArgs.Empty);
        }

        protected virtual void OnClosed()
        {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}

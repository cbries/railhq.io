// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Sockets;

namespace libZ21
{
    public delegate void Received(object sender, byte[] data);
    public delegate void Connected(object sender);
    public delegate void StatusUpdated(object sender, bool state, bool init);

    public interface IZ21Connection
    {
        string Z21Ip { get; }
        ushort Z21Port { get; }
        bool Connected { get; }

        UdpClient Client { get; }

        void Connect();
        void Disconnect();

        void SendCommand(byte[] data, int size);
    }

    public class Z21Connection : IZ21Connection
    {
        public event Received OnReceived;
        public event Connected OnConnected;
        public event StatusUpdated OnStatusUpdated;

        public const string DefaultHost = "192.168.0.111";
        public const ushort Port = 21105;

        #region IZ21Connection

        public UdpClient Client { get; private set; } = new();

        public string Z21Ip { get; set; } = DefaultHost;

        public ushort Z21Port { get; set; } = Port;

        public bool Connected { get; private set; }

        public void Connect()
        {
            if(Client == null) Client = new UdpClient();
            var z21Adr = new IPEndPoint(IPAddress.Parse(Z21Ip), Z21Port);
            Client.Connect(z21Adr);
            OnConnected?.Invoke(this);
            Client.BeginReceive(DataReceived, null);
            byte[] sendBytes = [0x04, 0x00, 0x10, 0x00];
            Client.Send(sendBytes, 4);
        }

        public void Disconnect()
        {
            LogOff();
            Client.Dispose();
            Connected = false;
            OnStatusUpdated?.Invoke(this, false, false);
        }

        public void SendCommand(byte[] data, int size)
        {
            if (!Connected) return;
            Client.Send(data, size);
        }

        #endregion

        private void DataReceived(IAsyncResult ar)
        {
            var ip = new IPEndPoint(IPAddress.Parse(Z21Ip), Z21Port);
            byte[] data;

            try
            {
                data = Client.EndReceive(ar, ref ip);

                if (data.Length == 0)
                {
                    Connected = false;
                    OnStatusUpdated?.Invoke(this, false, false);
                    return; // No more to receive
                }
                if (Connected == false) OnStatusUpdated?.Invoke(this, true, false);
                Connected = true;
                Client.BeginReceive(DataReceived, null);
            }
            catch //(ObjectDisposedException)
            {
                Connected = false;
                OnStatusUpdated?.Invoke(this, false, false);
                return;
            }

            OnReceived?.Invoke(this, data);
        }
        
        private void LogOff()
        {
            byte[] sendBytes = [0x04, 0x00, 0x30, 0x00];
            SendCommand(sendBytes, 4);
        }
    }
}

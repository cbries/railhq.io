// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace libUtilities
{
    public class NetworkInfo
    {
        /// <summary>
        /// Checks if the specified host is reachable via ICMP (Ping).
        /// </summary>
        /// <param name="host">The address of the host (IP address or domain name) to be checked.</param>
        /// <param name="defreturn"></param>
        /// <returns>Returns true if the host is reachable, otherwise false. When an internal exception raises `defreturn` is returned as default.</returns>
        public static bool IsPingOk(string host, bool defreturn = false)
        {
            try
            {
                var pingSender = new Ping();
                var options = new PingOptions
                {
                    DontFragment = true
                };
                const int timeout = 120;
                const string data = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";
                var buffer = Encoding.ASCII.GetBytes(data);
                if (string.IsNullOrEmpty(host)) return false;
                var reply = pingSender.Send(host, timeout, buffer, options);

                return (reply is { Status: IPStatus.Success });
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return defreturn;
        }

        /// <summary>
        /// Retrieves a list of local IP addresses that successfully respond to a ping.
        /// </summary>
        /// <returns>A list of IP addresses that successfully responded to a ping.</returns>
        public static List<string> GetLocalIpAddressesWithSuccessfulPing()
        {
            var successfulPings = new List<string>();

            foreach (NetworkInterface netInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (netInterface.OperationalStatus == OperationalStatus.Up)
                {
                    var ipProperties = netInterface.GetIPProperties();

                    if (ipProperties.GatewayAddresses.Any()) // Check if a default gateway is set
                    {
                        foreach (UnicastIPAddressInformation ip in ipProperties.UnicastAddresses)
                        {
                            if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            {
                                var ipAddress = ip.Address.ToString();

                                if (PingInternetAddress())
                                {
                                    successfulPings.Add(ipAddress);
                                }
                            }
                        }
                    }
                }
            }

            return successfulPings;
        }

        /// <summary>
        /// Pings an internet address (Google's DNS server 8.8.8.8) to check if the internet connection is working.
        /// </summary>
        /// <returns>Returns true if the ping to the internet address is successful, otherwise false.</returns>
        public static bool PingInternetAddress()
        {
            try
            {
                using var ping = new Ping();
                var reply = ping.Send("8.8.8.8", 1000, new byte[32], new PingOptions(64, true));
                if (reply == null) return false;
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }

        public static string GetLocalIpForInternetAccess()
        {
            try
            {
                using var udpClient = new System.Net.Sockets.UdpClient();
                // Zieladresse = externer DNS-Server (Google)
                udpClient.Connect("8.8.8.8", 53);
                var localEndPoint = (System.Net.IPEndPoint)udpClient.Client.LocalEndPoint!;
                return localEndPoint.Address.ToString();
            }
            catch
            {
                return "0.0.0.0"; // oder optional null
            }
        }
    }
}

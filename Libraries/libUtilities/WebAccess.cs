// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.Linq;
using System.Net;

namespace libUtilities
{
    public class WebAccess
    {
        public const string DockerIpRange0 = "172.26.0.0/16";
        public const string DockerIpRange1 = "172.27.0.0/16";
        public const string HomeIpRange = "192.168.178.0/24";
        public static List<string> NetcupServer = ["152.53.238.106"];

        private static bool IsIpInRange(string ipAddress, string cidr)
        {
            var ip = IPAddress.Parse(ipAddress);
            var network = IPNetwork.Parse(cidr);

            return network.Contains(ip);
        }

        public static bool IsAllowed(HttpContext ctx)
        {
            //
            // mit dem Start von nginx als Revers-Proxy brauchen wir diese Prüfung nicht mehr
            // der Host ist sowieso nur aus dem lokalen Dockernetz zu erreichen
            //
            return true;

            // var allowedHosts = new[]
            // {
            //     "railhq.io",
            //     "railhq.io-admin",
            //     "railhq.io-index",
            //     "railhq.io-app",
            //     "admin",
            //     "index",
            //     "app"
            // };
            // var requestHost = ctx.Request.Host.Host;
            // var ok0 = allowedHosts.Contains(requestHost);
            // var ok1 = true;

            //#if RELEASE
            //            ok1 = false;

            //            try
            //            {
            //                var ipAdress = ctx.Connection.RemoteIpAddress;
            //                if (ipAdress != null)
            //                {
            //                    if (ipAdress.IsIPv4MappedToIPv6)
            //                        ipAdress = ipAdress.MapToIPv4();
            //                }

            //                var ip = ipAdress?.ToString();
            //                if (!string.IsNullOrEmpty(ip))
            //                {
            //                    if (NetcupServer.Contains(ip)) ok1 = true;
            //                    else if (IsIpInRange(ip, DockerIpRange0)) ok1 = true;
            //                    else if (IsIpInRange(ip, DockerIpRange1)) ok1 = true;
            //                    else if (IsIpInRange(ip, HomeIpRange)) ok1 = true;
            //                }
            //            }
            //            catch
            //            {
            //                ok1 = false;
            //            }
            //#endif

            // return ok0 && ok1;
        }
    }
}

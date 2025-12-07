// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace railyWebApp.Cfg
{
    public class Cfg : ICfg
    {
        public IHost Host { get; set; } = new CfgHost();
    }

    public class CfgHost : IHost
    {
        public bool IsTls { get; set; }
        public int ListenPort { get; set; }
        public string ListenDevice { get; set; }
        public string RemoteHost { get; set; }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using Newtonsoft.Json;
using System.Text;
using libShared;
using libUtilities;

// ReSharper disable ConvertConstructorToMemberInitializers

namespace railyGateway.Cfg
{
    public interface ICfgService
    {
        string ConfigPath { get; }
        void RefreshFromHarddisk();
        Cfg GetConfig();
        string GetCfgContent();
    }

    public class CfgService : ICfgService
    {
        private Cfg _cfgInstance;

        public string ConfigPath { get; }

        public CfgService()
        {
            ConfigPath = RailEnvironment.GetConfigDirPath();
            Logging.Log.Info($"Load configuration: {ConfigPath}");
        }

        public void RefreshFromHarddisk()
        {
            var cfgCnt = GetCfgContent();
            var cfgObject = JsonConvert.DeserializeObject<Cfg>(cfgCnt);
            _cfgInstance = cfgObject;
        }

        public Cfg GetConfig()
        {
            if (_cfgInstance != null) return _cfgInstance;
            var cfgCnt = GetCfgContent();
            var cfgObject = JsonConvert.DeserializeObject<Cfg>(cfgCnt);
            _cfgInstance = cfgObject;
            return _cfgInstance;
        }

        public string GetCfgContent()
        {
            if (!File.Exists(ConfigPath))
                throw new Exception($"Missing file: {ConfigPath}");
            var cnt = File.ReadAllText(ConfigPath, Encoding.UTF8);
            if (string.IsNullOrEmpty(cnt))
                throw new Exception($"Configuration is empty ({ConfigPath}).");
            return cnt;
        }

    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Globalization;
using System.Resources;

namespace railySystray
{
    public class SystrayRm
    {
        public string DefaultLanguage { get; } = "en";

        private readonly ResourceManager _rm;

        public SystrayRm(string language)
        {
            if (!string.IsNullOrEmpty(language))
                if (language.Trim().Equals(DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                    language = string.Empty;
            if (!string.IsNullOrEmpty(language))
                CultureInfo.DefaultThreadCurrentUICulture = new CultureInfo(language);
            _rm = new ResourceManager("railySystray.Resources.Resources", typeof(Program).Assembly);
        }

        public string Get(string name)
        {
            try
            {
                return _rm.GetString(name);
            }
            catch
            {
                // ignore
            }

            return "<invalid>";
        }
    }
}

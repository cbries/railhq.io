// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using libShared.ExchangeProtocol;
// ReSharper disable RedundantNameQualifier

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class AutomationRequest : libShared.ExchangeProtocol.Request
    {
        public AutomationRequest()
        {
            Version = "0.1";
            ExtensionName = "automation";
            Timestamp = DateTime.Now;
            Data = new Data
            {
                Type = "command"
            };
        }
    }
}

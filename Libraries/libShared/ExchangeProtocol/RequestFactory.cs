// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libShared.ExchangeProtocol
{
    public static class RequestFactory
    {
        public static Request CreateRequest(
            string extensionName, 
            object payload, 
            string type = "command")
        {
            return new Request
            {
                Version = "0.1",
                ExtensionName = extensionName,
                Timestamp = DateTime.UtcNow,
                Data = new Data
                {
                    Type = type,
                    Payload = payload
                }
            };
        }
    }
}

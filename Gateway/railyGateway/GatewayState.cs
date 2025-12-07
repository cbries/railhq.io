// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Reflection.Metadata.Ecma335;

namespace railyGateway
{
    public class GatewayState
    {
        public const int HTTP_401_UNAUTHORIZED = 401;
        public const int HTTP_403_FORBIDDEN = 403;

        public bool IsConnected { get; set; } = false;
        public bool IsAuthenticated { get; set; } = false;
        public string AuthenticateState { get; set; } = string.Empty;
        public string AuthenticateToken { get; set; } = string.Empty;

        public int RecentErrorCode { get; set; } = -1;

        public void Reset()
        {
            IsConnected = false;
            IsAuthenticated = false;
            AuthenticateState = string.Empty;
            AuthenticateToken = string.Empty;
        }

        public bool IsOk() => IsConnected && IsAuthenticated;

        public bool IsAuthIssue() => RecentErrorCode == HTTP_401_UNAUTHORIZED;
    }
}

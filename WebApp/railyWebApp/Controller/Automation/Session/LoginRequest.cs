// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace railyWebApp.Controller.Automation.Session
{
    public class LoginRequest
    {
        [JsonProperty("username")] public string Username { get; set; } = string.Empty;
        [JsonProperty("password")] public string Password { get; set; } = string.Empty;

        public bool IsValid()
        {
            if (string.IsNullOrEmpty(Username)) return false;
            if (string.IsNullOrEmpty(Password)) return false;
            return true;
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace libShared.PODs
{
    public class LoginMessage
    {
        [JsonProperty("code")] public int Code { get; set; } = 1;
        [JsonProperty("error_code")] public string Error_Code { get; set; } = string.Empty;
        [JsonProperty("msg")] public string Msg { get; set; } = string.Empty;

        [JsonIgnore] public bool HasValue { get; private set; } = false;

        public static LoginMessage Parse(string data)
        {
            try
            {
                var instance = JsonConvert.DeserializeObject<LoginMessage>(data);
                instance.HasValue = true;
                return instance;
            }
            catch (Exception)
            {
                // ignore
            }

            return new LoginMessage
            {
                Msg = data,
                HasValue = !string.IsNullOrEmpty(data?.Trim())
            };
        }
    }

}

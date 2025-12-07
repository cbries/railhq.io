// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Newtonsoft.Json;
using System;

namespace railyWebIndex.Pages.Profile.Helper
{
    public class EditError
    {
        [JsonProperty("code")] public int Code { get; set; } = 1;
        [JsonProperty("details")] public string Details { get; set; } = string.Empty;
        [JsonProperty("hint")] public string Hint { get; set; } = string.Empty;
        [JsonProperty("message")] public string Message { get; set; } = string.Empty;

        [JsonIgnore] public bool HasValue { get; private set; } = false;

        public static EditError Parse(string data)
        {
            try
            {
                var instance = JsonConvert.DeserializeObject<EditError>(data);
                instance.HasValue = true;
                return instance;
            }
            catch (Exception)
            {
                // ignore
            }

            return new EditError
            {
                Message = data,
                HasValue = !string.IsNullOrEmpty(data?.Trim())
            };
        }
    }
}

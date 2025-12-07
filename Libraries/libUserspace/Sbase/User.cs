// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

// ReSharper disable ExplicitCallerInfoArgument

namespace libUserspace.Sbase
{
    /// <summary>
    /// User model (formerly stored in Supabase users table).
    /// </summary>
    public class User
    {
        [JsonIgnore] public bool IsNew { get; set; } = false;
        public int Id { get; set; }
        public Guid UserId { get; set; }
        public JObject Data { get; set; }

        public void Apply(UserData data)
        {
            Data = JObject.FromObject(data);
        }

        public UserData GetUserData()
        {
            try
            {
                if (Data == null) return new();
                var jsonData = Data.ToString(Formatting.None);
                if (string.IsNullOrEmpty(jsonData)) return new();

                return JsonConvert.DeserializeObject<UserData>(jsonData);

            }
            catch
            {
                // ignore
            }

            return new();
        }
    }
}

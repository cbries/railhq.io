// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libUtilities
{
    public static class JsonExtensions
    {
        public static bool IsJsonObject(this string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return false;
            var str = jsonMessage.Trim();
            if (str.Length == 0) return false;
            return str[0] == '{' && str[^1] == '}';
        }

        public static bool IsJsonArray(this string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return false;
            var str = jsonMessage.Trim();
            if (str.Length == 0) return false;
            return str[0] == '[' && str[^1] == ']';
        }

        public static JObject ToJsonObject(this string jsonMessage)
        {
            if(string.IsNullOrEmpty(jsonMessage)) return JObject.Parse("{}");
            return JObject.Parse(jsonMessage);
        }

        public static JArray ToJsonArray(this string jsonMessage)
        {
            if (string.IsNullOrEmpty(jsonMessage)) return JArray.Parse("[]");
            return JArray.Parse(jsonMessage);
        }

        public static string ToIntendedString(this JObject jsonObject)
        {
            if (jsonObject == null) return "{}";
            return jsonObject.ToString(Formatting.Indented);
        }

        public static string ToCleanString(this JObject jsonObject)
        {
            if (jsonObject == null) return "{}";
            return jsonObject.ToString(Formatting.None);
        }

        public static string ToIntendedString(this JArray jsonArray)
        {
            if (jsonArray == null) return "{}";
            return jsonArray.ToString(Formatting.Indented);
        }

        public static string ToCleanString(this JArray jsonObject)
        {
            if (jsonObject == null) return "{}";
            return jsonObject.ToString(Formatting.None);
        }

        public static int GetInt(this JToken obj, string key, int def = 0)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (int.TryParse(obj[key].ToString(), out var v))
                    return v;
            }
            return def;
        }

        public static double GetNumber(this JToken obj, string key, int def = 0)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (double.TryParse(obj[key].ToString(), out var v))
                    return v;
            }
            return def;
        }

        public static string GetString(this JToken obj, string key, string def = "")
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                if (obj[key] == null)
                    return def;

                var v = obj[key]?.ToString();
                if (v != null && v.Trim().Equals("null"))
                    return null;
                return v;
            }
            return def;
        }

        public static List<string> GetStringList(this JToken obj, string key)
        {
            if (obj?[key] == null) return null;
            var ar = obj[key] as JArray;
            if (ar == null) return null;
            var v = new List<string>();
            foreach (var it in ar)
            {
                if (it == null) continue;
                v.Add(it.ToString());
            }
            return v;
        }

        public static bool GetBool(this JToken obj, string key, bool def = false)
        {
            if (obj == null) return def;
            if (obj[key] != null)
            {
                var v = obj[key]?.ToString();
                if (bool.TryParse(v, out var vv))
                    return vv;
            }
            return def;
        }
    }
}

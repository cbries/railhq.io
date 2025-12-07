// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace libUtilities
{

    public class ConcurrentDictionaryToBagConverter<TKey, TValue> : JsonConverter<ConcurrentDictionary<TKey, TValue>>
    {
        public override ConcurrentDictionary<TKey, TValue> ReadJson(JsonReader reader, Type objectType, ConcurrentDictionary<TKey, TValue> existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            // Deserialize JSON into a list of TKey (for deserialization)
            var items = JArray.Load(reader);
            var dictionary = new ConcurrentDictionary<TKey, TValue>();

            foreach (var item in items)
            {
                // Assuming we only need the keys (for example, we map the value to a default value, like 'true')
                var key = item.ToObject<TKey>(serializer);
                dictionary.TryAdd(key, default(TValue));
            }

            return dictionary;
        }

        public override void WriteJson(JsonWriter writer, ConcurrentDictionary<TKey, TValue> value, JsonSerializer serializer)
        {
            // Serialize the keys of the dictionary as a list (for serialization)
            var list = new JArray();

            foreach (var kvp in value)
            {
                // Add only the keys to the list
                list.Add(JToken.FromObject(kvp.Key, serializer));
            }

            list.WriteTo(writer);
        }
    }

}

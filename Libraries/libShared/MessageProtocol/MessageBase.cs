// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json.Linq;

namespace libShared.MessageProtocol
{
    public abstract class MessageBase
    {
        public JObject ToJson()
        {
            return JObject.FromObject(this);
        }
    }

    /// <summary>
    /// Usage:
    ///  `var msg = JsonMessageParser.Parse<MessageSpeedstep>(tkn);`
    /// </summary>
    public static class JsonMessageParser
    {
        public static T Parse<T>(JToken tkn) where T : class
        {
            if (tkn is not JObject jobj)
                return null;

            try
            {
                return jobj.ToObject<T>();
            }
            catch(Exception)
            {
                // optional: logging for error cases

                return null;
            }
        }
    }
}

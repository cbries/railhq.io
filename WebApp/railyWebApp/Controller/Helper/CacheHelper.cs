// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using System;
using libShared;

namespace railyWebApp.Controller.Helper
{
    public class CacheHelper
    {
        public static void ClearCache(IMemoryCache cache, HttpContext ctx)
        {
            foreach (var it in SessionGlobals.AllKeys)
            {
                try
                {
                    RemoveCachedValue(cache, it, ctx);
                }
                catch
                {
                    // ignore
                }
            }
        }

        public static void RemoveCachedValue(IMemoryCache cache, string key, HttpContext ctx)
        {
            try
            {
                var ctxSession = ctx.Session;
                if (string.IsNullOrEmpty(ctxSession?.Id)) return;
                var sessionKey = $"Session:{ctxSession.Id}:{key}";
                cache?.Remove(sessionKey);
            }
            catch
            {
                //ignore
            }

            try
            {
                ctx?.Session?.Remove(key);
            }
            catch
            {
                // ignore
            }
        }

        public static void SetCachedValue(IMemoryCache cache, string key, string value, HttpContext ctx, bool applyToSession = true)
        {
            try
            {
                var ctxSession = ctx.Session;
                var sessionKey = $"Session:{ctxSession.Id}:{key}";
                if (!string.IsNullOrEmpty(value))
                {
                    cache.Set(sessionKey, value, TimeSpan.FromMinutes(5));
                }

                if (applyToSession)
                {
                    if (!string.IsNullOrEmpty(value))
                        ctx.Session.SetString(key, value);
                }
            }
            catch
            {
                // ignore
            }
        }

        public static string GetCachedValue(IMemoryCache cache, string key, HttpContext ctx, string defaultValue = "", string sessionId = "")
        {
            try
            {
                var ctxSessionId = !string.IsNullOrEmpty(sessionId) ? sessionId : ctx?.Session?.Id;
                var sessionKey = $"Session:{ctxSessionId}:{key}";
                if (!cache.TryGetValue(sessionKey, out string value))
                {
                    value = ctx?.Session?.GetString(key);

                    if (!string.IsNullOrEmpty(value))
                    {
                        cache.Set(sessionKey, value, TimeSpan.FromMinutes(5));
                    }
                    else
                    {
                        cache.Remove(sessionKey);

                        return defaultValue;
                    }
                }

                return value;
            }
            catch
            {
                // ignore
            }

            return defaultValue;
        }
    }
}

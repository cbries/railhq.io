// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using Microsoft.AspNetCore.Http;

namespace railyWebIndex
{
    public static class HttpContextExtension
    {
        public static bool GetBool(this HttpContext ctx, string name, bool def = false)
        {
            try
            {
                var v = ctx.Session.GetString(name);
                if (string.IsNullOrEmpty(v)) return def;
                if (bool.TryParse(v.Trim(), out var b))
                    return b;
                return def;
            }
            catch
            {
                // ignore
            }

            return def;
        }

        public static int GetInt(this HttpContext ctx, string name, int def = 0)
        {
            try
            {
                var v = ctx.Session.GetString(name);
                if (string.IsNullOrEmpty(v)) return def;
                if (int.TryParse(v.Trim(), out var b))
                    return b;
                return def;
            }
            catch
            {
                // ignore
            }

            return def;
        }

        public static DateTime GetDateTime(this HttpContext ctx, string name, DateTime def = default)
        {
            try
            {
                var v = ctx.Session.GetString(name);
                if (string.IsNullOrEmpty(v)) return def;
                if (DateTime.TryParse(v.Trim(), out var b))
                    return b;
                return def;
            }
            catch
            {
                // ignore
            }

            return def;
        }

    }
}

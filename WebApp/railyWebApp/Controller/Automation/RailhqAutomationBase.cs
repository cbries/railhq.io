// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    public class RailHqUser
    {
        public AuthUser User { get; internal set; }

        public string Email => User?.Email ?? string.Empty;
    }

    public abstract class RailhqAutomationBase : ControllerBase
    {
        protected readonly SupabaseService AuthService;
        protected readonly IMemoryCache Cache;

        protected RailhqAutomationBase(
            SupabaseService authService,
            IMemoryCache cache)
        {
            AuthService = authService;
            Cache = cache;
        }

        private RailHqUser SessionOwner => HttpContext.Items[RailhqAuthenticationFilter.AuthUserName] as RailHqUser;

        protected string Uid
        {
            get
            {
                var uid = GetCachedValue(SessionGlobals.AuthUid, HttpContext);
                return uid;
            }
        }

        protected string GetCachedValue(string key, HttpContext ctx, string defaultValue = "")
        {
            return Helper.CacheHelper.GetCachedValue(Cache, key, ctx, defaultValue);
        }
    }
}

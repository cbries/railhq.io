// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    public abstract class RailhqControllerBase : ControllerBase
    {
        protected readonly SupabaseService AuthService;
        protected readonly IMemoryCache Cache;

        protected RailhqControllerBase(
            SupabaseService authService,
            IMemoryCache cache)
        {
            AuthService = authService;
            Cache = cache;
        }

        protected string GetCachedValue(string key, HttpContext ctx, string defaultValue = "")
        {
            return Helper.CacheHelper.GetCachedValue(Cache, key, ctx, defaultValue);
        }

        protected async Task<AuthUser> ValideRequestBearer(HttpContext ctx)
        {
            var bearerToken = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUserSession, ctx);
            if (string.IsNullOrEmpty(bearerToken))
                bearerToken = ctx.Request.Headers["Authorization"].ToString().Replace("Bearer", string.Empty).Trim();
            if (string.IsNullOrEmpty(bearerToken)) throw new Exception("invalid Bearer token");
            var user = await AuthService.GetUser(bearerToken);
            if (user?.Id == null) throw new Exception("invalid Bearer token");
            return user;
        }

        protected bool ValidateRequestBearrer(HttpContext ctx, string providedBearerToken)
        {
            var bearerToken = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUserSession, ctx);
            if (string.IsNullOrEmpty(bearerToken))
                bearerToken = ctx.Request.Headers["Authorization"].ToString().Replace("Bearer", string.Empty).Trim();
            if (string.IsNullOrEmpty(bearerToken)) throw new Exception("invalid Bearer token");
            return bearerToken.Equals(providedBearerToken);
        }
    }
}

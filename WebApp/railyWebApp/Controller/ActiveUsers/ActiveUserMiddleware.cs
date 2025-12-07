// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using libShared;
using Microsoft.Extensions.Caching.Memory;
// ReSharper disable UnusedMember.Global
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.ActiveUsers
{
    public class ActiveUserMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ActiveUserService _activeUserService;
        private readonly IMemoryCache _cache;

        public ActiveUserMiddleware(
            RequestDelegate next, 
            ActiveUserService activeUserService,
            IMemoryCache cache)
        {
            _next = next;
            _activeUserService = activeUserService;
            _cache = cache;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var userId = Helper.CacheHelper.GetCachedValue(_cache, SessionGlobals.AuthUid, context);
            if (!string.IsNullOrEmpty(userId))
                await _activeUserService.MarkUserActive(userId);
            await _next(context);
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    /// <summary>
    /// Simplified authentication filter for local network usage.
    /// 
    /// HINWEIS: Diese Anwendung ist für den lokalen Netzwerkbetrieb konzipiert.
    /// Die Authentifizierung dient lediglich zur Unterscheidung verschiedener 
    /// Familienmitglieder/Benutzer und ist KEIN Enterprise-Level Sicherheitssystem.
    /// 
    /// Bei fehlendem Token wird automatisch ein anonymer Benutzer angenommen,
    /// sodass die API-Endpunkte auch ohne Anmeldung funktionieren.
    /// </summary>
    public class RailhqAuthenticationFilter : IAsyncActionFilter
    {
        public static string AuthUserName = "free@railhq.io";
        
        /// <summary>
        /// Default user ID for anonymous/local access
        /// </summary>
        public const string LocalAnonymousUserId = "b0ca0843-1d51-4c31-84c8-00f4fa9c196b";

        protected readonly SupabaseService AuthService;
        protected readonly IMemoryCache Cache;

        public RailhqAuthenticationFilter(
            SupabaseService authService,
            IMemoryCache cache)
        {
            AuthService = authService;
            Cache = cache;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;

            // Simplified auth: try to get user, but allow anonymous access
            var user = await TryGetUserOrAnonymous(httpContext);
            httpContext.Items[AuthUserName] = user;

            await next();
        }

        /// <summary>
        /// Tries to authenticate the user. If authentication fails, returns an anonymous local user.
        /// This allows the API to work without strict authentication for local network usage.
        /// </summary>
        protected async Task<RailHqUser> TryGetUserOrAnonymous(HttpContext ctx)
        {
            try
            {
                return await CheckApiUser(ctx, AuthService, Cache);
            }
            catch
            {
                // Authentication failed - return anonymous local user for local network usage
                return CreateAnonymousLocalUser();
            }
        }

        /// <summary>
        /// Creates an anonymous user for local network access.
        /// </summary>
        internal static RailHqUser CreateAnonymousLocalUser()
        {
            return new RailHqUser
            {
                User = new AuthUser
                {
                    Id = LocalAnonymousUserId,
                    Email = "local@localhost",
                    Role = "local"
                }
            };
        }

        internal static async Task<RailHqUser> CheckApiUser(
            HttpContext ctx,
            SupabaseService authService,
            IMemoryCache cache)
        {
            // 1. Try to get token from cache (session-based auth)
            var bearerToken = Helper.CacheHelper.GetCachedValue(cache, SessionGlobals.AuthUserSession, ctx);
            
            // 2. Try Authorization header
            if (string.IsNullOrEmpty(bearerToken))
                bearerToken = ctx.Request.Headers["Authorization"].ToString().Replace("Bearer", string.Empty).Trim();
            
            // 3. Try query parameter (for EventSource which cannot send headers)
            if (string.IsNullOrEmpty(bearerToken))
                bearerToken = ctx.Request.Query["token"].ToString();
            
            if (string.IsNullOrEmpty(bearerToken)) throw new Exception("no authentication token provided");
            var user = await authService.GetUser(bearerToken);
            if (user?.Id == null) throw new Exception("invalid authentication token");
            return new RailHqUser { User = user };
        }
    }
}

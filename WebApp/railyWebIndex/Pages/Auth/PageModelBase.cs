// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Auth
{
    public abstract class PageModelLoginBase : PageModel
    {
        protected readonly SupabaseService AuthService;
        protected readonly IMemoryCache Cache;

        protected PageModelLoginBase(
            SupabaseService authService,
            IMemoryCache cache)
        {
            AuthService = authService;
            Cache = cache;
        }
    }

    public abstract class PageModelBase : PageModelLoginBase
    {
        protected bool DoRedirectIfNotValidUser { get; set; } = true;

        protected string AccessToken { get; private set; }

        protected AuthUser RailhqUser { get; private set; }

        protected PageModelBase(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<bool> ValidateSessionAsync()
        {
            AccessToken = HttpContext.Session.GetString(SessionGlobals.AuthUserSession);

            if (string.IsNullOrEmpty(AccessToken))
            {
                return false;
            }

            try
            {
                RailhqUser = await AuthService.GetUser(AccessToken);
                return RailhqUser != null;
            }
            catch
            {
                return false;
            }
        }

        public override async Task OnPageHandlerExecutionAsync(
            PageHandlerExecutingContext context,
            PageHandlerExecutionDelegate next)
        {
            AuthService.LoadSession();

            var isValidSession = await ValidateSessionAsync();
            var isOnLoginPage = string.Equals(Request.Path, Globals.PageLogin, StringComparison.OrdinalIgnoreCase);

            if (!isValidSession && !isOnLoginPage)
            {
                if (DoRedirectIfNotValidUser)
                {
                    context.Result = RedirectToPage(Globals.PageLogin);
                    return;
                }
            }

            await next();
        }

        protected Task<List<libUserspace.Sbase.Notification>> LoadSystemNotifications(
            bool onlyUser = false,
            int limit = 20)
        {
            // Notifications are no longer loaded from Supabase
            // Return empty list - implement local storage if needed
            return Task.FromResult(new List<libUserspace.Sbase.Notification>());
        }

        protected Task<List<libUserspace.Sbase.Notification>> LoadNotifications(
            bool onlyUser = false, 
            int limit = 20,
            string type = "info")
        {
            // Notifications are no longer loaded from Supabase
            // Return empty list - implement local storage if needed
            return Task.FromResult(new List<libUserspace.Sbase.Notification>());
        }
    }
}

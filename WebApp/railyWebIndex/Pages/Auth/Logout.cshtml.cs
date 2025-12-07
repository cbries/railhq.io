// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Auth
{
    public class LogoutModel : PageModelLoginBase
    {
        public string RedirectUrl { get; set; } = Globals.PageLogin;

        public LogoutModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            try
            {
                railyWebApp.Controller.Helper.CacheHelper.ClearCache(Cache, HttpContext);

                HttpContext.Session.Clear();

                if (AuthService != null)
                    await AuthService.Logout();

                return RedirectToPage(RedirectUrl);
            }
            catch
            {
                // ignore
            }

            return Page();
        }
    }
}

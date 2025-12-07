// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using railyWebIndex.Pages.Auth;
using System.Threading.Tasks;
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages
{
    public class IndexModel : PageModelBase
    {
        private readonly ILogger<DashboardModel> _logger;

        public IndexModel(
            SupabaseService authService,
            ILogger<DashboardModel> logger,
            IMemoryCache cache)
            : base(authService, cache)
        {
            DoRedirectIfNotValidUser = false;
            _logger = logger;
        }

        public async Task<IActionResult> OnGet()
        {
            // Redirect to dashboard if logged in, otherwise to login page
            if (RailhqUser != null)
            {
                return RedirectToPage("/Dashboard");
            }
            
            return RedirectToPage("/Auth/Login");
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

// ReSharper disable once ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Auth
{
    public class RedirectModel : PageModelBase
    {
        public string RedirectUrl { get; set; } = Globals.PageLogin;

        public RedirectModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            try
            {
                var authToken = HttpContext.Session.GetString(SessionGlobals.AuthUserSession);
                if (!string.IsNullOrEmpty(authToken))
                {
                    var res = await AuthService.Validate(authToken);
                    if (res != null)
                    {
                        TempData["loginMessage"] = "Sitzung wurde aufgebaut.";

                        return Redirect(Globals.PageWorkspaceOverview);
                    }
                }

                TempData["loginMessage"] = "Keine Sitzung vorhanden, bitte anmelden.";
            }
            catch (InvalidDataException ex)
            {
                TempData["loginMessage"] = ex.Message;
            }

            return RedirectToPage(RedirectUrl);
        }
    }
}

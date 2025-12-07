// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebIndex.Pages.Auth;
using System;
using System.Threading.Tasks;
using libShared;
using libShared.PODs;

namespace railyWebIndex.Pages.Profile
{
    public class ChangeEmailModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)]
        public string EMail
        {
            get
            {
                if (RailhqUser == null) return "<invalid>";
                return RailhqUser.Email;
            }
        }

        [BindProperty] public string EMailNew { get; set; }

        public ChangeEmailModel(
            SupabaseService authService,
            IMemoryCache cache)
            : base(authService, cache)
        {
        }

        public Task<IActionResult> OnGet()
        {
            return Task.FromResult<IActionResult>(Page());
        }

        public Task<IActionResult> OnPost()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(EMailNew))
                {
                    ViewData["errorMessage"] = "Bitte gib eine neue E-Mail-Adresse ein.";
                    return Task.FromResult<IActionResult>(Page());
                }

                // Validate email format
                if (!EMailNew.Contains("@") || !EMailNew.Contains("."))
                {
                    ViewData["errorMessage"] = "Bitte gib eine gültige E-Mail-Adresse ein.";
                    return Task.FromResult<IActionResult>(Page());
                }

                if (RailhqUser == null)
                {
                    ViewData["errorMessage"] = "Benutzer nicht gefunden.";
                    return Task.FromResult<IActionResult>(Page());
                }

                // Check if new email is the same as current
                if (EMailNew.Equals(RailhqUser.Email, StringComparison.OrdinalIgnoreCase))
                {
                    ViewData["errorMessage"] = "Die neue E-Mail-Adresse ist identisch mit der aktuellen.";
                    return Task.FromResult<IActionResult>(Page());
                }

                // Update the email in users.json
                var result = AuthService.UpdateEmail(RailhqUser.Id, EMailNew);
                if (!result)
                {
                    ViewData["errorMessage"] = "Die E-Mail-Adresse konnte nicht geändert werden.";
                    return Task.FromResult<IActionResult>(Page());
                }

                TempData["statusMessage"] = "E-Mail-Adresse erfolgreich geändert. Bitte melde dich erneut an.";
                return Task.FromResult<IActionResult>(RedirectToPage("/Auth/Logout"));
            }
            catch (Exception ex)
            {
                ViewData["errorMessage"] = ex.Message;
                return Task.FromResult<IActionResult>(Page());
            }
        }
    }
}

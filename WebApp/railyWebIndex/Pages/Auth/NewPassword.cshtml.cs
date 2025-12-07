// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Threading.Tasks;
using libShared.PODs;

// ReSharper disable once ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Auth
{
    public class NewPasswordModel : PageModelLoginBase
    {
        [BindProperty] public string AccessToken { get; set; }
        [BindProperty] public string RefreshToken { get; set; }
        [BindProperty] public string Password { get; set; }
        [BindProperty] public string ConfirmPassword { get; set; }


        public NewPasswordModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public IActionResult OnGet()
        {
            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            if (string.IsNullOrEmpty(AccessToken) || string.IsNullOrEmpty(RefreshToken))
            {
                ModelState.AddModelError(string.Empty, "Ungültige Anfrage.");
                return Page();
            }

            if (string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(ConfirmPassword))
            {
                ViewData["errorMessage"] = "Alle Felder sind erforderlich!";
                return Page();
            }

            if (Password != ConfirmPassword)
            {
                ViewData["errorMessage"] = "Die Passwörter stimmen nicht überein!";
                return Page();
            }

            var resetSuccessful = await ResetUserPassword(Password);
            if (!resetSuccessful)
            {
                ViewData["errorTitle"] = "Passwort-Reset fehlgeschlagen!";
                return Page();
            }

            ViewData["statusMessage"] = "Passwort erfolgreich zurückgesetzt!";

            return RedirectToPage(Globals.PageLogin);
        }

        private async Task<bool> ResetUserPassword(string newPassword)
        {
            try
            {
                // Validate the access token first
                var user = await AuthService.GetUser(AccessToken);
                if (user == null)
                {
                    ViewData["errorMessage"] = "Passwort-Reset-Link nicht valide!";
                    return false;
                }

                // Update the password
                var success = AuthService.UpdatePassword(user.Id, newPassword);
                if (success)
                {
                    ViewData["updatedAt"] = DateTime.UtcNow;
                    return true;
                }

                ViewData["errorMessage"] = "Passwort-Reset fehlgeschlagen!";
            }
            catch (Exception ex)
            {
                var errorInstance = LoginMessage.Parse(ex.Message);
                if (errorInstance.HasValue)
                    ViewData["errorMessage"] = errorInstance.Msg;
            }

            return false;
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using libShared.PODs;
using railyWebIndex.Pages.Profile;

// ReSharper disable once ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Auth
{
    public class LoginModel : PageModelLoginBase
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly WebAppServiceApi _serviceApi;

        public LoginModel(
            SupabaseService authService,
            ILogger<DashboardModel> logger,
            IMemoryCache cache,
            WebAppServiceApi serviceApi) : base(authService, cache)
        {
            _logger = logger;
            _serviceApi = serviceApi;
        }

        public IActionResult OnGet()
        {
            if (TempData["loginMessage"] != null)
            {
                var errorInstance = LoginMessage.Parse(TempData["loginMessage"].ToString());
                if (errorInstance.HasValue)
                    ViewData["errorMessage"] = errorInstance.Msg;
            }
            
            if (TempData["successMessage"] != null)
            {
                ViewData["successMessage"] = TempData["successMessage"];
            }

            return Page();
        }

        private async Task CreateDefaultDatabaseEntries()
        {
            //
            // Wir brauchen einen Basiseintrag in "users".
            //
            var uid = railyWebApp.Controller.Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
            if (!string.IsNullOrEmpty(uid))
            {
                var r = await EditModel.CreateDefaultUser(uid, AuthService);
                if(!r)
                    ViewData["errorMessage"] = "Aktualisierung der Benutzerinformationen temporär fehlerhaft.";
            }
        }

        private async Task LoadDemoData()
        {
            try
            {
                var sessionId = HttpContext.Session.Id;
                var uid = railyWebApp.Controller.Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                await _serviceApi.CallDemoRestore2Async(sessionId, uid);
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Information, ex.Message);
            }
        }

        public async Task<IActionResult> OnPost(string email, string password)
        {
            try
            {
                var session = await AuthService.Login(email, password, Request.Form["remember"] == "on");

                if (!string.IsNullOrEmpty(session?.AccessToken))
                {
                    railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthUserSession, session.AccessToken, HttpContext);
                    railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthUserRefreshToken, session.RefreshToken, HttpContext);
                    railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthEmail, email, HttpContext);
                    railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthUid, session.User?.Id ?? string.Empty, HttpContext);
                    await HttpContext.Session.CommitAsync();

                    await CreateDefaultDatabaseEntries();
                    await LoadDemoData();

                    return RedirectToPage(Globals.PageDashboard);
                }

                ViewData["errorMessage"] = "Benutzername oder Passwort falsch.";
            }
            catch (Exception ex)
            {
                var errorInstance = LoginMessage.Parse(ex.Message);
                if(errorInstance.HasValue)
                    ViewData["errorMessage"] = errorInstance.Msg;
            }

            return Page();
        }
        
        public async Task<IActionResult> OnPostRegister(string registerEmail, string registerPassword, string registerPasswordConfirm)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(registerEmail) || !registerEmail.Contains("@"))
                {
                    ViewData["errorMessage"] = "Bitte geben Sie eine gültige E-Mail-Adresse ein.";
                    return Page();
                }
                
                if (string.IsNullOrWhiteSpace(registerPassword) || registerPassword.Length < 6)
                {
                    ViewData["errorMessage"] = "Das Passwort muss mindestens 6 Zeichen lang sein.";
                    return Page();
                }
                
                if (registerPassword != registerPasswordConfirm)
                {
                    ViewData["errorMessage"] = "Die Passwörter stimmen nicht überein.";
                    return Page();
                }
                
                // Register user using the JSON auth service
                var session = await AuthService.Register(registerEmail, registerPassword);
                
                if (session != null && !string.IsNullOrEmpty(session.User?.Id))
                {
                    _logger.LogInformation("New user registered: {Email}", registerEmail);
                    TempData["successMessage"] = "Registrierung erfolgreich! Sie können sich jetzt anmelden.";
                    return RedirectToPage();
                }
                
                ViewData["errorMessage"] = "Registrierung fehlgeschlagen. Bitte versuchen Sie es erneut.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for {Email}", registerEmail);
                
                // Handle specific error messages
                if (ex.Message.Contains("already exists") || ex.Message.Contains("bereits"))
                {
                    ViewData["errorMessage"] = "Ein Benutzer mit dieser E-Mail-Adresse existiert bereits.";
                }
                else
                {
                    ViewData["errorMessage"] = "Registrierung fehlgeschlagen: " + ex.Message;
                }
            }

            return Page();
        }
    }
}

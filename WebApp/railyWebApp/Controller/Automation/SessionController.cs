// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebApp.Controller.Automation.Dto;
using railyWebApp.Controller.Automation.Session;
using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class SessionController : RailhqAutomationBase
    {
        public SessionController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        /// <summary>
        /// Führt den Login durch.
        /// </summary>
        /// <param name="request">Login-Daten (Benutzername und Passwort)</param>
        /// <returns>AccessToken bei Erfolg</returns>
        /// <response code="200">Login erfolgreich</response>
        /// <response code="401">Ungültige Anmeldedaten</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!request.IsValid())
                return ApiError.Unauthorized(this);

            var session = await AuthServiceHelper.GetSession(
                AuthService,
                request.Username, 
                request.Password);

            if (session?.User?.Email != null
                && session.User.Email.Equals(request.Username))
            {
                return Ok(ApiResponse.Success("Login successful", new
                {
                    accessToken = session.AccessToken
                }));
            }

            return ApiError.Unauthorized(this);
        }

        /// <summary>
        /// Führt den Logout durch.
        /// </summary>
        /// <returns>Erfolgsmeldung</returns>
        /// <response code="200">Logout erfolgreich</response>
        /// <response code="401">Nicht angemeldet oder Fehler beim Logout</response>
        [HttpPost("logout")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Logout()
        {
            if (AuthService == null)
                return ApiError.Unauthorized(this);

            try
            {
                Helper.CacheHelper.ClearCache(Cache, HttpContext);
                HttpContext.Session.Clear();
                if (AuthService != null)
                    await AuthService.Logout();
                return Ok(ApiResponse.Success("Logout successful"));
            }
            catch
            {
                // ignore
            }

            return ApiError.Unauthorized(this);
        }

        /// <summary>
        /// Gibt Informationen zum aktuell angemeldeten Benutzer zurück.
        /// </summary>
        /// <returns>Nutzerdaten (E-Mail, ID, Token etc.)</returns>
        /// <response code="200">Benutzerdaten erfolgreich abgerufen</response>
        /// <response code="401">Nicht angemeldet</response>
        [HttpGet("me")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> Me()
        {
            try
            {
                var railHqUser = await RailhqAuthenticationFilter.CheckApiUser(
                    HttpContext,
                    AuthService,
                    Cache);

                var bearerToken = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUserSession, HttpContext);

                return Ok(ApiResponse.Success("User info", new
                {
                    email = railHqUser.Email,
                    userId = railHqUser.User.Id,
                    createdAt = railHqUser.User.CreatedAt,
                    lastSignInAt = railHqUser.User.LastSignInAt,
                    token = bearerToken
                }));
            }
            catch (Exception)
            {
                // ignore
            }

            return ApiError.Unauthorized(this);
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using libUserspace;
using Microsoft.AspNetCore.Mvc;
using railyWebApp.Controller.Automation.Session;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class Auth : ControllerBase
    {
        private readonly SupabaseService _authService;

        public Auth(SupabaseService authService)
        {
            _authService = authService;
        }

        [HttpGet("getUid")]
        public async Task<IActionResult> GetUid(string jwt)
        {
            if (string.IsNullOrEmpty(jwt))
                return Unauthorized(new { message = "invalid authentication" });

            var user = await _authService.Validate(jwt);
            if (user == null)
                return Unauthorized(new { message = "invalid authentication" });

            return Ok(new { uid = user.Id });
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!request.IsValid())
                return Unauthorized(new { message = "invalid authentication" });
            
            var session = await AuthServiceHelper.GetSession(
                _authService, 
                request.Username, 
                request.Password);

            if (session?.User?.Email != null
                && session.User.Email.Equals(request.Username))
            {
                return Ok(new
                {
                    message = "success",
                    accessToken = session.AccessToken
                });
            }

            return Unauthorized(new { message = "invalid authentication" });
        }
    }
}

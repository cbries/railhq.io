// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using Microsoft.AspNetCore.Mvc;

namespace railyWebIndex.Controller
{
    [ApiController]
    [Route("api/info")]
    public class Info : ControllerBase
    {
        [HttpGet("sessionId")]
        public IActionResult GetSessionId()
        {
            return Ok(HttpContext.Session.Id);
        }
    }
}

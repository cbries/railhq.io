// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using System.IO;
using System.Threading.Tasks;
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class FleetController : RailhqAutomationBase
    {
        private string FleetBaseDir => libUserspace.Filesystem.FleetBaseDir;
        private string FleetLocomotivesDir => Path.Combine(FleetBaseDir, Uid, "Locomotives");
        private string FleetAccessoriesDir => Path.Combine(FleetBaseDir, Uid, "Accessories");
        
        public FleetController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        /// <summary>
        /// Ruft eine gespeicherte Lokomotiv-Entity-Datei ab (z. B. Z21-Decoder mit Adresse 9).
        /// </summary>
        /// <remarks>
        /// Beispielaufrufe:
        /// 
        /// - GET https://railhq.io:5001/api/fleet/entity?driverName=z21&amp;objectId=9  
        /// - GET https://railhq.io:5001/api/fleet/entity?driverName=z21&amp;objectId=10
        /// 
        /// Rückgabe bei Erfolg (200):
        /// ```json
        /// {
        ///   "status": 200,
        ///   "message": "File found",
        ///   "fileName": "z21_9.json",
        ///   "content": "{...}"
        /// }
        /// ```
        /// Rückgabe bei Fehler (404):
        /// ```json
        /// {
        ///   "status": 404,
        ///   "message": "Entity(z21::9) not found"
        /// }
        /// ```
        /// </remarks>
        /// <param name="driverName">Name des Treibers, z. B. "z21"</param>
        /// <param name="objectId">Objekt-ID der Lokomotive</param>
        /// <returns>JSON-Antwort mit Dateiinhalten oder Fehlerbeschreibung</returns>
        /// <response code="200">Datei erfolgreich gefunden</response>
        /// <response code="400">Fehlender Parameter</response>w
        /// <response code="404">Datei nicht vorhanden</response>
        [HttpGet("entity")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFile(
            [FromQuery] string driverName,
            [FromQuery] string objectId)
        {
            if (string.IsNullOrWhiteSpace(driverName) || string.IsNullOrWhiteSpace(objectId))
            {
                return BadRequest(new
                {
                    status = 400,
                    message = "Missing required parameter: driverName and objectId are required"
                });
            }

            var fileName = $"{driverName}_{objectId}.json";
            var filePath = Path.Combine(FleetLocomotivesDir, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new
                {
                    status = 404,
                    message = $"Entity({driverName}::{objectId}) not found"
                });
            }

            var content = await System.IO.File.ReadAllTextAsync(filePath);
            return Ok(new
            {
                status = 200,
                message = "File found",
                fileName,
                content
            });
        }

    }
}

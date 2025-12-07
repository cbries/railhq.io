// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebApp.Controller.Automation.Dto;
using railyWebApp.Controller.Automation.Services.Impl;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Swashbuckle.AspNetCore.Annotations;
using railyWebApp.Controller.Automation.Dto.Commands;
using railyWebApp.Controller.Automation.Dto.Entity;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class LocomotiveController : RailhqAutomationBase
    {
        private LocomotiveService _locService;
        private LocomotiveService LocService
        {
            get
            {
                if (_locService == null)
                    _locService = new LocomotiveService(Uid);
                return _locService;
            }
        }

        public LocomotiveController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        // === Verwaltung ===

        /// <summary>
        /// Gibt eine Liste aller Lokomotiven im aktuellen Workspace zurück.
        /// Wird im UI z. B. beim Öffnen des Fuhrpark-Tabs verwendet.
        ///
        /// Beispiel:
        ///   https://railhq.io:5001/api/automation/locomotive/
        /// </summary>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Alle Lokomotiven abrufen",
            Description = "Gibt eine Liste aller Lokomotiven im aktuellen Workspace zurück. Wird z. B. beim Öffnen des Fuhrpark-Tabs verwendet."
        )]
        [ProducesResponseType(typeof(IEnumerable<Locomotive>), 200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllLocomotives()
        {
            try
            {
                var status = await LocService.GetAllLocomotivesAsync();
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Ruft Details zu einer bestimmten Lokomotive ab – identifiziert durch Treiber ("ecos", "z21", "demo") und Adresse.
        /// Ideal für direkte Abfragen z. B. aus Loksteuerungs-UI.
        ///
        /// Beispiel:
        ///   https://railhq.io:5001/api/automation/locomotive/ecos/1010
        /// </summary>
        [HttpGet("{driverName}/{address}")]
        [SwaggerOperation(
            Summary = "Lokomotive abrufen",
            Description = "Liefert Details zu einer bestimmten Lokomotive, identifiziert durch Treibername (z. B. \"ecos\") und Adresse."
        )]
        [ProducesResponseType(typeof(Locomotive), 200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetLocomotive(string driverName, int address)
        {
            try
            {
                var status = await LocService.GetStatusAsync(driverName, address);
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound); 

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        // === Steuerung ===

        /// <summary>
        /// Setzt die Geschwindigkeit der Lok (z. B. von Automatik oder manueller Steuerung).
        /// Unterstützt auch negative Werte, wenn rückwärts erlaubt ist.
        /// </summary>
        [HttpPost("{driverName}/{address}/speed")]
        [SwaggerOperation(
            Summary = "Lok-Geschwindigkeit setzen",
            Description = "Setzt die Geschwindigkeit der Lok (positiv oder negativ, je nach Richtung)."
        )]
        [ProducesResponseType(200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetSpeed(string driverName, int address, [FromBody] SpeedCommand command)
        {
            try
            {
                var status = await LocService.SetSpeedAsync(driverName, address, command.Speed);
                if (!status)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok();
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Setzt die Fahrtrichtung der Lok explizit (true = vorwärts, false = rückwärts).
        /// Kann in Kombination mit Geschwindigkeit oder separat verwendet werden.
        /// </summary>
        [HttpPost("{driverName}/{address}/direction")]
        [SwaggerOperation(
            Summary = "Lok-Fahrtrichtung setzen",
            Description = "Ändert die Richtung der Lok (true = vorwärts, false = rückwärts)."
        )]
        [ProducesResponseType(200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetDirection(string driverName, int address, [FromBody] DirectionCommand command)
        {
            try
            {
                var status = await LocService.SetDirectionAsync(driverName, address, command.Forward);
                if (!status)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok();
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Stoppt die Lok (z. B. für Notfall oder Haltepunkt).
        /// Implementierung kann wahlweise auf 0 setzen oder Stop-Befehl der Zentrale senden.
        /// </summary>
        [HttpPost("{driverName}/{address}/stop")]
        [SwaggerOperation(
            Summary = "Lok stoppen",
            Description = "Sendet einen Stoppbefehl an die Lok – entweder durch Setzen der Geschwindigkeit auf 0 oder über die Zentrale."
        )]
        [ProducesResponseType(200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> StopLocomotive(string driverName, int address)
        {
            try
            {
                var status = await LocService.StopLocomotiveAsync(driverName, address);
                if (!status)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok();
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Schaltet eine Funktionstaste – etwa Licht (F0), Sound (F1), Rauchgenerator (F2).
        /// Flexibel für jede Funktion nutzbar.
        /// </summary>
        [HttpPost("{driverName}/{address}/function/{functionNumber}")]
        [SwaggerOperation(
            Summary = "Lok-Funktion schalten",
            Description = "Schaltet eine Funktionstaste wie Licht (F0), Sound (F1) oder Sonderfunktionen (F2, F3...)."
        )]
        [ProducesResponseType(200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> ToggleFunction(string driverName, int address, int functionNumber, [FromBody] FunctionCommand command)
        {
            try
            {
                var status = await LocService.ToggleFunctionAsync(driverName, address, functionNumber, command.Active);
                if (!status)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok();
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        // === Statusabfragen ===

        /// <summary>
        /// Liefert aktuellen Status der Lok: Geschwindigkeit, Richtung, Blockposition, aktive Funktionen.
        /// Ideal für UI-Aktualisierung oder Debug.
        /// </summary>
        [HttpGet("{driverName}/{address}/status")]
        [SwaggerOperation(
            Summary = "Lok-Status abrufen",
            Description = "Liefert den aktuellen Status der Lok: Geschwindigkeit, Richtung, Blockposition und Funktionen."
        )]
        [ProducesResponseType(typeof(LocomotiveState), 200)]
        [ProducesResponseType((int)ErrorCode.LocomotiveNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetStatus(string driverName, int address)
        {
            try
            {
                var status = await LocService.GetStatusAsync(driverName, address);
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.LocomotiveNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }
    }
}

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
using railyWebApp.Controller.Automation.Dto.Entity;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class ControlStationController : RailhqAutomationBase
    {
        private ControlStationService _csService;
        private ControlStationService CsService
        {
            get
            {
                if (_csService == null)
                    _csService = new ControlStationService(Uid);
                return _csService;
            }
        }


        public ControlStationController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        /// <summary>
        /// Gibt eine Liste aller verfügbaren Digitalzentralen (z. B. Z21, ECoS) im aktuellen Workspace zurück.
        /// Wird z. B. für die Auswahl einer Steuerzentrale verwendet.
        /// 
        /// Beispiel:
        ///   https://railhq.io:5001/api/automation/station/available
        /// </summary>
        [HttpGet("available")]
        [SwaggerOperation(
            Summary = "Verfügbare Zentralen abrufen",
            Description = "Gibt eine Liste aller verfügbaren Digitalzentralen im aktuellen Workspace zurück."
        )]
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAvailableStations()
        {
            try
            {
                var stations = await CsService.GetAvailableStationsAsync();
                return Ok(stations);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        [HttpGet("info/{driverName}")]
        [SwaggerOperation(
            Summary = "Zentralen-Info abrufen",
            Description = "Gibt Informationen zu einer bestimmten Zentrale zurück."
        )]
        [ProducesResponseType(typeof(ControlStationInfo), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetStationInfo(string driverName)
        {
            if (string.IsNullOrWhiteSpace(driverName))
                return BadRequest("Parameter 'driverName' darf nicht leer sein.");

            try
            {
                var info = await CsService.GetInfoAsync(driverName);
                return Ok(info);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        [HttpPost("power/{driverName}/{on}")]
        [SwaggerOperation(
            Summary = "Fahrstrom schalten",
            Description = "Schaltet die Fahrstromversorgung einer Zentrale ein oder aus."
        )]
        [ProducesResponseType(typeof(bool), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> SetPower(string driverName, bool on)
        {
            if (string.IsNullOrWhiteSpace(driverName))
                return BadRequest("Parameter 'driverName' darf nicht leer sein.");

            try
            {
                var result = await CsService.SetPowerAsync(driverName, on);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        [HttpGet("power/status/{driverName}")]
        [SwaggerOperation(
            Summary = "Fahrstromstatus abfragen",
            Description = "Gibt zurück, ob der Fahrstrom für eine bestimmte Zentrale ein- oder ausgeschaltet ist."
        )]
        [ProducesResponseType(typeof(bool?), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetPowerStatus(string driverName)
        {
            if (string.IsNullOrWhiteSpace(driverName))
                return BadRequest("Parameter 'driverName' darf nicht leer sein.");

            try
            {
                var status = await CsService.GetPowerStatusAsync(driverName);
                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

    }
}

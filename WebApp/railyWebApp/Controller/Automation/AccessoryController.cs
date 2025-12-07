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
    public class AccessoryController : RailhqAutomationBase
    {
        private AccessoryService _accService;
        private AccessoryService AccService
        {
            get
            {
                if (_accService == null)
                    _accService = new AccessoryService(Uid);
                return _accService;
            }
        }

        public AccessoryController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        // === Verwaltung ===

        /// <summary>
        /// Alle Accessories abrufen
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Alle Accessories abrufen", OperationId = "GetAllAccessories")]
        [SwaggerResponse(200, "Liste aller Accessories", typeof(IEnumerable<Accessory>))]
        [SwaggerResponse((int)ErrorCode.AccessoryNotFound, "Keine Accessories gefunden")]
        [SwaggerResponse(500, "Serverfehler", typeof(ApiError))]
        public async Task<IActionResult> GetAllAccessories()
        {
            try
            {
                var status = await AccService.GetAll();
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.AccessoryNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Einzelnes Accessory abrufen
        /// </summary>
        /// <param name="driverName">Name des Treibers</param>
        /// <param name="address">Adresse des Accessories</param>
        [HttpGet("{driverName}/{address}")]
        [SwaggerOperation(Summary = "Einzelnes Accessory abrufen", OperationId = "GetAccessory")]
        [SwaggerResponse(200, "Accessory gefunden", typeof(Accessory))]
        [SwaggerResponse((int)ErrorCode.AccessoryNotFound, "Accessory nicht gefunden")]
        [SwaggerResponse(500, "Serverfehler", typeof(ApiError))]
        public async Task<IActionResult> GetAccessory(string driverName, int address)
        {
            try
            {
                var status = await AccService.Get(driverName, address);
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.AccessoryNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        // === Steuerung ===

        /// <summary>
        /// Accessory schalten (switch)
        /// </summary>
        /// <param name="driverName">Name des Treibers</param>
        /// <param name="address">Adresse des Accessories</param>
        /// <param name="command">Steuerungsbefehl zum Schalten</param>
        [HttpPost("{driverName}/{address}/switch")]
        [SwaggerOperation(Summary = "Accessory schalten (switch)", OperationId = "SwitchAccessory")]
        [SwaggerResponse(200, "Schaltvorgang erfolgreich")]
        [SwaggerResponse((int)ErrorCode.AccessoryNotFound, "Accessory nicht gefunden")]
        [SwaggerResponse(500, "Serverfehler", typeof(ApiError))]
        public async Task<IActionResult> SwitchAccessory(string driverName, int address, [FromBody] SwitchCommand command)
        {
            try
            {
                var status = await AccService.Switch(driverName, address, command.TargetState.ToString());
                if (!status)
                    return ApiError.NotFound(this, ErrorCode.AccessoryNotFound);

                return Ok();
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }
    }
}

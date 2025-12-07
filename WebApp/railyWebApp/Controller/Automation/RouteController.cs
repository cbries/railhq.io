// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebApp.Controller.Automation.Dto;
using railyWebApp.Controller.Automation.Services.Impl;
using System;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class RouteController : RailhqAutomationBase
    {
        private RouteService _routeService;

        private RouteService RouteSrvic
        {
            get
            {
                if (_routeService == null)
                    _routeService = new RouteService(Uid);
                return _routeService;
            }
        }

        public RouteController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        // === Verwaltung ===

        /// <summary>
        /// Gibt eine Liste aller definierten Fahrstraßen im aktuellen Workspace zurück.
        /// Wird z. B. für UI-Elemente wie Fahrstraßensteuerung oder Automatisierung verwendet.
        /// </summary>
        /// <returns>Liste aller Fahrstraßen als JSON</returns>
        /// <response code="200">Erfolgreich geladen</response>
        /// <response code="404">Keine Fahrstraßen vorhanden</response>
        /// <response code="500">Interner Fehler</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType((int)ErrorCode.RoutesNotAvailable)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllRoutes()
        {
            try
            {
                var routes = await RouteSrvic.GetAll();
                if (routes == null)
                    return ApiError.NotFound(this, ErrorCode.RoutesNotAvailable);

                return Ok(routes);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }
    }
}

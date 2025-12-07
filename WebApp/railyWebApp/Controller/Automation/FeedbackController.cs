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
    public class FeedbackController : RailhqAutomationBase
    {
        private FeedbackService _feedbackService;
        private FeedbackService FeedbackService
        {
            get
            {
                if (_feedbackService == null)
                    _feedbackService = new FeedbackService(Uid);
                return _feedbackService;
            }
        }

        public FeedbackController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        // === Verwaltung ===

        /// <summary>
        /// Gibt eine Liste aller bekannten Feedback-Module zurück.
        /// </summary>
        /// <returns>Liste aller Feedbacks (z. B. S88-Rückmelder).</returns>
        [HttpGet]
        [SwaggerOperation(
            Summary = "Alle Feedback-Module abrufen",
            Description = "Liefert eine vollständige Liste aller registrierten Feedback-Module inklusive deren Status.",
            OperationId = "GetAllFeedbacks"
        )]
        [ProducesResponseType(typeof(IEnumerable<Feedback>), 200)]
        [ProducesResponseType((int)ErrorCode.FeedbackNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAllFeedbacks()
        {
            try
            {
                var status = await FeedbackService.GetAll();
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.FeedbackNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Gibt ein bestimmtes Feedback-Modul zurück.
        /// </summary>
        /// <param name="driverName">Name des Treibers (z. B. „LDT“ oder „HSI88“).</param>
        /// <param name="module">Moduladresse (z. B. 1, 2, 3...).</param>
        /// <returns>Details zum Feedback-Modul.</returns>
        [HttpGet("{driverName}/{module}")]
        [SwaggerOperation(
            Summary = "Einzelnes Feedback-Modul abrufen",
            Description = "Liefert Statusinformationen zu einem spezifischen Feedback-Modul anhand von Treibername und Modulnummer.",
            OperationId = "GetFeedback"
        )]
        [ProducesResponseType(typeof(Feedback), 200)]
        [ProducesResponseType((int)ErrorCode.FeedbackNotFound)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetFeedback(string driverName, int module)
        {
            try
            {
                var status = await FeedbackService.Get(driverName, module);
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.FeedbackNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }
    }
}

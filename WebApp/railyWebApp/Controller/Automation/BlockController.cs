// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebApp.Controller.Automation.Dto;
using railyWebApp.Controller.Automation.Dto.Entity;
using railyWebApp.Controller.Automation.Services.Impl;
using Swashbuckle.AspNetCore.Annotations;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation
{
    [TypeFilter(typeof(RailhqAuthenticationFilter))]
    [ApiController]
    [Route("api/v1/automation/[controller]")]
    public class BlockController : RailhqAutomationBase
    {
        private BlockService _blockService;
        private BlockService BlockSrvic
        {
            get
            {
                if (_blockService == null)
                    _blockService = new BlockService(Uid);
                return _blockService;
            }
        }

        public BlockController(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
            // ignore
        }

        // === Verwaltung ===

        /// <summary>
        /// Alle Blöcke abrufen
        /// </summary>
        [HttpGet]
        [SwaggerOperation(Summary = "Alle Blöcke abrufen", OperationId = "GetAllBlocks")]
        [SwaggerResponse(200, "Liste aller Blöcke", typeof(IEnumerable<Block>))]
        [SwaggerResponse((int)ErrorCode.BlockNotFound, "Keine Blöcke gefunden")]
        [SwaggerResponse(500, "Serverfehler", typeof(ApiError))]
        public async Task<IActionResult> GetAllBlocks()
        {
            try
            {
                var status = await BlockSrvic.GetAll();
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.BlockNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }

        /// <summary>
        /// Einzelnen Block nach Name abrufen
        /// </summary>
        /// <param name="blockName">Name des Blocks</param>
        [HttpGet("{blockName}")]
        [SwaggerOperation(Summary = "Einzelnen Block abrufen", OperationId = "GetBlock")]
        [SwaggerResponse(200, "Liste aller Blöcke", typeof(Block))]
        [SwaggerResponse((int)ErrorCode.BlockNotFound, "Keine Blöcke gefunden")]
        [SwaggerResponse(500, "Serverfehler", typeof(ApiError))]
        public async Task<IActionResult> GetBlock(string blockName)
        {
            try
            {
                var status = await BlockSrvic.Get(blockName);
                if (status == null)
                    return ApiError.NotFound(this, ErrorCode.BlockNotFound);

                return Ok(status);
            }
            catch (Exception ex)
            {
                return ApiError.StatusCode(this, ex);
            }
        }
    }
}

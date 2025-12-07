// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.LoggerDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class TravelingData : RailhqControllerBase
    {
        public TravelingData(SupabaseService authService, IMemoryCache cache) 
            : base(authService, cache)
        {
        }

        [HttpGet("locs")]
        public async Task<IActionResult> GetLocs()
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                {
                    var user = await ValideRequestBearer(HttpContext);
                    uid = user.Id;
                }

                var locLoggingDbDir = new DirectoryInfo(Path.Combine(libUserspace.Filesystem.LoggingBaseDir, uid));
                var locomotiveMetadata = new LocomotiveMetadata(locLoggingDbDir);
                var metadataShort = locomotiveMetadata.GetAllMetadataShort();

                return Ok(JsonConvert.SerializeObject(metadataShort, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpGet("schedules")]
        public async Task<IActionResult> GetSchedules(
            string selector,
            string startDate,
            string endDate)
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                {
                    var user = await ValideRequestBearer(HttpContext);
                    uid = user.Id;
                }

                // Wenn startDate nicht gesetzt ist, setze es auf 100 Jahre in der Vergangenheit, erste Sekunde des Tages
                var startDateTime = string.IsNullOrEmpty(startDate)
                    ? DateTime.UtcNow.AddYears(-100).Date.AddSeconds(1)  // 100 Jahre in der Vergangenheit, erste Sekunde des Tages
                    : DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddSeconds(1);  // 00:00:01

                // Wenn endDate nicht gesetzt ist, setze es auf das heutige Datum und die letzte Sekunde des Tages
                var endDateTime = string.IsNullOrEmpty(endDate)
                    ? DateTime.UtcNow.Date.AddDays(1).AddSeconds(-1)  // Heute bis 23:59:59
                    : DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture).AddDays(1).AddSeconds(-1);  // 23:59:59 des Tages

                var locLoggingDbDir = new DirectoryInfo(Path.Combine(libUserspace.Filesystem.LoggingBaseDir, uid));
                var fahrzeitenData = new LocomotiveLogger(locLoggingDbDir);
                var entries = await fahrzeitenData.GetTrainLogsAsync(selector, startDateTime, endDateTime, 1, -1);

                return Ok(JsonConvert.SerializeObject(entries, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }
}

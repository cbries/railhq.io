// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.LoggerDB;
using libUserspace.LoggerDB.PODs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoggerData : RailhqControllerBase
    {
        public LoggerData(SupabaseService authService, IMemoryCache cache) 
            : base(authService, cache)
        {
            // ignore
        }

        private string GetName(List<MetadataShort> data, string driverName, int objectId)
        {
            foreach (var it in data)
            {
                if (it.ObjectId != objectId) continue;
                if (!it.DriverName.Equals(driverName)) continue;

                return it.DisplayName;
            }

            return "???";
        }

        [HttpGet]
        public async Task<IActionResult> Get(string driverName, int? objectId)
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                {
                    var user = await ValideRequestBearer(HttpContext);
                    uid = user.Id;
                }

                if (string.IsNullOrEmpty(driverName) || objectId == null)
                    return await GetAll(uid);

                return await GetSingle(uid, driverName, objectId.Value);
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        private async Task<IActionResult> GetSingle(string uid, string driverName, int objectId)
        {
            var locLoggingDbDir = new DirectoryInfo(Path.Combine(libUserspace.Filesystem.LoggingBaseDir, uid));
            var locomotiveLogger = new LocomotiveLogger(locLoggingDbDir);
            var data = locomotiveLogger.GetTrainStatisticsAsJson($"{driverName}_{objectId}");
            return Ok(JsonConvert.SerializeObject(data, Formatting.Indented));
        }

        private async Task<IActionResult> GetAll(string uid)
        {
            var locLoggingDbDir = new DirectoryInfo(Path.Combine(libUserspace.Filesystem.LoggingBaseDir, uid));
            var locomotiveLogger = new LocomotiveLogger(locLoggingDbDir);
            var stats = locomotiveLogger.GetAllTrainStatisticsAsJson();

            var locomotiveMetadata = new LocomotiveMetadata(locLoggingDbDir);
            var metadataShort = locomotiveMetadata.GetAllMetadataShort();

            var data = stats.ToObject<List<RideStats>>();
            foreach (var it in data)
            {
                var driverName0 = it.DriverName;
                var objectId0 = int.Parse(it.ObjectId);

                it.DisplayName = GetName(metadataShort, driverName0, objectId0);
            }

            return Ok(JsonConvert.SerializeObject(data, Formatting.Indented));
        }
    }
}

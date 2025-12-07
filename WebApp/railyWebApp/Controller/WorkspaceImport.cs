// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Converter.Rocrail;
using libShared;
using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    public class ImportData
    {
        [JsonProperty("format")] public string Format { get; set; }
        [JsonProperty("data")] public string DataBase64Encoded { get; set; }

        [JsonIgnore]
        public string DataDecoded
        {
            get
            {
                var fileBytes = Convert.FromBase64String(DataBase64Encoded);
                var textString = System.Text.Encoding.UTF8.GetString(fileBytes);
                return textString;
            }
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class WorkspaceImport : RailhqControllerBase
    {
        public WorkspaceImport(SupabaseService authService, IMemoryCache cache) 
            : base(authService, cache)
        {
            // ignore
        }

        public string OutputDirectory { get; set; }

        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] JsonElement data)
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                {
                    var user = await ValideRequestBearer(HttpContext);
                    uid = user.Id;
                }

                if (string.IsNullOrEmpty(uid))
                    return Unauthorized(new { error = "incorrect username/password combination" });

                var jsonString = data.GetRawText();
                if (string.IsNullOrEmpty(jsonString))
                    return StatusCode(500, new { error = "no data" });

                var importData = JsonConvert.DeserializeObject<ImportData>(jsonString);
                if (string.IsNullOrEmpty(importData.DataDecoded))
                    return StatusCode(500, new { message = "invalid data" });

                var outputDirectory = Path.Combine(libUserspace.Filesystem.WorkspacesBaseDir, uid);

                var importer = new ImportRocrail();
                var res = importer.ExecuteData(importData.DataDecoded, outputDirectory, out var errorMessage);
                if (!res)
                    return StatusCode(500, new { message = errorMessage });

                var wsBaseDir = libUserspace.Filesystem.WorkspacesBaseDir;
                var wsInfoInstance = new libUserspace.Info.Workspaces(wsBaseDir);
                await wsInfoInstance.Update(uid);

                return Ok(new { message = "success" });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }
    }
}

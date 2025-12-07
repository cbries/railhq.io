// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUtilities;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebApp.Controller.Helper;
using railyWebApp.Playground;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text.Json;
using System.Threading.Tasks;
using System.Xml.Linq;
using Helpers = railyWebApp.Controller.Helper.Helpers;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class Workspace : RailhqControllerBase
    {
        public Workspace(
            SupabaseService authService,
            IMemoryCache cache)
            : base(authService, cache)
        {
            // ignore
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                    uid = Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;
                var workspaceInfos = await libUserspace.Info.Workspaces.GetInfo(libUserspace.Filesystem.WorkspacesBaseDir, uid);
                return Ok(JsonConvert.SerializeObject(workspaceInfos, Formatting.Indented));
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("demo/restore")]
        public async Task<IActionResult> DemoRestore(string force, string userId)
        {
            try
            {
                bool forceRestore = false;
                if (!string.IsNullOrEmpty(force))
                {
                    if (force.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
                        forceRestore = true;
                    else if (force.Trim().Equals("1", StringComparison.Ordinal))
                        forceRestore = true;
                }

                var inputDir = Path.Combine(libUserspace.Filesystem.ResourceBaseDir, "_demo");
                if (!Directory.Exists(inputDir))
                    return NotFound(new { message = $"Restore data are not available" });

                //
                // source directories of the "demo"
                //
                var inputDirWorkspace = Path.Combine(inputDir, "Workspaces");
                var inputDirUserImages = Path.Combine(inputDir, "UserImages");

                var uid = GetCachedValue(SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                    uid = userId;
                if (string.IsNullOrEmpty(uid))
                    uid = Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;

                //
                // copy demo workspace
                //
                var workspaceBaseDir = string.Empty;
                if (!string.IsNullOrEmpty(uid))
                    workspaceBaseDir = Path.Combine(libUserspace.Filesystem.WorkspacesBaseDir, uid);
                if (!Directory.Exists(workspaceBaseDir))
                    await Helper.FileHelper.CopyDirectoryAsync(inputDirWorkspace, workspaceBaseDir);
                else if (Directory.Exists(workspaceBaseDir) && forceRestore)
                    await Helper.FileHelper.CopyDirectoryAsync(inputDirWorkspace, workspaceBaseDir);

                //
                // copy demo userimages
                //
                var userimagesBaseDir = string.Empty;
                if (!string.IsNullOrEmpty(uid))
                    userimagesBaseDir = Path.Combine(libUserspace.Filesystem.UserImagesBaseDir, uid);
                if (!Directory.Exists(userimagesBaseDir))
                    await Helper.FileHelper.CopyDirectoryAsync(inputDirUserImages, userimagesBaseDir);
                else if (Directory.Exists(userimagesBaseDir) && forceRestore)
                    await Helper.FileHelper.CopyDirectoryAsync(inputDirUserImages, userimagesBaseDir);

                // refresh workspace file
                var wsBaseDir = libUserspace.Filesystem.WorkspacesBaseDir;
                var wsInfoInstance = new libUserspace.Info.Workspaces(wsBaseDir);
                await wsInfoInstance.Update(uid);

                return Ok(new { message = "Demo restore successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error during demo restore.", error = ex.Message });
            }
        }

        [HttpGet("screenshot")]
        public async Task<IActionResult> GetScreenshot(string wsName)
        {
            try
            {
                var uid = GetCachedValue(SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                    uid = Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;
                var dirpath = Path.Combine(libUserspace.Filesystem.WorkspacesBaseDir, uid);
                var pathScreenshot = Path.Combine(dirpath, wsName, "screenshot.png");
                if (!System.IO.File.Exists(pathScreenshot))
                    return GetFallbackImage();

                return PhysicalFile(pathScreenshot, "image/png");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        private FileContentResult GetFallbackImage()
        {
            var data = Helpers.GetFallbackImage(new List<string>
            {
                " ",
                " ",
                "Vorschau noch nicht erstellt!"
            }, 500, 300);
            var bytes = data.ToArray();
            data?.Dispose();
            return File(bytes, "image/png");
        }

        private async Task UnloadWorkspaceAndInformUser(string uid, string workspaceName)
        {
            if (string.IsNullOrEmpty(uid)) return;
            if (string.IsNullOrEmpty(workspaceName)) return;

            var wsres = Globals.UserWorkspaces.TryGetValue(uid, out var userWorkspace);
            if (!wsres)
            {
                // no workspace found

                return;
            }

            if (userWorkspace.Name.Equals(workspaceName))
            {
                var resUnload = userWorkspace.Unload();
                if (!resUnload)
                    Logging.Log.Info($"Unload of workspace {workspaceName} for Uid({uid}) failed.");

                var errMsg = $"The workspace is renamed/deleted. Your session is not valid anymore. Please check your desktop browser for any information. This session will be closed now.";
                Logging.Log.Info(errMsg);
                var data1 = new JObject
                {
                    ["command"] = "fatal",
                    ["info"] = errMsg
                };

                //var connections = ConnectionManager.GetConnection(uid);
                //var browserConnections = connections?.BrowserSockets ?? new List<WebSocket>();

                await WebSocketModule.DataExchange.SendObjectToAllClients(uid, data1);
                await WebSocketModule.DataExchange.CloseClients(uid, "workspace changed", new List<WebSocket>());
            }
        }

        [HttpPost]
        public async Task<IActionResult> PostData([FromBody] JsonElement data)
        {
            try
            {
                var uid = Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                    uid = Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;

                var jsonString = data.GetRawText();
                if (string.IsNullOrEmpty(jsonString))
                    return StatusCode(500, new { error = "invalid data" });

                //
                // rename:
                // { command: 'wsRename', name: string, newName: string }
                //
                // delete:
                // { command: 'wsDelete', name: string }
                //
                // refresh:
                // { command: 'wsRefresh', name: string }
                //
                // copy:
                // { command: 'wsCopy', name: string, newName: string (optional) }
                //

                var jsonData = JObject.Parse(jsonString);
                var command = jsonData.GetString("command");
                var name = jsonData.GetString("name");
                var newName = jsonData.GetString("newName");

                var wsBaseDir = libUserspace.Filesystem.WorkspacesBaseDir;
                var wsInfoInstance = new libUserspace.Info.Workspaces(wsBaseDir);

                try
                {
                    switch (command.ToLower())
                    {
                        case "wsrename":
                            {
                                var webSafeNewName = FileNameHelper.ToWebSafeFileName(newName, false);

                                await UnloadWorkspaceAndInformUser(uid, name);
                                await wsInfoInstance.RenameWorkspace(uid, name, webSafeNewName);
                                await wsInfoInstance.Update(uid);
                            }
                            break;

                        case "wsdelete":
                            {
                                await UnloadWorkspaceAndInformUser(uid, name);
                                await wsInfoInstance.DeleteWorkspace(uid, name);
                                await wsInfoInstance.Update(uid);
                            }
                            break;

                        case "wsrefresh":
                            {
                                await wsInfoInstance.Update(uid);
                            }
                            break;

                        case "wscopy":
                            {
                                var webSafeNewName = newName;
                                if (!string.IsNullOrEmpty(webSafeNewName))
                                    webSafeNewName = FileNameHelper.ToWebSafeFileName(newName, false);
                                await wsInfoInstance.CopyWorkspace(uid, name, webSafeNewName);
                                await wsInfoInstance.Update(uid);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { message = ex.Message });
                }

                return Ok(new { message = "success" });
            }
            catch (Exception ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
        }

        [HttpPost("emergencyStop")]
        [EnableCors("AllowAll")]
        public async Task<IActionResult> StopWorkspace([FromBody] StopRequest request)
        {
            try
            {
                var uid = GetCachedValue(SessionGlobals.AuthUid, HttpContext);
                if (string.IsNullOrEmpty(uid))
                    uid = Automation.RailhqAuthenticationFilter.LocalAnonymousUserId;
                await PgHelper.SendToGateway(uid, BaseCommands.CmdEmergencyStop);
                return Ok(new { message = string.Empty });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        public class StopRequest
        {
            public string Token { get; set; }
        }
    }
}

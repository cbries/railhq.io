// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libShared.Web;
using libUtilities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

// ReSharper disable UnusedMember.Global

namespace railyWebApp
{
    public class LocomotiveRequest
    {
        public static LocomotiveRequest Parse(HttpRequest req)
        {
            var instance = new LocomotiveRequest();
            instance.ParseRequest(req);
            return instance;
        }

        [JsonProperty("driverName")] public string DriverName { get; set; }
        [JsonProperty("objectId")] public int ObjectId { get; set; }
        [JsonProperty("uid")] public string Uid { get; set; }
        [JsonProperty("workspace")] public string Workspace { get; set; }
        [JsonProperty("hasService")] public bool HasService { get; set; }

        public void ParseRequest(HttpRequest req)
        {
            foreach (var it in req.Query)
            {
                if (string.IsNullOrEmpty(it.Key)) continue;

                switch (it.Key.ToLower())
                {
                    case "drivername": DriverName = it.Value.ToString().Trim(); break;
                    case "objectid":
                        {
                            var dummyStr = it.Value.ToString();
                            if (int.TryParse(dummyStr, out var v))
                                ObjectId = v;
                            else
                                ObjectId = -1;
                        }
                        break;
                    case "uid": Uid = it.Value.ToString().Trim(); break;
                    case "workspace": Workspace = it.Value.ToString().Trim(); break;
                    case "hasservice":
                        {
                            var dummyStr = it.Value.ToString();
                            if (bool.TryParse(dummyStr, out var v))
                                HasService = v;
                            else
                                HasService = false;
                        }
                        break;
                }
            }
        }
    }

    public class AuthData
    {
        public string AuthToken { get; set; } = string.Empty;
        public string EMail { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;
        public bool HasService { get; set; } = true; // always true !!! since 2025-08-10
    }

    public class HomeModule
    {
        private static async Task LoadSession(HttpContext context)
        {
            try
            {
                if (!context.Session.IsAvailable)
                {
                    await context.Session.LoadAsync();
                }
            }
            catch (TimeoutException ex)
            {
                Logging.Log.Info("Session konnte nicht geladen werden: " + ex.Message);
            }
            catch (Exception ex)
            {
                Logging.Log.Info("Ein Fehler ist beim Laden der Session aufgetreten: " + ex.Message);
            }
        }

        private static async Task<AuthData> GetSessionData(HttpContext context, IMemoryCache cache)
        {
            await LoadSession(context);

            var authData = new AuthData
            {
                AuthToken = Controller.Helper.CacheHelper.GetCachedValue(cache, SessionGlobals.AuthUserSession, context),
                EMail = Controller.Helper.CacheHelper.GetCachedValue(cache, SessionGlobals.AuthEmail, context),
                Uid = Controller.Helper.CacheHelper.GetCachedValue(cache, SessionGlobals.AuthUid, context)
            };

            return authData;
        }

        public static async Task HandleFiles(HttpContext context, IMemoryCache cache)
        {
            var filepath = context.Request.Path.Value.TrimStart('/');
            if (string.IsNullOrEmpty(filepath)) filepath = HomeModuleShared.DefaultFile;

            //
            // check "workspace" und speichere den Namen bei Bedarf zwischen
            //
            if (true)
            {
                const string wsKeyName = "workspace";
                var wsName = context.Request.Query[wsKeyName].ToString();
                if (string.IsNullOrEmpty(wsName)
                    && filepath.Equals(HomeModuleShared.DefaultFile, StringComparison.OrdinalIgnoreCase))
                {
                    Controller.Helper.CacheHelper.RemoveCachedValue(
                        cache,
                        SessionGlobals.SessionWorkspaceName, 
                        context);
                }
                else
                {
                    Controller.Helper.CacheHelper.SetCachedValue(
                        cache,
                        SessionGlobals.SessionWorkspaceName,
                        wsName,
                        context);
                }
            }

            if (!HomeModuleShared.HasVirtualMap(filepath, out var infoPath))
            {
                var fullPath = Path.Combine(Globals.HttpRootDir, filepath);
                infoPath = new FileInfo(fullPath);
            }

            if (infoPath.Exists)
            {
                byte[] fileBytes;

                if (libUtilities.Filesystem.IsSubst(infoPath.FullName, HomeModuleShared.FilesForSubstitution))
                {
                    // apply access token
                    var sessionData = await GetSessionData(context, cache);

                    var cnt = await HomeModuleShared.DoSubstitutions(infoPath.FullName, sessionData);

                    Logging.Log.Debug($"Substitude: {infoPath.Name}");
                    if (string.IsNullOrEmpty(sessionData.AuthToken))
                        Logging.Log.Debug("HTTP session data not available");
                    else
                        Logging.Log.Debug($"Session: {sessionData.EMail}, {sessionData.AuthToken}");

                    cnt = cnt.Replace("##access_token##", sessionData.AuthToken);
                    cnt = cnt.Replace("##access_email##", sessionData.EMail);
                    cnt = cnt.Replace("##access_uid##", sessionData.Uid);
                    cnt = cnt.Replace("##access_hasService##", $"{sessionData.HasService}");

                    fileBytes = Encoding.UTF8.GetBytes(cnt);
                }
                else
                {
                    fileBytes = await File.ReadAllBytesAsync(infoPath.FullName);
                }

                var fileExtension = Path.GetExtension(infoPath.FullName);
                var contentType = ContentType.GetContentType(fileExtension);
                if (contentType.StartsWith("image", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleImageRequest(context, infoPath.FullName);
                }
                else
                {
                    context.Response.ContentType = contentType;
                    await context.Response.Body.WriteAsync(fileBytes);
                }
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File not found: {filepath}");
            }
        }

        public static async Task HandleImageRequest(HttpContext context, string filePath)
        {
            if (!System.IO.File.Exists(filePath))
            {
                context.Response.StatusCode = StatusCodes.Status404NotFound;
                return;
            }
            context.Response.ContentType = "image/png";
            context.Response.Headers["Cache-Control"] = "public, max-age=31536000"; // Caching aktivieren
            await context.Response.SendFileAsync(filePath);
        }

        public static async Task HandleLocomotive(HttpContext context, IMemoryCache cache)
        {
            var request = context.Request;
            
            //var requestData = LocomotiveRequest.Parse(request);
            
            var filepath = request.Path.Value.TrimStart('/');
            if (string.IsNullOrEmpty(filepath) || filepath.Equals("locomotive", StringComparison.OrdinalIgnoreCase))
                filepath = HomeModuleShared.DefaultLocomotveFile;

            if (!HomeModuleShared.HasVirtualMap(filepath, out var infoPath))
            {
                var fullPath = Path.Combine(Globals.HttpRootDir, filepath);
                infoPath = new FileInfo(fullPath);
            }

            if (infoPath.Exists)
            {
                byte[] fileBytes;

                if (libUtilities.Filesystem.IsSubst(infoPath.FullName, HomeModuleShared.FilesForSubstitution))
                {
                    var authData = await GetSessionData(context, cache);
                    var cnt = await HomeModuleShared.DoSubstitutions(infoPath.FullName, authData);
                    fileBytes = Encoding.UTF8.GetBytes(cnt);
                }
                else
                {
                    fileBytes = await File.ReadAllBytesAsync(infoPath.FullName);
                }

                var fileExtension = Path.GetExtension(infoPath.FullName);
                var contentType = ContentType.GetContentType(fileExtension);
                context.Response.ContentType = contentType;
                await context.Response.Body.WriteAsync(fileBytes);
            }
            else
            {
                context.Response.StatusCode = 404;
                await context.Response.WriteAsync($"File not found: {filepath}");
            }
        }
    }
}

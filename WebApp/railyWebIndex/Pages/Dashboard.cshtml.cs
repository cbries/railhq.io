// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.Sbase;
using libUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using railyWebIndex.Pages.Auth;
using railyWebIndex.Pages.Database;
using railyWebIndex.Pages.Workspaces.Helper;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages
{

    public class DashboardModel : PageModelBase
    {
        private readonly ILogger<DashboardModel> _logger;
        private readonly WebAppServiceApi _serviceApi;

        [BindProperty(SupportsGet = true)] public string Uid { get; private set; }
        [BindProperty(SupportsGet = true)] public string WelcomeName { get; protected set; }
        [BindProperty(SupportsGet = true)] public string Acronym { get; protected set; }

        [BindProperty(SupportsGet = true)] public string ServiceUrl => Globals.ServiceUrl;
        [BindProperty(SupportsGet = true)] public string ApiUrl => $"{Globals.ServiceUrl}/api/Workspace";
        [BindProperty(SupportsGet = true)] public string UrlToPlay => $"{Globals.ServiceUrl}/?workspace=";
        [BindProperty] public List<libUserspace.Info.WorkspaceInfo> WorkspaceInfos { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string BearerToken => base.AccessToken;

        /// <summary>
        /// Latest version number from assembly
        /// </summary>
        [BindProperty(SupportsGet = true)] public string LatestVersion { get; private set; }

        [BindProperty(SupportsGet = true)] public string LastLogin { get; private set; }

        public DashboardModel(
            SupabaseService authService,
            ILogger<DashboardModel> logger,
            IMemoryCache cache,
            WebAppServiceApi serviceApi)
            : base(authService, cache)
        {
            _logger = logger;
            _serviceApi = serviceApi;
        }

        private async Task LoadUserName(DbUser dbUser)
        {
            var cachedWelcomeName =
                railyWebApp.Controller.Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.SessionWelcomeName,
                    HttpContext);

            if (string.IsNullOrEmpty(cachedWelcomeName))
            {
                WelcomeName = await dbUser.GetWelcomeName();
                Acronym = await dbUser.GetAcronym();
                var localUid = dbUser.Uid;
                if (!string.IsNullOrEmpty(localUid))
                    Uid = localUid;

                railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.SessionWelcomeName,
                    WelcomeName, HttpContext);
                railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.SessionAcronym, Acronym,
                    HttpContext);
                railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthUid, Uid,
                    HttpContext);
            }
            else
            {
                WelcomeName = cachedWelcomeName;
                Acronym = railyWebApp.Controller.Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.SessionAcronym,
                    HttpContext);
                Uid = railyWebApp.Controller.Helper.CacheHelper.GetCachedValue(Cache, SessionGlobals.AuthUid,
                    HttpContext);
            }
        }

        public async Task<IActionResult> OnGet()
        {
            var dbUser = new DbUser(AuthService, RailhqUser);

            await LoadUserName(dbUser);

            LastLogin = dbUser.LastLogin;

            var nativeJsonArray = await WorkspaceInfo.QueryWorkspaceInfo(BearerToken);
            WorkspaceInfos = nativeJsonArray.ToObject<List<libUserspace.Info.WorkspaceInfo>>();

            // Get version from assembly (set at build time via Directory.Build.props)
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            LatestVersion = version != null 
                ? $"{version.Major}.{version.Minor}" 
                : "1.61";
            
            return Page();
        }
    }

}
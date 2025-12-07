// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using libUtilities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using railyWebIndex.Pages.Auth;
using railyWebIndex.Pages.Workspaces.Helper;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Workspaces
{
    public class OverviewModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)] public string ApiUrl => $"{Globals.ServiceUrl}/api/Workspace";
        [BindProperty(SupportsGet = true)] public string ApiUrlImport => $"{Globals.ServiceUrl}/api/WorkspaceImport";
        [BindProperty(SupportsGet = true)] public string UrlToPlay => $"{Globals.ServiceUrl}/?workspace=";
        [BindProperty] public List<libUserspace.Info.WorkspaceInfo> WorkspaceInfos { get; set; } = new();
        [BindProperty(SupportsGet = true)] public string BearerToken => base.AccessToken;

        public OverviewModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            var nativeJsonArray = await WorkspaceInfo.QueryWorkspaceInfo(BearerToken);
            WorkspaceInfos = nativeJsonArray.ToObject<List<libUserspace.Info.WorkspaceInfo>>();
            return Page();
        }
    }
}

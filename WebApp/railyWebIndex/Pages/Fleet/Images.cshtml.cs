// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libShared;
using libUserspace;
using libUserspace.LoggerDB.PODs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebIndex.Pages.Auth;
using railyWebIndex.Pages.Fleet.Helper;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Fleet
{
    public class ImagesModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)] public string BearerToken => base.AccessToken;
        [BindProperty] public string ServiceUrl => Globals.ServiceUrl;
        [BindProperty] public List<MetadataShort> LocsData { get; set; } = new();

        public ImagesModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            // Get user ID from current authenticated user
            var uid = AuthService.CurrentUser?.Id;
            if(uid != null)
                railyWebApp.Controller.Helper.CacheHelper.SetCachedValue(Cache, SessionGlobals.AuthUid, uid, HttpContext);

            var nativeJsonArray = await StatisticsHelper.QueryLocsData(BearerToken);
            LocsData = nativeJsonArray.ToObject<List<MetadataShort>>();

            return Page();
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using libUserspace.LoggerDB.PODs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using railyWebIndex.Pages.Auth;
using railyWebIndex.Pages.Fleet.Helper;
using System.Collections.Generic;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages.Fleet
{
    public class LocomotivesModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)] public string BearerToken => base.AccessToken;
        [BindProperty] public List<RideStats> RideStats { get; set; } = new();
        [BindProperty] public string ServiceUrl => Globals.ServiceUrl;

        public LocomotivesModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }
        
        public async Task<IActionResult> OnGet()
        {
            var nativeJsonArray = await StatisticsHelper.QueryStatisticsData(BearerToken);
            RideStats = nativeJsonArray.ToObject<List<RideStats>>();
            return Page();
        }
    }
}

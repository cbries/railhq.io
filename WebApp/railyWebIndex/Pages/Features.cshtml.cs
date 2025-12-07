// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using railyWebIndex.Pages.Auth;
using System.Collections.Generic;
using System.Threading.Tasks;
using libUtilities;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex.Pages
{
    public class FeaturesModel : PageModelBase
    {
        [BindProperty(SupportsGet = true)] public List<RoadmapStep> Roadmap { get; private set; } = new();

        [BindProperty(SupportsGet = true)] public List<string> Colors { get; set; } = new();

        public FeaturesModel(SupabaseService authService, IMemoryCache cache) : base(authService, cache)
        {
        }

        public async Task<IActionResult> OnGet()
        {
            Colors = ["bg-gray-100", "bg-blue-100", "bg-green-100"];

            var loader = new JsonLoader<List<RoadmapStep>>(
                Globals.UrlRoadmapListFiles,
                Globals.RoadmapName,
                msg => ViewData["errorMessage"] = msg
            );

            Roadmap = await loader.LoadAsync();

            return Page();
        }

        public class Feature
        {
            public string GetIconHtml()
            {
                if (string.IsNullOrEmpty(Icon)) return string.Empty;
                return $"<i class=\"{Icon}\"></i>&nbsp;&nbsp;";
            }

            [JsonProperty("icon")] public string Icon { get; set; } = string.Empty;
            [JsonProperty("name")] public string Name { get; set; } = string.Empty;
            [JsonProperty("description")] public string Description { get; set; } = string.Empty;
        }

        public class RoadmapStep
        {
            [JsonProperty("step")] public string Step { get; set; } = string.Empty;
            [JsonProperty("features")] public List<Feature> Features { get; set; } = new();
        }
    }
}

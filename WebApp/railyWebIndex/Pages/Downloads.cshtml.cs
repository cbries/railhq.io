// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Threading.Tasks;

namespace railyWebIndex.Pages
{
    public class DownloadsModel : PageModel
    {
        /// <summary>
        /// Latest version number from assembly
        /// </summary>
        [BindProperty(SupportsGet = true)] public string LatestVersion { get; private set; }
        
        public Task<IActionResult> OnGet()
        {
            // Get version from assembly (set at build time via Directory.Build.props)
            var assembly = Assembly.GetExecutingAssembly();
            var version = assembly.GetName().Version;
            LatestVersion = version != null 
                ? $"{version.Major}.{version.Minor}" 
                : "1.61";

            return Task.FromResult<IActionResult>(Page());
        }
    }
}

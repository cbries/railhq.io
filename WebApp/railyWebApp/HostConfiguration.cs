// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Configuration;

namespace railyWebApp
{
    /// <summary>
    /// Configuration class for host settings.
    /// Values are loaded from appsettings.json "Host" section.
    /// </summary>
    public class HostConfiguration
    {
        /// <summary>
        /// The main domain without leading dot (e.g., "railhq.io" or "localhost")
        /// </summary>
        public string Domain { get; set; } = "railhq.io";
        
        /// <summary>
        /// The cookie domain with leading dot for subdomain support (e.g., ".railhq.io")
        /// Set to null or empty for localhost.
        /// </summary>
        public string CookieDomain { get; set; } = ".railhq.io";
        
        /// <summary>
        /// Main website URL (e.g., "https://railhq.io/")
        /// </summary>
        public string WebsiteUrl { get; set; } = "https://localhost/";
        
        /// <summary>
        /// Wiki/Support URL (e.g., "https://railhq.io/Support")
        /// </summary>
        public string WikiUrl { get; set; } = "https://localhost/Support";
        
        /// <summary>
        /// Returns true if running in local development mode (localhost) or OnPremise mode
        /// </summary>
        public bool IsLocalhost
        {
            get
            {
                // Check for explicit local mode setting first
                var localMode = Environment.GetEnvironmentVariable("RAILHQ_LOCAL_MODE");
                if (!string.IsNullOrEmpty(localMode) && localMode.Equals("true", StringComparison.OrdinalIgnoreCase))
                    return true;
                
                return Domain?.ToLowerInvariant().Contains("localhost") == true 
                       || Domain == "127.0.0.1";
            }
        }
        
        /// <summary>
        /// Returns the effective cookie domain (null for localhost)
        /// </summary>
        public string EffectiveCookieDomain => IsLocalhost ? null : CookieDomain;
        
        // Static instance for easy access
        private static HostConfiguration _instance;
        
        /// <summary>
        /// Gets the current host configuration instance
        /// </summary>
        public static HostConfiguration Current => _instance ?? new HostConfiguration();
        
        /// <summary>
        /// Initialize the host configuration from IConfiguration
        /// </summary>
        public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _instance = configuration.GetSection("Host").Get<HostConfiguration>() ?? new HostConfiguration();
        }
    }
}

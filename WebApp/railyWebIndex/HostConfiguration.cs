// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace railyWebIndex
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
        public string Domain { get; set; } = "localhost";
        
        /// <summary>
        /// The cookie domain with leading dot for subdomain support (e.g., ".railhq.io")
        /// Set to null or empty for localhost.
        /// </summary>
        public string CookieDomain { get; set; } = ".localhost";
        
        /// <summary>
        /// Service host without protocol (e.g., "localhost" or "railhq.io")
        /// </summary>
        public string ServiceHost { get; set; } = "localhost";
        
        /// <summary>
        /// Service port (default: 5001)
        /// </summary>
        public int ServicePort { get; set; } = 5001;
        
        /// <summary>
        /// The full URL to the service/API - dynamically uses http/https based on RAILHQ_USE_TLS
        /// </summary>
        public string ServiceUrl => $"{HttpProtocol}://{ServiceHost}:{ServicePort}";
        
        /// <summary>
        /// Index host without protocol (e.g., "localhost" or "railhq.io")
        /// </summary>
        public string IndexHost { get; set; } = "localhost";
        
        /// <summary>
        /// Index port (optional, null means no port in URL)
        /// </summary>
        public int? IndexPort { get; set; } = 13443;
        
        /// <summary>
        /// The full URL to the index/landing page - dynamically uses http/https based on RAILHQ_USE_TLS
        /// </summary>
        public string IndexUrl => IndexPort.HasValue 
            ? $"{HttpProtocol}://{IndexHost}:{IndexPort}"
            : $"{HttpProtocol}://{IndexHost}";
        
        /// <summary>
        /// Base URL for list files (releases, videos, FAQ, etc.)
        /// Points to GitHub Raw Content for community edition.
        /// </summary>
        public string BaseUrlOfListFiles { get; set; } = "https://raw.githubusercontent.com/cbries/railhq.io/main/WebApp/railyWebIndex/wwwroot/";
        
        /// <summary>
        /// URL to the Swagger API documentation - dynamically derived from ServiceUrl
        /// </summary>
        public string SwaggerUrl => $"{ServiceUrl}/swagger/index.html";
        
        /// <summary>
        /// Website host without protocol (e.g., "localhost" or "railhq.io")
        /// </summary>
        public string WebsiteHost { get; set; } = "localhost";
        
        /// <summary>
        /// Main website URL - dynamically uses http/https based on RAILHQ_USE_TLS
        /// </summary>
        public string WebsiteUrl => $"{HttpProtocol}://{WebsiteHost}/";
        
        /// <summary>
        /// Wiki/Support URL - dynamically uses http/https based on RAILHQ_USE_TLS
        /// </summary>
        public string WikiUrl => $"{HttpProtocol}://{WebsiteHost}/Support";
        
        /// <summary>
        /// Returns true if running in local development mode (localhost)
        /// </summary>
        public bool IsLocalhost => Domain?.ToLowerInvariant().Contains("localhost") == true 
                                   || Domain == "127.0.0.1";
        
        /// <summary>
        /// Returns the effective cookie domain (null for localhost)
        /// </summary>
        public string EffectiveCookieDomain => IsLocalhost ? null : CookieDomain;
        
        /// <summary>
        /// Returns true if TLS should be used (based on RAILHQ_USE_TLS environment variable)
        /// </summary>
        public static bool UseTls
        {
            get
            {
                var useTls = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
                return !string.IsNullOrEmpty(useTls) && useTls.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        
        /// <summary>
        /// Returns "https" or "http" based on UseTls
        /// </summary>
        public static string HttpProtocol => UseTls ? "https" : "http";
        
        /// <summary>
        /// Returns "wss" or "ws" based on UseTls
        /// </summary>
        public static string WsProtocol => UseTls ? "wss" : "ws";
    }
}

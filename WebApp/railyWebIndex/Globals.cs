// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using Microsoft.Extensions.Configuration;

namespace railyWebIndex
{
    public class Globals
    {
        public const string DocumentationDirName = "documentation";
        public const string DocumentationDirPath = "/" + DocumentationDirName;

        public const string CertName = "railhq-local.pfx";

        public static string CompanyName = "riesolution";
        public static string AppName = "RailHQ";
        public static string ProductName = "railhq.io";
        public static string DomainName = "RailHQ.io";

        // Internal storage for host and port (without protocol)
        private static string _serviceHost = "localhost";
        private static int _servicePort = 5001;
        private static string _indexHost = "localhost";
        private static int? _indexPort = null;

        // Dynamic URL properties based on UseTls
        public static string ServiceUrl => $"{(UseTls ? "https" : "http")}://{_serviceHost}:{_servicePort}";
        public static string IndexUrl => _indexPort.HasValue 
            ? $"{(UseTls ? "https" : "http")}://{_indexHost}:{_indexPort}" 
            : $"{(UseTls ? "https" : "http")}://{_indexHost}";
        
        public static string Domain = "localhost";
        public static string CookieDomain = ".localhost";

        public static string PageDashboard = "/dashboard";
        public static string PageLogin = "/auth/login";
        public static string PageProfileEdit = "/profile/edit";
        public static string PageWorkspaceOverview = "/workspaces/overview";

        // GitHub Raw Content URL for JSON data files
        public static string BaseUrlOfListFiles = "https://raw.githubusercontent.com/cbries/railhq.io/main/WebApp/railyWebIndex/wwwroot/";

        public const string FaqName = "entriesFaq.json";
        public static string UrlFaqListFiles => BaseUrlOfListFiles + FaqName;

        public const string RoadmapName = "entriesRoadmap.json";
        public static string UrlRoadmapListFiles => BaseUrlOfListFiles + RoadmapName;
        
        /// <summary>
        /// Initialize globals from configuration.
        /// Call this during application startup.
        /// </summary>
        public static void InitializeFromConfiguration(IConfiguration configuration)
        {
            var hostConfig = configuration.GetSection("Host").Get<HostConfiguration>();
            if (hostConfig != null)
            {
                Domain = hostConfig.Domain ?? Domain;
                CookieDomain = hostConfig.CookieDomain ?? CookieDomain;
                
                // Parse ServiceUrl to extract host and port
                if (!string.IsNullOrEmpty(hostConfig.ServiceUrl))
                {
                    try
                    {
                        var uri = new Uri(hostConfig.ServiceUrl);
                        _serviceHost = uri.Host;
                        _servicePort = uri.Port;
                    }
                    catch
                    {
                        // Keep defaults if parsing fails
                    }
                }
                
                // Parse IndexUrl to extract host and port
                if (!string.IsNullOrEmpty(hostConfig.IndexUrl))
                {
                    try
                    {
                        var uri = new Uri(hostConfig.IndexUrl);
                        _indexHost = uri.Host;
                        // Only store port if it's explicitly specified (not default 80/443)
                        if (uri.Port != 80 && uri.Port != 443 && !uri.IsDefaultPort)
                        {
                            _indexPort = uri.Port;
                        }
                        else
                        {
                            _indexPort = null;
                        }
                    }
                    catch
                    {
                        // Keep defaults if parsing fails
                    }
                }
                
                BaseUrlOfListFiles = hostConfig.BaseUrlOfListFiles ?? BaseUrlOfListFiles;
                
                // Update DomainName for display
                if (!string.IsNullOrEmpty(hostConfig.Domain))
                {
                    DomainName = hostConfig.Domain;
                }
            }
        }
        
        /// <summary>
        /// Returns true if running on localhost or in local/OnPremise mode
        /// </summary>
        public static bool IsLocalhost
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
        public static string EffectiveCookieDomain => IsLocalhost ? null : CookieDomain;
        
        /// <summary>
        /// Returns the service port (default: 5001)
        /// </summary>
        public static int ServicePort => _servicePort;
        
        /// <summary>
        /// Returns true if TLS should be used (based on RAILHQ_USE_TLS environment variable)
        /// Default is false for local development compatibility.
        /// </summary>
        public static bool UseTls
        {
            get
            {
                var useTls = Environment.GetEnvironmentVariable("RAILHQ_USE_TLS");
                // Default to false for local development, set to "true" to enable TLS
                return !string.IsNullOrEmpty(useTls) && useTls.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;

namespace railyWebIndex.Helper
{
    /// <summary>
    /// Factory for creating HttpClient instances with proper SSL handling for local development.
    /// </summary>
    public static class HttpClientFactory
    {
        /// <summary>
        /// Checks if we're connecting to localhost (based on ServiceUrl or IndexUrl).
        /// </summary>
        private static bool IsLocalhostConnection =>
            Globals.ServiceUrl?.Contains("localhost", StringComparison.OrdinalIgnoreCase) == true ||
            Globals.ServiceUrl?.Contains("127.0.0.1") == true ||
            Globals.IndexUrl?.Contains("localhost", StringComparison.OrdinalIgnoreCase) == true ||
            Globals.IndexUrl?.Contains("127.0.0.1") == true;

        /// <summary>
        /// Creates an HttpClientHandler with SSL certificate validation disabled for localhost.
        /// </summary>
        public static HttpClientHandler CreateHandler()
        {
            var handler = new HttpClientHandler();
            
            // Allow self-signed certificates for local development
            if (IsLocalhostConnection)
            {
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            
            return handler;
        }

        /// <summary>
        /// Creates an HttpClientHandler with cookie support and SSL certificate validation disabled for localhost.
        /// </summary>
        public static HttpClientHandler CreateHandlerWithCookies(out CookieContainer cookieContainer)
        {
            cookieContainer = new CookieContainer();
            var handler = new HttpClientHandler
            {
                CookieContainer = cookieContainer,
                UseCookies = true
            };
            
            // Allow self-signed certificates for local development
            if (IsLocalhostConnection)
            {
                handler.ServerCertificateCustomValidationCallback = 
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            }
            
            return handler;
        }

        /// <summary>
        /// Creates an HttpClient with SSL certificate validation disabled for localhost.
        /// </summary>
        public static HttpClient CreateClient()
        {
            return new HttpClient(CreateHandler());
        }

        /// <summary>
        /// Creates an HttpClient with a specific base address and SSL certificate validation disabled for localhost.
        /// </summary>
        public static HttpClient CreateClient(string baseUrl)
        {
            var client = new HttpClient(CreateHandler())
            {
                BaseAddress = new Uri(baseUrl)
            };
            return client;
        }
    }
}

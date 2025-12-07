// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using railyWebIndex.Helper;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebIndex
{
    public class WebIndexServiceApi
    {
        private readonly HttpClient _httpClient;

        public WebIndexServiceApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private HttpClient GetCookieClient(string sessionId)
        {
            var cookieHost = Globals.IndexUrl;
            var baseUrl = Globals.IndexUrl;
            var domain = Globals.Domain;
            
            var handler = HttpClientFactory.CreateHandlerWithCookies(out var cookieContainer);
            var c = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
            var uri = new Uri(cookieHost);
            
            // For localhost, don't set domain on cookie
            var cookie = new Cookie(".RaillHQ.Session", sessionId)
            {
                Path = "/"
            };
            if (!Globals.IsLocalhost)
            {
                cookie.Domain = domain;
            }
            cookieContainer.Add(uri, cookie);
            return c;
        }

        private HttpClient GetClient()
        {
            var baseUrl = Globals.IndexUrl;
            
            var handler = HttpClientFactory.CreateHandlerWithCookies(out _);
            var c = new HttpClient(handler)
            {
                BaseAddress = new Uri(baseUrl)
            };
            return c;
        }
    }
}

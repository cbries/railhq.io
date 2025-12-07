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
    public class WebAppServiceApi
    {
        private readonly HttpClient _httpClient;

        public WebAppServiceApi(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private HttpClient GetCookieClient(string sessionId)
        {
            var cookieHost = Globals.ServiceUrl;
            var baseUrl = Globals.ServiceUrl;
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

        public async Task<string> CallDemoRestoreAsync(string sessionId)
        {
            using var cookieClient = GetCookieClient(sessionId);
            var response = await cookieClient.GetAsync($"/api/workspace/demo/restore");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Fehler: {response.StatusCode}");
            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> CallDemoRestore2Async(string sessionId, string userId)
        {
            using var cookieClient = GetCookieClient(sessionId);
            //
            // TODO temporary the user id is appended to the url
            //      can be done, because we are in backend and no one can see our request
            //
            var response = await cookieClient.GetAsync($"/api/workspace/demo/restore?force=1&userId={userId}");
            if (!response.IsSuccessStatusCode)
                throw new Exception($"Fehler: {response.StatusCode}");
            return await response.Content.ReadAsStringAsync();
        }
    }
}

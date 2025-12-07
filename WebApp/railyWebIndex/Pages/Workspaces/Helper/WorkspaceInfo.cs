// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using libUtilities;
using Newtonsoft.Json.Linq;
using railyWebIndex.Helper;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Threading.Tasks;
using System;

namespace railyWebIndex.Pages.Workspaces.Helper
{
    public class WorkspaceInfo
    {
        public static async Task<JArray> QueryWorkspaceInfo(string bearerToken)
        {
            var url = $"{Globals.ServiceUrl}/api/Workspace";
            using var client = HttpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseBody))
                        return JArray.Parse(responseBody);
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return [];
        }
    }
}

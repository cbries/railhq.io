// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUtilities;
using Newtonsoft.Json.Linq;
using railyWebIndex.Helper;
using System;
using System.Linq;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace railyWebIndex.Pages.Fleet.Helper
{
    public class StatisticsHelper
    {
        private static async Task<JArray> GetArrayData(string bearerToken, string url)
        {
            using var client = HttpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
            try
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        var array = JArray.Parse(responseBody);
                        var sortedArray = new JArray(array.OrderBy(obj => (string)obj["DisplayName"]));
                        return sortedArray;
                    }
                }
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return [];
        }

        public static async Task<JArray> QueryLocsData(string bearerToken)
        {
            var url = $"{Globals.ServiceUrl}/api/TravelingData/Locs";
            return await GetArrayData(bearerToken, url);
        }

        public static async Task<JArray> QueryStatisticsData(string bearerToken)
        {
            var url = $"{Globals.ServiceUrl}/api/LoggerData";
            return await GetArrayData(bearerToken, url);
        }
    }
}

// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Helper
{
    /// <summary>
    /// Health check for the authentication system.
    /// Now checks if the users.json file is accessible instead of Supabase.
    /// </summary>
    public class SupabaseHealthCheck
    {
        private const int CacheSeconds = 120;

        private readonly IConfiguration _configuration;
        private readonly IDistributedCache _cache;

        public SupabaseHealthCheck(IConfiguration configuration, IDistributedCache cache)
        {
            _configuration = configuration;
            _cache = cache;
        }

        public async Task<bool> CheckDatabaseAsync()
        {
            var cachedResult = await _cache.GetStringAsync("AuthHealth");
            if (cachedResult != null)
                return JsonSerializer.Deserialize<bool>(cachedResult);
            var isHealthy = await PerformHealthCheckAsync();
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(CacheSeconds)
            };
            await _cache.SetStringAsync("AuthHealth", JsonSerializer.Serialize(isHealthy), cacheOptions);
            return isHealthy;
        }

        private Task<bool> PerformHealthCheckAsync()
        {
            try
            {
                // Check if the users.json file exists and is readable
                var usersFilePath = Environment.GetEnvironmentVariable("USERS_FILE_PATH") 
                    ?? _configuration?["Auth:UsersFilePath"];
                
                if (string.IsNullOrEmpty(usersFilePath))
                {
                    var baseDir = libShared.RailEnvironment.GetBaseDirectory();
                    usersFilePath = Path.Combine(baseDir, "resources", "users.json");
                }

                if (!File.Exists(usersFilePath))
                    return Task.FromResult(false);

                // Try to read the file to ensure it's accessible
                var content = File.ReadAllText(usersFilePath);
                return Task.FromResult(!string.IsNullOrEmpty(content));
            }
            catch
            {
                return Task.FromResult(false);
            }
        }
    }
}

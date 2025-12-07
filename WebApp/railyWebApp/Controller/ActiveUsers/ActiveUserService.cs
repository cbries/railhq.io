// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using StackExchange.Redis;
using System;
using System.Threading.Tasks;
// ReSharper disable InconsistentNaming
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.ActiveUsers
{
    public class ActiveUserService
    {
        private readonly IDatabase _db;
        private const string ACTIVE_USERS_KEY = "active_users";
        private const int TTL_SECONDS = 300; // 5 Minuten

        public ActiveUserService(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task MarkUserActive(string userId)
        {
            await _db.SetAddAsync(ACTIVE_USERS_KEY, userId);
            await _db.KeyExpireAsync(ACTIVE_USERS_KEY, TimeSpan.FromSeconds(TTL_SECONDS));
        }

        public async Task<int> GetActiveUserCount()
        {
            return (int)await _db.SetLengthAsync(ACTIVE_USERS_KEY);
        }
    }
}

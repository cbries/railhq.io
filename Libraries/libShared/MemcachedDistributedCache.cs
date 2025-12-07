// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿//using System;
//using System.Threading;
//using System.Threading.Tasks;
//using Enyim.Caching;
//using Enyim.Caching.Memcached;
//using Microsoft.Extensions.Caching.Distributed;

//namespace libShared
//{
//    public class MemcachedDistributedCache : IDistributedCache
//    {
//        private readonly IMemcachedClient _memcachedClient;

//        public MemcachedDistributedCache(IMemcachedClient memcachedClient)
//        {
//            _memcachedClient = memcachedClient;
//        }

//        public byte[] Get(string key)
//        {
//            return _memcachedClient.Get<byte[]>(key);
//        }

//        public Task<byte[]> GetAsync(string key, CancellationToken token = default)
//        {
//            return Task.FromResult(Get(key));
//        }

//        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
//        {
//            var success = _memcachedClient.Store(StoreMode.Set, key, value, DateTime.Now.Add(options.AbsoluteExpirationRelativeToNow ?? TimeSpan.Zero));
//            if (!success)
//            {
//                throw new InvalidOperationException("Failed to store value in Memcached.");
//            }
//        }

//        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default)
//        {
//            Set(key, value, options);
//            return Task.CompletedTask;
//        }

//        public void Refresh(string key)
//        {
//            // Memcached unterstützt keine explizite "Refresh"-Funktion, also lassen wir es hier leer.
//        }

//        public Task RefreshAsync(string key, CancellationToken token = default)
//        {
//            Refresh(key);
//            return Task.CompletedTask;
//        }

//        public void Remove(string key)
//        {
//            _memcachedClient.Remove(key);
//        }

//        public Task RemoveAsync(string key, CancellationToken token = default)
//        {
//            Remove(key);
//            return Task.CompletedTask;
//        }
//    }

//}

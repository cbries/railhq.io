// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

namespace libAutomaticModus.Running
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;

    public class Routes : IEnumerable<RouteData>
    {
        private readonly List<RouteData> _routes = new();
        private readonly ReaderWriterLockSlim _lock = new();

        public int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _routes.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }

        public RouteData GetRandomRoute()
        {
            _lock.EnterReadLock();
            try
            {
                if (_routes == null || _routes.Count == 0)
                    return null; 

                var zufall = new Random();
                int index = zufall.Next(_routes.Count);  
                return _routes[index]; 
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }
        
        public void Add(RouteData route)
        {
            _lock.EnterWriteLock();
            try { _routes.Add(route); }
            finally { _lock.ExitWriteLock(); }
        }

        public void Clear()
        {
            _lock.EnterWriteLock();
            try { _routes.Clear(); }
            finally { _lock.ExitWriteLock(); }
        }

        public bool Remove(RouteData route)
        {
            _lock.EnterWriteLock();
            try { return _routes.Remove(route); }
            finally { _lock.ExitWriteLock(); }
        }

        public RouteData Get(int index)
        {
            _lock.EnterReadLock();
            try { return _routes[index]; }
            finally { _lock.ExitReadLock(); }
        }

        public List<RouteData> GetAll()
        {
            _lock.EnterReadLock();
            try { return [.._routes]; }  // Kopie für Sicherheit
            finally { _lock.ExitReadLock(); }
        }

        public IEnumerator<RouteData> GetEnumerator()
        {
            _lock.EnterReadLock();
            try { return _routes.ToList().GetEnumerator(); } // Kopie für sicheres Iterieren
            finally { _lock.ExitReadLock(); }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

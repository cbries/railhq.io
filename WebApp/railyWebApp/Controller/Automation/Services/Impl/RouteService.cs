// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using railyWebApp.Controller.Automation.Dto.Entity;

// ReSharper disable PossibleMultipleEnumeration
// ReSharper disable ConvertToPrimaryConstructor

namespace railyWebApp.Controller.Automation.Services.Impl
{
    public class RouteService : AbstractBaseService, IRouteService
    {
        public RouteService(string uid) : base(uid)
        {
            // ignore
        }

        #region IRouteService

        public async Task<IEnumerable<Route>> GetAll()
        {
            var res = Globals.UserWorkspaces.TryGetValue(Uid, out var workspace);
            if (!res) throw new Exception("Workspace not loaded");

            var metamodel = workspace.Metamodel?.Settings;
            if (metamodel == null) throw new Exception("Workspace model not loaded");

            var resRoutes = new List<Route>();

            foreach (var itRoute in metamodel.Routes)
            {
                var route = itRoute.Key;

                var instance = new Route
                {
                    Name = route.Name,
                    IsEnabled = route.IsEnabled,
                    IsLocked = route.IsLocked,
                    IsOccupied = route.IsOccupied,
                    IsOccupiedReason = route.IsOccupiedReason
                };

                resRoutes.Add(instance);
            }

            return resRoutes;
        }

        #endregion
    }
}

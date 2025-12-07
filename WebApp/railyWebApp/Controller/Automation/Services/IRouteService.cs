// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using railyWebApp.Controller.Automation.Dto.Entity;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace railyWebApp.Controller.Automation.Services
{
    public interface IRouteService
    {
        // === Verwaltung ===
        Task<IEnumerable<Route>> GetAll();
    }
}

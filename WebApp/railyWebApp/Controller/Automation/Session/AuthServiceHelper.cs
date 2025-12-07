// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using libUserspace;
using libUserspace.Auth;
using libUtilities;
using System;
using System.Threading.Tasks;

namespace railyWebApp.Controller.Automation.Session
{
    public class AuthServiceHelper
    {
        internal static async Task<AuthSession> GetSession(
            SupabaseService authService,
            string username, string password)
        {
            if (string.IsNullOrEmpty(username)) return null;
            if (string.IsNullOrEmpty(password)) return null;

            try
            {
                var session = await authService.Login(username, password, false);
                if (session?.User != null) return session;
                return null;
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }

            return null;
        }
    }
}

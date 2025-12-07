// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using libUserspace.Auth;
using libUtilities;
// ReSharper disable InconsistentNaming

namespace libUserspace
{
    /// <summary>
    /// Authentication service that uses a JSON file for user management.
    /// This replaces the previous Supabase-based authentication.
    /// </summary>
    public class SupabaseService : IAuthService
    {
        private readonly JsonAuthService _authService;
        
        protected string UsersFilePath { get; set; }

        /// <summary>
        /// Gets the underlying JsonAuthService for admin operations
        /// </summary>
        public JsonAuthService Auth => _authService;

        /// <summary>
        /// Creates a new SupabaseService with JSON-based authentication.
        /// </summary>
        /// <param name="usersFilePath">Path to the users.json file</param>
        public SupabaseService(string usersFilePath)
        {
            UsersFilePath = usersFilePath;

            if (string.IsNullOrEmpty(UsersFilePath))
            {
                throw new Exception("Users file path is missing in configuration");
            }

            // Ensure the path exists
            var directory = Path.GetDirectoryName(UsersFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            _authService = new JsonAuthService(UsersFilePath);
        }

        /// <summary>
        /// Legacy constructor for backwards compatibility.
        /// The url and key parameters are ignored - uses default users.json path.
        /// </summary>
        [Obsolete("Use the constructor with usersFilePath instead")]
        public SupabaseService(string url, string key) 
            : this(GetDefaultUsersFilePath())
        {
            // url and key are ignored - they were for Supabase
            Logging.Log.Warn("SupabaseService: url and key parameters are ignored. Using JSON-based auth.");
        }

        private static string GetDefaultUsersFilePath()
        {
            var baseDir = libShared.RailEnvironment.GetBaseDirectory();
            return Path.Combine(baseDir, "resources", "users.json");
        }

        public void LoadSession()
        {
            _authService.LoadSession();
        }
        
        public async Task<AuthSession> Register(string email, string password)
        {
            try
            {
                return await _authService.Register(email, password);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
                throw;
            }
        }

        public async Task<AuthSession> Login(string email, string password, bool remember = false)
        {
            try
            {
                return await _authService.Login(email, password, remember);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
                return null;
            }
        }

        public async Task Logout()
        {
            try
            {
                await _authService.Logout();
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        public async Task ResetPassword(string email)
        {
            try
            {
                await _authService.ResetPassword(email);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
            }
        }

        public async Task<AuthUser> Validate(string jwt)
        {
            if (string.IsNullOrEmpty(jwt)) return null;

            try
            {
                return await _authService.Validate(jwt);
            }
            catch (Exception ex)
            {
                Logging.ExceptionLog(ex);
                return null;
            }
        }

        public async Task<AuthUser> GetUser(string jwt)
        {
            return await _authService.GetUser(jwt);
        }

        public AuthUser CurrentUser => _authService.CurrentUser;

        public async Task<bool> DeleteUser(Guid userGuid)
        {
            return await _authService.DeleteUser(userGuid);
        }

        /// <summary>
        /// Gets all users (admin operation)
        /// </summary>
        public List<AuthUser> GetAllUsers()
        {
            return _authService.GetAllUsers();
        }

        /// <summary>
        /// Creates a new user directly (admin operation)
        /// </summary>
        public AuthUser CreateUser(string email, string password, string role = "user")
        {
            return _authService.CreateUser(email, password, role);
        }

        /// <summary>
        /// Updates a user's password
        /// </summary>
        public bool UpdatePassword(string userId, string newPassword)
        {
            return _authService.UpdatePassword(userId, newPassword);
        }

        /// <summary>
        /// Updates a user's email address
        /// </summary>
        /// <param name="userId">The user ID</param>
        /// <param name="newEmail">The new email address</param>
        /// <returns>True if successful, false otherwise</returns>
        public bool UpdateEmail(string userId, string newEmail)
        {
            return _authService.UpdateEmail(userId, newEmail);
        }
    }
}

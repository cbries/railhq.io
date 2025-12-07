// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace libUserspace.Auth
{
    /// <summary>
    /// Interface for authentication services.
    /// Implementations can use JSON files, databases, or external services like Supabase.
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// Registers a new user with email and password
        /// </summary>
        Task<AuthSession> Register(string email, string password);

        /// <summary>
        /// Logs in a user with email and password
        /// </summary>
        Task<AuthSession> Login(string email, string password, bool remember = false);

        /// <summary>
        /// Logs out the current user
        /// </summary>
        Task Logout();

        /// <summary>
        /// Sends a password reset email
        /// </summary>
        Task ResetPassword(string email);

        /// <summary>
        /// Validates a JWT token and returns the user if valid
        /// </summary>
        Task<AuthUser> Validate(string jwt);

        /// <summary>
        /// Gets a user by their JWT token
        /// </summary>
        Task<AuthUser> GetUser(string jwt);

        /// <summary>
        /// Gets the currently logged in user (if any)
        /// </summary>
        AuthUser CurrentUser { get; }

        /// <summary>
        /// Loads an existing session from storage
        /// </summary>
        void LoadSession();

        /// <summary>
        /// Deletes a user by their GUID (admin operation)
        /// </summary>
        Task<bool> DeleteUser(Guid userGuid);
    }
}

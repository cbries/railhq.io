// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libUserspace.Auth
{
    /// <summary>
    /// Represents an authenticated user (replaces Supabase.Gotrue.User)
    /// </summary>
    public class AuthUser
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public DateTime? EmailConfirmedAt { get; set; }
        public DateTime? ConfirmationSentAt { get; set; }
        public DateTime? ConfirmedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSignInAt { get; set; }
        public DateTime? RecoverySentAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? BannedUntil { get; set; }
        public string Role { get; set; } = "user";
        
        /// <summary>
        /// Password hash (BCrypt or similar)
        /// Only used internally for JSON-based authentication
        /// </summary>
        public string PasswordHash { get; set; }
    }
}

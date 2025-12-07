// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libUserspace.Auth
{
    /// <summary>
    /// Represents an authentication session (replaces Supabase.Gotrue.Session)
    /// </summary>
    public class AuthSession
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public long ExpiresIn { get; set; }
        public DateTime ExpiresAt { get; set; }
        public AuthUser User { get; set; }
        public string TokenType { get; set; } = "Bearer";
    }
}

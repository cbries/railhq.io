// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace libUserspace.Auth
{
    /// <summary>
    /// Container class for JSON serialization of users
    /// </summary>
    public class UsersContainer
    {
        public List<AuthUser> Users { get; set; } = new List<AuthUser>();
    }
}

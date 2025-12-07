// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

namespace libUserspace.Sbase
{
    public class UserData
    {
        /// <summary>
        /// Unique username used in public areas like Comments / Forums.
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// The name of the user how he likes to be called.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Can be used for password reset or OTP logins.
        /// </summary>
        public string PhoneNumber { get; set; }

        /// <summary>
        /// Country of the user for localization purposes.
        /// </summary>
        public string Country { get; set; }
    }
}
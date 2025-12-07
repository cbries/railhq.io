// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable ExplicitCallerInfoArgument

namespace libUserspace.Sbase
{
    /// <summary>
    /// User subscriptions model - DEPRECATED, subscription functionality has been removed.
    /// Kept for backward compatibility only.
    /// </summary>
    [Obsolete("Subscription functionality has been removed. This class is kept for backward compatibility only.")]
    public class UserSubscriptions
    {
        public int Id { get; set; }
        public string UserId { get; set; }
    }
}

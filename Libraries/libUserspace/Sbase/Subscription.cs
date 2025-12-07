// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable ExplicitCallerInfoArgument

namespace libUserspace.Sbase
{
    /// <summary>
    /// Subscription created model - DEPRECATED, subscription functionality has been removed.
    /// Kept for backward compatibility only.
    /// </summary>
    [Obsolete("Subscription functionality has been removed. This class is kept for backward compatibility only.")]
    public class SubscriptionCreated
    {
        public int Id { get; set; }
        public Guid UserId { get; set; } = Guid.Empty;
    }

    /// <summary>
    /// Subscription activated model - DEPRECATED, subscription functionality has been removed.
    /// </summary>
    [Obsolete("Subscription functionality has been removed. This class is kept for backward compatibility only.")]
    public class SubscriptionActivated
    {
        public int Id { get; set; }
    }

    /// <summary>
    /// Subscription cancelled model - DEPRECATED, subscription functionality has been removed.
    /// </summary>
    [Obsolete("Subscription functionality has been removed. This class is kept for backward compatibility only.")]
    public class SubscriptionCancelled
    {
        public int Id { get; set; }
    }
}

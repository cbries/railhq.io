// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

using System;

// ReSharper disable ExplicitCallerInfoArgument

namespace libUserspace.Sbase
{
    /// <summary>
    /// Payment model - DEPRECATED, payment functionality has been removed.
    /// Kept for backward compatibility only.
    /// </summary>
    [Obsolete("Payment functionality has been removed. This class is kept for backward compatibility only.")]
    public class Payment
    {
        public int Id { get; set; }
        public Guid UserId { get; set; }
    }

    /// <summary>
    /// Failed payment model - DEPRECATED, payment functionality has been removed.
    /// </summary>
    [Obsolete("Payment functionality has been removed. This class is kept for backward compatibility only.")]
    public class PaymentFailed : Payment
    {
    }
}

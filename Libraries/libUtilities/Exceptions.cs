// Copyright (c) 2025 Dr. Christian Benjamin Ries
// Licensed under the MIT License
// See LICENSE file in the project root for full license information.

﻿using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace libUtilities
{
    public static class Exceptions
    {
        public static IEnumerable<Exception> GetInnerExceptions(this Exception ex)
        {
            Exception innerException = ex ?? throw new ArgumentNullException(nameof(ex));
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            } while (innerException != null);
        }

        public static string GetInnerExceptionsAsString(this Exception ex)
        {
            return string.Join(Environment.NewLine, ex.GetInnerExceptions());
        }

        public static string GetExceptionMessages(this Exception ex) =>
            ex.Message
            + Environment.NewLine
            + string.Join(Environment.NewLine, ex.GetInnerExceptions());

        public static void ShowException(this Exception ex)
        {
            Trace.WriteLine($"{ex.GetExceptionMessages()}");
        }
    }
}
